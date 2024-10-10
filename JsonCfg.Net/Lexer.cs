using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace JsonCfgNet;

internal enum TokenKind
{
    CURLY_OPEN,
    CURLY_CLOSE,
    COLON,
    STRING,
    NUMBER,
    ARRAY_OPEN,
    ARRAY_CLOSE,
    COMMA,
    BOOLEAN,
    NULL,
    COMMENT,
    INLINE_COMMENT
}
internal record Token(TokenKind Kind, string Value, Position Start, Position End)
{
    public bool IsCommentToken() => Kind == TokenKind.COMMENT || Kind == TokenKind.INLINE_COMMENT;

    public override string ToString() => $"{Kind} : {Value}";
}

internal record Position(int offset, int Line);

internal class Lexer
{
    public string TryParseEscapeSequence(string input, int pos)
    {
        var c = input[pos];
        if (c == '"' || c == '\\' || c == '/' || c == 'b' || c == 'f' || c == 'n' || c == 'r' || c == 't')
        {
            return c.ToString();
        }
        else if (c == 'u')
        {
            pos++;
            var i = 0;
            var sb = new StringBuilder();
            while (pos < input.Length && i < 4)
            {
                c = input[pos];
                if (char.IsDigit(c))
                {
                    pos++;
                    sb.Append(c);
                }
                else
                {
                    throw new JsonCfgException($"Unexpected character '{c}' at position {pos}, expected hexadecimal digit for unicode escape sequence");
                }
            }
            return sb.ToString();
        }
        else
        {
            throw new JsonCfgException($"Unexpected character '{c}' at position {pos}, expected escape sequence");
        }
    }

    public TokenStream Tokenize(string input)
        => new TokenStream(EnumerateTokens(input).GetEnumerator());
        
    private IEnumerable<Token> EnumerateTokens(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentNullException(nameof(input));
            
        var line = 0;
        var pos = 0;
        while (pos < input.Length)
        {
            var current = input[pos];

            if (current == '{')
            {
                yield return new Token(
                    TokenKind.CURLY_OPEN, 
                    "{", 
                    new Position(pos, line), 
                    new Position(pos + 1, line)
                );
                pos++;
            }
            else if (current == '}')
            {
                yield return new Token(
                    TokenKind.CURLY_CLOSE,
                    "}",
                    new Position(pos, line),
                    new Position(pos + 1, line)
                );
                pos++;
            }
            else if (current == '[')
            {
                yield return new Token(
                    TokenKind.ARRAY_OPEN,
                    "[",
                    new Position(pos, line),
                    new Position(pos + 1, line)
                );
                pos++;
            }
            else if (current == ']')
            {
                yield return new Token(
                    TokenKind.ARRAY_CLOSE,
                    "]",
                    new Position(pos, line),
                    new Position(pos + 1, line)
                );
                pos++;
            }
            else if (current == ':')
            {
                yield return new Token(
                    TokenKind.COLON,
                    ":",
                    new Position(pos, line),
                    new Position(pos + 1, line)
                );
                pos++;
            }
            else if (current == ',')
            {
                yield return new Token(
                    TokenKind.COMMA,
                    ",",
                    new Position(pos, line),
                    new Position(pos + 1, line)
                );
                pos++;
            }
            else if (current == '/')
            {
                if (input[pos + 1] == '/')
                {
                    var start = new Position(pos, line);
                    pos += 2;
                    var sb = new StringBuilder("//");
                    var finished = false;
                    while (pos < input.Length)
                    {
                        var c = input[pos];
                        sb.Append(c);
                        if(c == '\n')
                        {
                            finished = true;
                            break;
                        }
                        pos++;
                    }
                    var end = new Position(pos, line);
                    if(!finished)
                        throw new JsonCfgException($"Unterminated comment at pos {pos}");
                    yield return new Token(TokenKind.COMMENT, sb.ToString(), start, end);
                }
                else if (input[pos + 1] == '*')
                {
                    var start = new Position(pos, line);
                    pos += 2;
                    var sb = new StringBuilder("/*");
                    var finished = false;
                    while (pos < input.Length)
                    {
                        if (input[pos] == '*' && input[pos + 1] == '/')
                        {
                            pos += 2;
                            sb.Append("*/");
                            finished = true;
                            break;
                        }
                        sb.Append(input[pos]);
                        pos++;
                    }
                    var end = new Position(pos, line);
                    if(!finished)
                        throw new JsonCfgException($"Unterminated comment at pos {pos}");
                    yield return new Token(TokenKind.INLINE_COMMENT, sb.ToString(), start, end);
                }
                else
                {
                    throw new JsonCfgException($"Unexpected character '{input[pos + 1]}' at position {pos + 1}, expected '/' or '*' for comment");
                }
            }
            else if (current == '"')
            {
                var sb = new StringBuilder();
                sb.Append(current);
                var start = new Position(pos, line);
                
                pos++;
                bool finished = false;
                while (pos < input.Length)
                {
                    current = input[pos];
                    sb.Append(current);
                    pos++;
                    if (current == '\\')
                    {
                        var escape = TryParseEscapeSequence(input, pos);
                        sb.Append(escape);
                        pos += escape.Length;
                    }
                    else if (current == '"')
                    {
                        finished = true;
                        break;
                    }
                    else if (current == '\n')
                    {
                        throw new JsonCfgException($"Unterminated string node at pos {pos}");
                    }
                }
                var end = new Position(pos, line);
                if (!finished)
                    throw new JsonCfgException($"Unterminated string node at pos {pos}");

                yield return new Token(TokenKind.STRING, sb.ToString(), start, end);
            }
            else if (current == '-' || char.IsDigit(current) || current == '.' || current == 'e' || current == 'E')
            {
                var sb = new StringBuilder();
                sb.Append(current);
                var start = new Position(pos, line);
                pos++;
                while (pos < input.Length)
                {
                    current = input[pos];
                    if (current == '-' || char.IsDigit(current) || current == '.' || current == 'e' || current == 'E')
                    {
                        sb.Append(current);
                    }
                    else
                    {
                        break;
                    }
                    pos++;
                }
                var end = new Position(pos, line);
                var content = sb.ToString();
                if (content.Contains('e') || content.Contains('E'))
                {
                    if (!decimal.TryParse(content, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
                        throw new JsonCfgException($"Invalid number format {content} at pos {pos}");
                }
                else
                {
                    if (!decimal.TryParse(content, System.Globalization.CultureInfo.InvariantCulture, out _))
                        throw new JsonCfgException($"Invalid number format {content} at pos {pos}");
                }
                yield return new Token(TokenKind.NUMBER, content, start, end);
            }
            else if (current == 't')
            {
                var peek = input.SubstringSafe(pos, 4);
                if (peek == "true")
                {
                    var start = new Position(pos, line);
                    pos += 4;
                    yield return new Token(
                        TokenKind.BOOLEAN, 
                        "true", 
                        start, 
                        new Position(pos, line)
                    );
                }
                else
                {
                    throw new JsonCfgException($"Unexpected token {peek}, expected \"true\"");
                }
            }
            else if (current == 'f')
            {
                var peek = input.SubstringSafe(pos, 5);
                if (peek == "false")
                {
                    var start = new Position(pos, line);
                    pos += 5;
                    yield return new Token(
                        TokenKind.BOOLEAN, 
                        "false",
                        start,
                        new Position(pos, line)
                    );
                }
                else
                {
                    throw new JsonCfgException($"Unexpected token {peek}, expected \"false\" at pos {pos}");
                }
            }
            else if (current == 'n')
            {
                var peek = input.SubstringSafe(pos, 4);
                if (peek == "null")
                {
                    var start = new Position(pos, line);
                    pos += 4;
                    yield return new Token(
                        TokenKind.NULL,
                        "null",
                        start,
                        new Position(pos, line)
                    );
                }
                else
                {
                    throw new JsonCfgException($"Unexpected token {peek}, expected \"null\" at pos {pos}");
                }
            }
            else if(current == '\n')
            {
                pos++;
                line++;
            }
            else if (current == '\t' || current == ' ' || current == '\r')
            {
                pos++;
            }
            else
            {
                throw new Exception($"Unexpected character '{current}' at position {pos}");
            }
        }
    }
}

internal class TokenStream
{
    private readonly IEnumerator<Token> _enumerator;
    private Token? _current = null;
    private Token? _last = null;
    public Token? Last => _last;
    
    public TokenStream(IEnumerator<Token> stream)
    {
        _enumerator = stream;
    }

    public Token Current => _current ?? throw new InvalidOperationException("GetNextToken() has never been called");
    
    public Token GetNextToken()
    {
        if(!TryGetNextToken(out var token))
            throw new JsonCfgException("Unexpected end of input token stream");
        return token;
    }
    
    // public Token PeekUntil(Func<Token, bool> predicate)
    // {
    //     Token token;
    //     var count = 0;
    //     do
    //     {
    //         token = Peek(count);
    //         count++;
    //     }while(!predicate(token));
    //     return token;
    // }
    
    // public Token Peek(int count = 1)
    // {
    //     if(count == 0)
    //         throw new ArgumentOutOfRangeException(nameof(count));
    //     if(count <= _peak.Count)
    //         return _peak.ElementAt(count - 1);
            
    //     Token? token = null;
    //     for(int i=_peak.Count; i<count; i++)
    //     {
    //         token = _enumerator.GetNextToken();
    //         _peak.Enqueue(token);
    //     }
    //     return token!;
    // }
    
    public bool TryGetNextToken([NotNullWhen(true)] out  Token? token)
    {
        if(_enumerator.TryGetNext(out token))
        {
            _last = _current;
            _current = token; 
            return true;
        }
        return false;
    }
    
    
    
    
}