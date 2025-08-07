using System.Collections.Generic;
using System.Linq;

namespace HaroohieClub.NitroPacker.Patcher;

/// <summary>
/// Enum representing the different build system types supported by the NitroPacker patcher
/// </summary>
public enum BuildType
{
    /// <summary>
    /// The make-based build system using devkitARM and make
    /// </summary>
    Make,
    /// <summary>
    /// The Docker-based build system using Docker (and WSL on Windows)
    /// </summary>
    Docker,
    /// <summary>
    /// The ninja-based build system using ninja and clang
    /// </summary>
    Ninja,
    /// <summary>
    /// Indicates no build system was specified (will error)
    /// </summary>
    NotSpecified,
}

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
}