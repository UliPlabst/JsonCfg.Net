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
                for(var i=obj.Properties.Count-1; i>= 0; i--)
                {
                   stack.Insert(0, obj.Properties[i]); 
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

    public JsonValue(JsonValueKind valueKind, string stringValue)
    {
        ValueKind = valueKind;
        _value = stringValue;
    }
    
    public static JsonValue FromObject(object? value)
    {
        return value switch {
            null => new JsonValue(JsonValueKind.Null, "null"),
            string s => new JsonValue(JsonValueKind.String, s),
            int i => new JsonValue(JsonValueKind.Number, i.ToString()),
            long l => new JsonValue(JsonValueKind.Number, l.ToString()),
            float f => new JsonValue(JsonValueKind.Number, f.ToString()),
            double d => new JsonValue(JsonValueKind.Number, d.ToString()),
            decimal d => new JsonValue(JsonValueKind.Number, d.ToString()),
            bool b => new JsonValue(JsonValueKind.Boolean, b.ToString().ToLowerInvariant()),
            DateTime dt => new JsonValue(JsonValueKind.String, dt.ToString("o")),
            DateTimeOffset dto => new JsonValue(JsonValueKind.String, dto.ToString("o")),
            Guid g => new JsonValue(JsonValueKind.String, g.ToString()),
            TimeSpan ts => new JsonValue(JsonValueKind.String, ts.ToString()),
            DateOnly dto => new JsonValue(JsonValueKind.String, dto.ToString()),
            TimeOnly to => new JsonValue(JsonValueKind.String, to.ToString()),
            _ => throw new ArgumentException("Unsupported value type")
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
    public string Key { get; set; }
    public JsonNode Value { get; set; }
}

public class JsonObject: JsonNode
{
    public override JsonNodeKind Kind => JsonNodeKind.Object;
    public List<JsonProperty> Properties { get; set; } = [];
}

public class JsonArray : JsonNode
{
    public override JsonNodeKind Kind => JsonNodeKind.Array;
    public List<JsonNode> Items { get; set; } = [];
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
    public bool WillBreakLine => LeadingNewline || Comment.Contains(Environment.NewLine);
}

public class Trivia
{
    public List<JsonComment>? LeadingComments { get; set; }
    public List<JsonComment>? TrailingComments { get; set; }
    public List<JsonComment>? ContainedComments { get; set; }
}