using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace HaroohieClub.NitroPacker.Patcher.Overlay;

/// <summary>
/// An object representing an overlay binary
/// </summary>
public class Overlay
{
    /// <summary>
    /// The name of the overlay as designated by NitroPacker
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// The integer ID of the overlay (e.g. main_0001's ID is 1)
    /// </summary>
    public int Id { get => int.Parse(Name[^4..], System.Globalization.NumberStyles.HexNumber); }
    private List<byte> _data;
    /// <summary>
    /// The address at which the overlay is loaded into memory
    /// </summary>
    public uint Address { get; }
    /// <summary>
    /// The size of the overlay in bytes
    /// </summary>
    public int Length => _data.Count;

    /// <summary>
    /// Creates an overlay
    /// </summary>
    /// <param name="file">The file to load the overlay from</param>
    /// <param name="romInfoPath">The path to the rominfo XML file produced by NitroPacker on unpack</param>
    public Overlay(string file, string romInfoPath)
    {
        XDocument romInfo = XDocument.Load(romInfoPath);

        Name = Path.GetFileNameWithoutExtension(file);
        _data = File.ReadAllBytes(file).ToList();
        // Every overlay seems to have functions that write an integer directly after the end of the overlay.
        // This ensures that when we begin appending, we don't have any code get overwritten.
        _data.AddRange(new byte[4]);

        var overlayTableEntry = romInfo.Root.Element("RomInfo").Element("ARM9Ovt").Elements()
            .First(o => o.Attribute("Id").Value == $"{Id}");
        Address = uint.Parse(overlayTableEntry.Element("RamAddress").Value);
    }

    /// <summary>
    /// Saves the overlay to a file
    /// </summary>
    /// <param name="file">The file to save the overlay to</param>
    public void Save(string file)
    {
        File.WriteAllBytes(file, _data.ToArray());
    }

    internal void Patch(uint address, byte[] patchData)
    {
        int loc = (int)(address - Address);
        _data.RemoveRange(loc, patchData.Length);
        _data.InsertRange(loc, patchData);
    }

    internal void Append(byte[] appendData, string ndsProjectFile)
    {
        _data.AddRange(appendData);
        XDocument ndsProjectFileDocument = XDocument.Load(ndsProjectFile);
        Console.WriteLine($"Expanding RAM size in overlay table for overlay {Id}...");
        var overlayTableEntry = ndsProjectFileDocument.Root.Element("RomInfo").Element("ARM9Ovt").Elements()
            .First(o => o.Attribute("Id").Value == $"{Id}");
        overlayTableEntry.Element("RamSize").Value = $"{_data.Count}";
        ndsProjectFileDocument.Save(ndsProjectFile);
    }
}