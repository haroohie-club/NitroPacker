using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HaroohieClub.NitroPacker.Nitro.Card;

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
    /// <param name="projectPath">The path to the JSON project file produced by NitroPacker on unpack</param>
    public Overlay(string file, string projectPath)
    {
        Name = Path.GetFileNameWithoutExtension(file);
        _data = File.ReadAllBytes(file).ToList();
        // Every overlay seems to have functions that write an integer directly after the end of the overlay.
        // This ensures that when we begin appending, we don't have any code get overwritten.
        _data.AddRange(new byte[4]);
        
        NdsProjectFile project = JsonSerializer.Deserialize<NdsProjectFile>(File.ReadAllText(projectPath));
        RomOverlayTable overlay = project.RomInfo.ARM9Ovt.First(o => o.Id == Id);
        Address = overlay.RamAddress;
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

    internal void Append(byte[] appendData, string projectPath)
    {
        _data.AddRange(appendData);
        NdsProjectFile project = JsonSerializer.Deserialize<NdsProjectFile>(File.ReadAllText(projectPath));
        Console.WriteLine($"Expanding RAM size in overlay table for overlay {Id}...");
        project.RomInfo.ARM9Ovt.First(o => o.Id == Id).RamSize = (uint)_data.Count;
        File.WriteAllText(projectPath, JsonSerializer.Serialize(project));
    }
}