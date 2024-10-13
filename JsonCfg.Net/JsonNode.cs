using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace JsonCfgNet;

public enum JsonNodeKind
{
    Value,
    Array,
    Object,
    Property,
    Comment
}

public enum JsonValueKind
{
    Null,
    String,
    Number,
    Boolean
}

public abstract class JsonNode
{
    public FormattingHint? FormattingHint { get; set; }
    public abstract JsonNodeKind Kind { get; }
    public Trivia Trivia { get; set; } = new Trivia();
    public bool HasTrailingComma { get; set; }
    
    public abstract bool WillBreakLine { get; }
    
    public static JsonNode Parse(string input)
        => Parser.Instance.Parse(input);
    
    public static IEnumerable<JsonNode> EnumerateAll(JsonNode node)
    {
        var stack = new List<JsonNode>
        {
            node
        };
        while(stack.Count > 0)
        {
            var current = stack[0];
            stack.RemoveAt(0);
            yield return current;
            
            if(current is JsonObject obj)
            {
                var props = obj.Properties.Values.ToList();
                for(var i=props.Count-1; i>= 0; i--)
                {
                   stack.Insert(0, props[i]); 
                }
            }   
            else if(current is JsonArray arr)
            {
                for(var i=arr.Items.Count-1; i>= 0; i--)
                {
                   stack.Insert(0, arr.Items[i]); 
                }
            }         
            else if(current is JsonProperty prop)
            {
                stack.Insert(0, prop.Value);
            }
        }
    }
}

public class JsonValue: JsonNode
{
    public override JsonNodeKind Kind => JsonNodeKind.Value;
    public JsonValueKind ValueKind { get; set; }
    private string _value { get; set; }
    public string StringValue => _value;

    public override bool WillBreakLine 
        => (Trivia.LeadingComments?.Any(e => e.WillBreakLine) ?? false)
        || (Trivia.ContainedComments?.Any(e => e.WillBreakLine) ?? false)
        || (Trivia.TrailingComments?.Any(e => e.WillBreakLine) ?? false);
    
    public JsonValue(JsonValueKind valueKind, string stringValue)
    {
        ValueKind = valueKind;
        _value = stringValue;
    }
    
    public static JsonValue FromObject(object? value)
    {
        return value switch {
            null               => new JsonValue(JsonValueKind.Null, "null"),
            string s           => new JsonValue(JsonValueKind.String, $"\"{s}\""),
            int i              => new JsonValue(JsonValueKind.Number, i.ToString()),
            long l             => new JsonValue(JsonValueKind.Number, l.ToString()),
            float f            => new JsonValue(JsonValueKind.Number, f.ToString()),
            double d           => new JsonValue(JsonValueKind.Number, d.ToString()),
            decimal d          => new JsonValue(JsonValueKind.Number, d.ToString()),
            bool b             => new JsonValue(JsonValueKind.Boolean, b.ToString().ToLowerInvariant()),
            DateTime dt        => new JsonValue(JsonValueKind.String, $"\"{dt:o}\""),
            DateTimeOffset dto => new JsonValue(JsonValueKind.String, $"\"{dto:o}\""),
            Guid g             => new JsonValue(JsonValueKind.String, $"\"{g}\""),
            TimeSpan ts        => new JsonValue(JsonValueKind.String, $"\"{ts}\""),
            DateOnly dto       => new JsonValue(JsonValueKind.String, $"\"{dto}\""),
            TimeOnly to        => new JsonValue(JsonValueKind.String, $"\"{to}\""),
            _                  => throw new ArgumentException("Unsupported value type")
        };
    }
}

public enum FormattingHint
{
    Inline,
    Multiline
}

public class JsonProperty : JsonNode
{
    public override JsonNodeKind Kind => JsonNodeKind.Property;
    public required string Key { get; set; }
    public required JsonNode Value { get; set; }

    public override bool WillBreakLine 
        => Value.WillBreakLine
        || (Trivia.LeadingComments?.Any(e => e.WillBreakLine) ?? false)
        || (Trivia.ContainedComments?.Any(e => e.WillBreakLine) ?? false)
        || (Trivia.TrailingComments?.Any(e => e.WillBreakLine) ?? false);
}

public class JsonObject: JsonNode, IDictionary<string, JsonNode>
{
    public override bool WillBreakLine 
        => FormattingHint != JsonCfgNet.FormattingHint.Inline
        || (Trivia.LeadingComments?.Any(e => e.WillBreakLine) ?? false)
        || (Trivia.ContainedComments?.Any(e => e.WillBreakLine) ?? false)
        || (Trivia.TrailingComments?.Any(e => e.WillBreakLine) ?? false)
        || Properties.Any(e => e.Value.WillBreakLine);

    public override JsonNodeKind Kind => JsonNodeKind.Object;
    public Dictionary<string, JsonProperty> Properties { get; set; } = [];
    public ICollection<string> Keys => Properties.Keys;
    public ICollection<JsonNode> Values => Properties.Values.Select(e => e.Value).ToList();
    public int Count => Properties.Count;
    public bool IsReadOnly => false;

    public void Add(string key, JsonNode value) => Properties.Add(key, new JsonProperty
    {
        Key = key,
        Value = value
    });

    public void Add(KeyValuePair<string, JsonNode> item) => Add(item.Key, item.Value);

    public void Clear() => Properties.Clear();
    public bool Contains(KeyValuePair<string, JsonNode> item) => Properties.Any(e => e.Key == item.Key && e.Value.Value == item.Value);
    public bool ContainsKey(string key) => Properties.ContainsKey(key);

    public void CopyTo(KeyValuePair<string, JsonNode>[] array, int arrayIndex) 
    {
        if(array.Length + arrayIndex < Properties.Count)
            throw new ArgumentException("Array is too small", nameof(array));
        foreach(var a in Properties)
        {
            array[arrayIndex++] = new KeyValuePair<string, JsonNode>(a.Key, a.Value.Value);
        }
    }

    public IEnumerator<KeyValuePair<string, JsonNode>> GetEnumerator() 
        => Properties.Select(e => new KeyValuePair<string, JsonNode>(e.Key, e.Value)).GetEnumerator();

    public bool Remove(string key) => Properties.Remove(key);
    public bool Remove(KeyValuePair<string, JsonNode> item) => Remove(item.Key);

    public bool TryGetValue(string key, [NotNullWhen(true)] out JsonNode? value)
    {
        if(Properties.TryGetValue(key, out var prop))
        {
            value = prop.Value;
            return true;
        }
        value = null;
        return false;
    }
    
    public JsonNode this[string key]
    {
        get 
        {
            if(TryGetValue(key, out var value))
                return value;
            return null!;    
        }
        set 
        {
            Properties[key] = new JsonProperty
            {
                Key = key,
                Value = value!
            };
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class JsonArray : JsonNode, ICollection<JsonNode>
{
    public override JsonNodeKind Kind => JsonNodeKind.Array;
    public List<JsonNode> Items { get; set; } = [];
    
    public override bool WillBreakLine 
        => FormattingHint != JsonCfgNet.FormattingHint.Inline
        || (Trivia.LeadingComments?.Any(e => e.WillBreakLine) ?? false)
        || (Trivia.ContainedComments?.Any(e => e.WillBreakLine) ?? false)
        || (Trivia.TrailingComments?.Any(e => e.WillBreakLine) ?? false)
        || Items.Any(e => e.WillBreakLine);

    public int Count => Items.Count;

    public bool IsReadOnly => false;

    public void Add(JsonNode item) => Items.Add(item);
    public void Clear() => Items.Clear();
    public bool Contains(JsonNode item) => Items.Contains(item);
    public void CopyTo(JsonNode[] array, int arrayIndex) => Items.CopyTo(array, arrayIndex);
    public IEnumerator<JsonNode> GetEnumerator() => Items.GetEnumerator();
    public bool Remove(JsonNode item) => Items.Remove(item);
    IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
}

public enum JsonCommentKind
{
    Line,
    Inline
}

public class JsonComment : JsonNode
{
    public override JsonNodeKind Kind => JsonNodeKind.Comment;
    public JsonCommentKind CommentKind { get; set; }
    public required string Comment { get; set; }
    public required bool LeadingNewline { get; set; }
    public override bool WillBreakLine => LeadingNewline || Comment.Contains(Environment.NewLine);
}

public class Trivia
{
    public List<JsonComment>? LeadingComments { get; set; }
    public List<JsonComment>? TrailingComments { get; set; }
    public List<JsonComment>? ContainedComments { get; set; }
}