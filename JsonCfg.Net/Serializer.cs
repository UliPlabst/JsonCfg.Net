
using System.Text;

namespace JsonCfgNet;

public class SerializerSettings
{
    public bool WriteComments { get; set; } = true;
    public string Indent { get; set; } = "  ";
    public bool WriteTrailingCommas { get; set; } = false;
    public int SpaceToLineComments { get; set; } = 1;
    public int SpaceToInlineComments { get; set; } = 2;
    public bool SpacedBraces { get; set; } = true;
}

internal record WriteOptions(
    bool SkipLeadingComments = false,
    bool SkipTrailingComments = false
){}

internal class JsonWriter(SerializerSettings settings)
{
    private readonly StringBuilder _sb = new();
    private int _indentLevel = 0;
    private bool _doIndent => !string.IsNullOrEmpty(settings.Indent);
    
    public void WriteIndent()
    {
        if (!_doIndent)
            return;
        for (int i = 0; i < _indentLevel; i++)
        {
            Append(settings.Indent);
        }
    }

    public void WriteComment(JsonComment comment, bool skipLeadingNewline = false)
    {
        if (comment.CommentKind == JsonCommentKind.Inline)
        {
            if (comment.LeadingNewline)
            {
                if (!skipLeadingNewline)
                {
                    Append(Environment.NewLine);
                    WriteIndent();
                }
                Append(comment.Comment);
            }
            else
            {
                Append(new string(' ', settings.SpaceToInlineComments));
                Append(comment.Comment);
                Append(new string(' ', settings.SpaceToInlineComments));
            }
        }
        else
        {
            if (comment.LeadingNewline)
            {
                if (!skipLeadingNewline)
                {
                    Append(Environment.NewLine);
                    WriteIndent();
                }
            }
            else
            {
                Append(new string(' ', settings.SpaceToLineComments));
            }
            Append(comment.Comment);
        }
    }

    public bool WriteComments(IEnumerable<JsonComment>? comments, bool skipInitialNewline = false)
    {
        if (comments == null || !settings.WriteComments)
            return false;

        var i = 0;
        var found = false;
        foreach(var comment in comments)
        {
            WriteComment(comment, skipLeadingNewline: i == 0 && skipInitialNewline);
            i++;
            found = true;
        }
        return found;
    }
    
    public void WriteObject(JsonObject obj, WriteOptions? options = null)
    {
        if (obj.FormattingHint == FormattingHint.Inline && !obj.WillBreakLine)
        {
            WriteObjectInline(obj, options);
        }
        else
        {
            WriteObjectMultiline(obj, options);
        }
    }
    
    public void WriteObjectInline(JsonObject obj, WriteOptions? options = null)
    {
        WriteComments(obj.Trivia.LeadingComments);
        Append("{");
        if(settings.SpacedBraces)
            Append(" ");
            
        var props = obj.Properties.Values.ToList();
        for (int i = 0; i < props.Count; i++)
        {
            var prop = props[i];
            var isLast = i == obj.Properties.Count - 1;
            
            WriteComments(prop.Trivia.LeadingComments, true);
            Append($"\"{prop.Key}\"");
            WriteComments(prop.Trivia.TrailingComments);
            Append(":");
            if(prop.Value.Trivia.LeadingComments == null || !prop.Value.Trivia.LeadingComments.Any())
                Append(" ");
            Write(prop.Value);

            if (!isLast)
            {
                Append(",");
                var next = props[i + 1];
                WriteComments(next.Trivia.LeadingComments);
            }
            else if (settings.WriteTrailingCommas || prop.HasTrailingComma)
            {
                Append(",");
            }
        }
        WriteComments(obj.Trivia.ContainedComments);
        if(settings.SpacedBraces)
            Append(" ");
        Append("}");
        if(options?.SkipTrailingComments != true)
            WriteComments(obj.Trivia.TrailingComments);
    }

    public void WriteObjectMultiline(JsonObject obj, WriteOptions? options = null)
    {
        WriteComments(obj.Trivia.LeadingComments);
        Append("{");
        if(obj.Properties.Count == 0 && obj.Trivia.ContainedComments?.Count > 0)
        {
            WriteComments(obj.Trivia.ContainedComments);
            Append(Environment.NewLine);
        }
        else if (obj.Properties.Count > 0)
        {
            WriteComments(
                obj.Properties.First().Value.Trivia.LeadingComments
                    ?.Where(e => !e.LeadingNewline)
            );
            Append(Environment.NewLine);
        }
        _indentLevel++;
        var props = obj.Properties.Values.ToList();
        for (int i = 0; i < props.Count; i++)
        {
            var isLast = i == obj.Properties.Count - 1;
            var prop = props[i];
            if (i == 0)
            {
                if (prop.Trivia.LeadingComments?.Any(e => e.LeadingNewline) == true)
                {
                    WriteIndent();
                    if(WriteComments(
                        prop.Trivia.LeadingComments
                            ?.Where(e => e.LeadingNewline), 
                        true
                    ))
                    {
                        Append(Environment.NewLine);
                    }
                }
            }

            WriteIndent();
            Append($"\"{prop.Key}\"");
            WriteComments(prop.Trivia.TrailingComments);
            Append(":");
            if(prop.Value.Trivia.LeadingComments == null || !prop.Value.Trivia.LeadingComments.Any())
                Append(" ");
            Write(prop.Value, new(SkipTrailingComments: true));

            if (!isLast)
            {
                Append(",");
                WriteComments(prop.Value.Trivia.TrailingComments);
                var next = props[i + 1];
                WriteComments(next.Trivia.LeadingComments);
                Append(Environment.NewLine);
            }
            else if (settings.WriteTrailingCommas || prop.HasTrailingComma)
            {
                Append(",");
                WriteComments(prop.Value.Trivia.TrailingComments);
            }
            else
            {
                WriteComments(prop.Value.Trivia.TrailingComments);
            }

        }
        WriteComments(obj.Trivia.ContainedComments);
        _indentLevel--;
        if (obj.Properties.Count > 0 || obj.Trivia.ContainedComments?.Count > 0)
        {
            Append(Environment.NewLine);
            WriteIndent();
        }
        Append("}");
        if(options?.SkipTrailingComments != true)
            WriteComments(obj.Trivia.TrailingComments);
    }

    public void WriteValue(JsonValue value, WriteOptions? options = null)
    {
        if(options?.SkipLeadingComments != true)
            WriteComments(value.Trivia.LeadingComments);
            
        Append(value.StringValue);
        
        if(options?.SkipTrailingComments != true)
            WriteComments(value.Trivia.TrailingComments);
    }

    public void Append(string str)
    {
        _sb.Append(str);
        // Console.WriteLine(_sb.ToString());
    }

    public void Write(JsonNode node, WriteOptions? options = null)
    {
        if (node is JsonArray array)
            WriteArray(array, options);
        else if (node is JsonObject obj)
            WriteObject(obj, options);
        else if (node is JsonValue value)
            WriteValue(value, options);
        else
            throw new InvalidOperationException($"Invalid node type {node}");
    }

    
    public void WriteArray(JsonArray array, WriteOptions? options = null)
    {
        if (array.FormattingHint == FormattingHint.Inline && !array.WillBreakLine)
        {
            WriteArrayInline(array, options);
        }
        else
        {
            WriteArrayMultiline(array, options);
        }
    }

    public void WriteArrayInline(JsonArray array, WriteOptions? options = null)
    {
        WriteComments(array.Trivia.LeadingComments);
        Append("[");
        if(settings.SpacedBraces)
            Append(" ");
        for (int i = 0; i < array.Items.Count; i++)
        {
            var isLast = i == array.Items.Count - 1;
            var item = array.Items[i];
            if(i == 0)
                WriteComments(item.Trivia.LeadingComments, true);
            Write(item, new(SkipLeadingComments: true));
            if (!isLast)
            {
                Append(",");
                var next = array.Items[i + 1];
                WriteComments(next.Trivia.LeadingComments);
            }
            else if (settings.WriteTrailingCommas)
            {
                Append(",");
            }

        }
        WriteComments(array.Trivia.ContainedComments);
        if(settings.SpacedBraces)
            Append(" ");
        Append("]");
        if(options?.SkipTrailingComments != true)
            WriteComments(array.Trivia.TrailingComments);
    }

    public void WriteArrayMultiline(JsonArray array, WriteOptions? options = null)
    {
        WriteComments(array.Trivia.LeadingComments);
        Append("[");
        if (array.Items.Count > 0 || array.Trivia.ContainedComments?.Count > 0)
            Append(Environment.NewLine);
        _indentLevel++;
        for (int i = 0; i < array.Items.Count; i++)
        {
            var isLast = i == array.Items.Count - 1;
            var item = array.Items[i];
            if (i == 0)
            {
                if (item.Trivia.LeadingComments?.Count > 0)
                {
                    WriteIndent();
                    WriteComments(item.Trivia.LeadingComments, true);
                    Append(Environment.NewLine);
                }
            }

            WriteIndent();
            Write(item, new(SkipTrailingComments: true));
            if (!isLast)
            {
                Append(",");
                WriteComments(item.Trivia.TrailingComments);
                var next = array.Items[i + 1];
                WriteComments(next.Trivia.LeadingComments);
                Append(Environment.NewLine);
            }
            else if (settings.WriteTrailingCommas)
            {
                Append(",");
                WriteComments(item.Trivia.TrailingComments);
            }

        }
        WriteComments(array.Trivia.ContainedComments);
        _indentLevel--;
        if (array.Items.Count > 0 || array.Trivia.ContainedComments?.Count > 0)
        {
            Append(Environment.NewLine);
            WriteIndent();
        }
        Append("]");
        if(options?.SkipTrailingComments != true)
            WriteComments(array.Trivia.TrailingComments);
    }


    public string GetContent() => _sb.ToString();

}

public static class Serializer
{
    public static SerializerSettings DefaultSettings { get; } = new SerializerSettings();

    public static string Serialize(JsonNode node, SerializerSettings? settings = null)
    {
        var writer = new JsonWriter(settings ?? DefaultSettings);
        writer.Write(node);
        return writer.GetContent();
    }
}