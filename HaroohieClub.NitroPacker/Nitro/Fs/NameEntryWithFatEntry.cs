namespace HaroohieClub.NitroPacker.Nitro.Fs;

public class NameEntryWithFatEntry
{
    public string Path { get; set; }
    public uint FatOffset { get; set; }

    public NameEntryWithFatEntry()
    {
    }
}

public class NameFatWithData
{
    public NameEntryWithFatEntry NameFat { get; set; }
    public byte[] Data { get; set; }

    public NameFatWithData()
    {
    }

    public NameFatWithData(byte[] data)
    {
        Data = data;
    }

    public NameFatWithData(NameEntryWithFatEntry nameFat, byte[] data)
    {
        NameFat = nameFat;
        Data = data;
    }
}