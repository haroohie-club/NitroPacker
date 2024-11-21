namespace HaroohieClub.NitroPacker.Nitro.Fs;

/// <summary>
/// A class used to associate a file with its file allocation table offset
/// </summary>
public class NameEntryWithFatEntry
{
    /// <summary>
    /// The path to the file
    /// </summary>
    public string Path { get; set; }
    /// <summary>
    /// The FAT offset of the file
    /// </summary>
    public uint FatOffset { get; set; }

    /// <summary>
    /// Empty constructor for serialization
    /// </summary>
    public NameEntryWithFatEntry()
    {
    }
}

/// <summary>
/// A class used to associated a file/FAT entry with its data
/// </summary>
public class NameFatWithData
{
    /// <summary>
    /// The <see cref="NameEntryWithFatEntry"/> associated with this data
    /// </summary>
    public NameEntryWithFatEntry NameFat { get; set; }
    /// <summary>
    /// The binary data of this file
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// Empty constructor for serialization
    /// </summary>
    public NameFatWithData()
    {
    }

    /// <summary>
    /// Constructs an entry with just file data (<see cref="NameFat"/> can be initialized later)
    /// </summary>
    /// <param name="data"></param>
    public NameFatWithData(byte[] data)
    {
        Data = data;
    }
    
    /// <summary>
    /// Constructs an entry with its arguments
    /// </summary>
    /// <param name="nameFat">The <see cref="NameEntryWithFatEntry"/></param>
    /// <param name="data">The file data for that file</param>
    public NameFatWithData(NameEntryWithFatEntry nameFat, byte[] data)
    {
        NameFat = nameFat;
        Data = data;
    }
}