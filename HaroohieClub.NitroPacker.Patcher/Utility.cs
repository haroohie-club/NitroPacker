using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HaroohieClub.NitroPacker.Patcher
{
    internal static class Utility
    {
        public static int IndexOfSequence<T>(this IEnumerable<T> items, IEnumerable<T> search)
        {
            int searchLength = search.Count();
            int lastIndex = items.Count() - searchLength;
            for (int i = 0; i < lastIndex; i++)
            {
                if (items.Skip(i).Take(searchLength).SequenceEqual(search))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
