
using System.Collections.Generic;
using System.Linq;

public static class TokenListExt
{
	public static CharType GetTypeAt(this IList<Token> tokens, int index)
	{
		if (index >= 0 && index < tokens.Count)
		{
			return tokens[index].Type;
		}
		return CharType.Unknown;
	}
}


public static class IEnumerableExtensions
{
	public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
	   => self.Select((item, index) => (item, index));
}
