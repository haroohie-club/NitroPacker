using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HaroohieClub.NitroPacker.Patcher;

internal static class Utility
{
    public static int IndexOfSequence<T>(this IEnumerable<T> items, IEnumerable<T> search)
    {
        IEnumerable<T> searchArray = search as T[] ?? search.ToArray();
        int searchLength = searchArray.Count();
        IEnumerable<T> itemsArray = items as T[] ?? items.ToArray();
        int lastIndex = itemsArray.Count() - searchLength;
        for (int i = 0; i < lastIndex; i++)
        {
            if (itemsArray.Skip(i).Take(searchLength).SequenceEqual(searchArray))
            {
                return i;
            }
        }
        return -1;
    }

    public static string PathCombineAgnostic(params string[] paths)
    {
        return Path.Combine(paths).Replace('\\', '/');
    }

    public static string PathRelativeAgnostic(string path1, string path2)
    {
        return Path.GetRelativePath(path1, path2).Replace('\\', '/');
    }
}