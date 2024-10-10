using System.Diagnostics.CodeAnalysis;

namespace JsonCfgNet;

internal static class Extensions
{
    public static string SubstringSafe(this string str, int startIndex, int length)
    {
        if (startIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));
        var end = Math.Min(startIndex + length, str.Length);
        return str.Substring(startIndex, end - startIndex);
    }
    
    public static bool TryGetNext(this IEnumerator<Token> enumerator, [NotNullWhen(true)] out Token? token)
    {
        if (enumerator.MoveNext())
        {
            token = enumerator.Current;
            return true;
        }
        token = null;
        return false;
    }

    public static Token GetNextToken(this IEnumerator<Token> enumerator)
    {
        if (!enumerator.MoveNext())
            throw new JsonCfgException("Unexpected end of input token stream");
        return enumerator.Current;
    }
}
