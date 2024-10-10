namespace JsonCfgNet;

internal class Parser
{
    private Parser(){}
    public static Parser Instance = new();

    private readonly Lexer _lexer = new();
    public JsonNode Parse(string input)
    {
        var stream = _lexer.Tokenize(input);
        stream.GetNextToken();
        return ParseNode(stream);
    }

    private JsonNode ParseNode(TokenStream stream)
    {
        var comments = MaybeParseComments(stream);
        var token = stream.Current;
        var result = token.Kind switch
        {
            TokenKind.CURLY_OPEN => (JsonNode)ParseObject(stream),
            TokenKind.ARRAY_OPEN => ParseArray(stream),
            TokenKind.STRING => ParseValue(stream),
            TokenKind.NUMBER => ParseValue(stream),
            TokenKind.BOOLEAN => ParseValue(stream),
            TokenKind.NULL => ParseValue(stream),
            _ => throw new JsonCfgException($"Unexpected token: {token.Kind}"),
        };
        result.Trivia.LeadingComments = comments;
        return result;
    }
    
    private JsonValue ParseValue(TokenStream stream)
    {
        var token = stream.Current;
        var result = token.Kind switch {
            TokenKind.STRING => new JsonValue(JsonValueKind.String, token.Value),
            TokenKind.NUMBER => new JsonValue(JsonValueKind.Number, token.Value),
            TokenKind.BOOLEAN => new JsonValue(JsonValueKind.Boolean, token.Value),
            TokenKind.NULL => new JsonValue(JsonValueKind.Null, token.Value),
            _ => throw new JsonCfgException($"Unexpected token: {token.Kind}")
        };
        stream.GetNextToken();
        var trailingComments = MaybeParseComments(stream);
        result.Trivia.TrailingComments = trailingComments;
        return result;
    }
    
    private List<JsonComment>? MaybeParseComments(TokenStream stream)
    {
        if(!stream.Current.IsCommentToken())
            return null;
        var res = new List<JsonComment>();
        while(stream.Current.IsCommentToken())
        {
            var comment = new JsonComment
            {
                CommentKind = stream.Current.Kind switch
                {
                    TokenKind.COMMENT => JsonCommentKind.Line,
                    TokenKind.INLINE_COMMENT => JsonCommentKind.Inline,
                    _ => throw new JsonCfgException($"Unexpected token: {stream.Current.Kind}")
                },
                Comment = stream.Current.Value!,
                LeadingNewline = stream.Last != null 
                    && stream.Last.End.Line != stream.Current.Start.Line
            };
            res.Add(comment);
            stream.GetNextToken();
        }
        return res;
    }
    
    private JsonProperty ParseJsonProperty(TokenStream stream)
    {
        var token = stream.Current;
        if (token.Kind != TokenKind.STRING)
            throw new JsonCfgException($"Unexpected token: {token.Kind} expected property key");
        var key = token.Value!;
        
        stream.GetNextToken();
        var trailingComments = MaybeParseComments(stream);
        token = stream.Current;
        if (token.Kind != TokenKind.COLON)
            throw new JsonCfgException($"Unexpected token: {token.Kind} expected colon");
            
        stream.GetNextToken();
        var value = ParseNode(stream);
        
        var prop = new JsonProperty
        {
            Key = key[1..^1],
            Value = value
        };
        prop.Trivia.TrailingComments = trailingComments; //trailing comments of JsonProperty are the comments between key and colon
        return prop;
    }

    private JsonObject ParseObject(TokenStream stream)
    {
        var res = new JsonObject();
        var startToken = stream.Current;
        
        var token = stream.Current;
        if(token.Kind != TokenKind.CURLY_OPEN)
            throw new JsonCfgException($"Unexpected token: {token.Kind} expected open braces");
        
        stream.GetNextToken();
        var comments = MaybeParseComments(stream);
        token = stream.Current;
        
        while (token.Kind != TokenKind.CURLY_CLOSE)
        {
            var prop = ParseJsonProperty(stream);
            if(comments != null)
            {
                prop.Trivia.LeadingComments = comments;
                comments = null;
            }
                
            token = stream.Current;
            res.Properties.Add(prop);
            if (token.Kind == TokenKind.COMMA)
            {
                prop.HasTrailingComma = true;
                stream.GetNextToken();
                comments = MaybeParseComments(stream);
                token = stream.Current;
            }
            else if (token.Kind != TokenKind.CURLY_CLOSE)
            {
                throw new JsonCfgException($"Unexpected token: {token.Kind} expected comma or close braces");
            }
        }
        
        if(comments != null)
            res.Trivia.ContainedComments = comments;
            
        stream.TryGetNextToken(out _);
        res.Trivia.TrailingComments = MaybeParseComments(stream);
        if(token.End.Line == startToken.Start.Line)
        {
            res.FormattingHint = FormattingHint.Inline;
        }
        return res;
    }

    private JsonArray ParseArray(TokenStream stream)
    {
        var res = new JsonArray();
        var startToken = stream.Current;
        var token = stream.Current;
        if(token.Kind != TokenKind.ARRAY_OPEN)
            throw new JsonCfgException($"Unexpected token: {token.Kind} expected open braces");
        
        stream.GetNextToken();
        var comments = MaybeParseComments(stream);
        token = stream.Current;
        
        while (token.Kind != TokenKind.ARRAY_CLOSE)
        {
            var prop = ParseNode(stream);
            if(comments != null)
                prop.Trivia.LeadingComments = comments;
                
            token = stream.Current;
            res.Items.Add(prop);
            if (token.Kind == TokenKind.COMMA)
            {
                stream.GetNextToken();
                comments = MaybeParseComments(stream);
                token = stream.Current;
            }
            else if (token.Kind != TokenKind.ARRAY_CLOSE)
            {
                throw new JsonCfgException($"Unexpected token: {token.Kind} expected comma or close braces");
            }
        }
        
        if(comments != null)
            res.Trivia.ContainedComments = comments;
            
        stream.TryGetNextToken(out _);
        res.Trivia.TrailingComments = MaybeParseComments(stream);
        
        if(token.End.Line == startToken.Start.Line)
        {
            res.FormattingHint = FormattingHint.Inline;
        }
        return res;
    }
}
