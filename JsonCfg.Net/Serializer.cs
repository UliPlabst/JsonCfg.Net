
using System.Text;

namespace JsonCfgNet;

public class SerializerSettings
{
    public bool WriteComments { get; set; } = true;
    public string Indent { get; set; } = "  ";
    public bool WriteTrailingCommas { get; set; } = false;
    public int SpaceToInlineComments { get; set; } = 2;
    public bool SpacedBraces { get; set; } = true;
}

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
            Append(comment.Comment.Trim());
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
                Append(new string(' ', settings.SpaceToInlineComments));
            }
            Append(comment.Comment.Trim());
        }
    }

    public bool WriteComments(List<JsonComment>? comments, bool skipInitialNewline = false)
    {
        if (comments == null || !settings.WriteComments)
            return false;

        for (int i = 0; i < comments.Count; i++)
        {
            var comment = comments[i];
            WriteComment(comment, skipLeadingNewline: i == 0 && skipInitialNewline);
        }
        return comments.Count > 0;
    }
    
    public void WriteObject(JsonObject obj)
    {
        if (obj.FormattingHint == FormattingHint.Inline && CanWriteInline(obj))
        {
            WriteObjectInline(obj);
        }
        else if (!obj.FormattingHint.HasValue || obj.FormattingHint == FormattingHint.Multiline)
        {
            WriteObjectMultiline(obj);
        }
    }
    
    public void WriteObjectInline(JsonObject obj)
    {
        WriteComments(obj.Trivia.LeadingComments);
        Append("{");
        if(settings.SpacedBraces)
            Append(" ");
        for (int i = 0; i < obj.Properties.Count; i++)
        {
            var prop = obj.Properties[i];
            var isLast = i == obj.Properties.Count - 1;
            
            WriteComments(prop.Trivia.LeadingComments, true);
            Append($"\"{prop.Key}\": ");
            WriteComments(prop.Trivia.TrailingComments);
            Write(prop.Value);

            if (!isLast)
            {
                Append(",");
                var next = obj.Properties[i + 1];
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
        WriteComments(obj.Trivia.TrailingComments);
    }

    public void WriteObjectMultiline(JsonObject obj)
    {
        WriteComments(obj.Trivia.LeadingComments);
        Append("{");
        if (obj.Properties.Count > 0 || obj.Trivia.ContainedComments?.Count > 0)
            Append(Environment.NewLine);
        _indentLevel++;
        for (int i = 0; i < obj.Properties.Count; i++)
        {
            var isLast = i == obj.Properties.Count - 1;
            var prop = obj.Properties[i];
            if (i == 0)
            {
                if (prop.Trivia.LeadingComments?.Count > 0)
                {
                    WriteIndent();
                    WriteComments(prop.Trivia.LeadingComments, true);
                    Append(Environment.NewLine);
                }
            }

            WriteIndent();
            Append($"\"{prop.Key}\": ");
            WriteComments(prop.Trivia.TrailingComments);
            Write(prop.Value);

            if (!isLast)
            {
                Append(",");
                var next = obj.Properties[i + 1];
                WriteComments(next.Trivia.LeadingComments);
                Append(Environment.NewLine);
            }
            else if (settings.WriteTrailingCommas || prop.HasTrailingComma)
            {
                Append(",");
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
        WriteComments(obj.Trivia.TrailingComments);
    }

    public void WriteValue(JsonValue value)
    {
        WriteComments(value.Trivia.LeadingComments);
        Append(value.StringValue);
        WriteComments(value.Trivia.TrailingComments);
    }

    public void Append(string str)
    {
        _sb.Append(str);
        // Console.WriteLine(_sb.ToString());
    }

    public void Write(JsonNode node)
    {
        if (node is JsonArray array)
            WriteArray(array);
        else if (node is JsonObject obj)
            WriteObject(obj);
        else if (node is JsonValue value)
            WriteValue(value);
        else
            throw new InvalidOperationException($"Invalid node type {node}");
    }

    private bool CanWriteInline(JsonNode array)
    {
        return !JsonNode.EnumerateAll(array)
            .Any(e =>
                (e.Trivia.LeadingComments?.Any(e => e.WillBreakLine) ?? false)
                || (e.Trivia.ContainedComments?.Any(e => e.WillBreakLine) ?? false)
                || (e.Trivia.TrailingComments?.Any(e => e.WillBreakLine) ?? false)
                || (e is JsonObject jo && jo.FormattingHint == FormattingHint.Multiline)
                || (e is JsonArray ja && ja.FormattingHint == FormattingHint.Multiline)
            );
    }
    
    public void WriteArray(JsonArray array)
    {
        if (array.FormattingHint == FormattingHint.Inline && CanWriteInline(array))
        {
            WriteArrayInline(array);
        }
        else if (!array.FormattingHint.HasValue || array.FormattingHint == FormattingHint.Multiline)
        {
            WriteArrayMultiline(array);
        }
    }

    public void WriteArrayInline(JsonArray array)
    {
        WriteComments(array.Trivia.LeadingComments);
        Append("[");
        if(settings.SpacedBraces)
            Append(" ");
        for (int i = 0; i < array.Items.Count; i++)
        {
            var isLast = i == array.Items.Count - 1;
            var item = array.Items[i];
            WriteComments(item.Trivia.LeadingComments, true);
            Write(item);
            WriteComments(item.Trivia.TrailingComments);
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
        WriteComments(array.Trivia.TrailingComments);
    }

    public void WriteArrayMultiline(JsonArray array)
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
            Write(item);
            WriteComments(item.Trivia.TrailingComments);
            if (!isLast)
            {
                Append(",");
                var next = array.Items[i + 1];
                WriteComments(next.Trivia.LeadingComments);
                Append(Environment.NewLine);
            }
            else if (settings.WriteTrailingCommas)
            {
                Append(",");
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