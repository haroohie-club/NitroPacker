using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace HaroohieClub.NitroPacker.Patcher.Overlay
{
    public class Overlay
    {
        public string Name { get; set; }
        public int Id { get => int.Parse(Name[^4..], System.Globalization.NumberStyles.HexNumber); }
        public List<byte> Data { get; set; }
        public uint Address { get; set; }
        public int Length => Data.Count;

        public Overlay(string file, string romInfoPath)
        {
            XDocument romInfo = XDocument.Load(romInfoPath);

            Name = Path.GetFileNameWithoutExtension(file);
            Data = File.ReadAllBytes(file).ToList();
            // Every overlay seems to have functions that write an integer directly after the end of the overlay.
            // This ensures that when we begin appending, we don't have any code get overwritten.
            Data.AddRange(new byte[4]);

            var overlayTableEntry = romInfo.Root.Element("RomInfo").Element("ARM9Ovt").Elements()
                .First(o => o.Attribute("Id").Value == $"{Id}");
            Address = uint.Parse(overlayTableEntry.Element("RamAddress").Value);
        }

        public void Save(string file)
        {
            File.WriteAllBytes(file, Data.ToArray());
        }

        public void Patch(uint address, byte[] patchData)
        {
            int loc = (int)(address - Address);
            Data.RemoveRange(loc, patchData.Length);
            Data.InsertRange(loc, patchData);
        }

        public void Append(byte[] appendData, string ndsProjectFile)
        {
            Data.AddRange(appendData);
            XDocument ndsProjectFileDocument = XDocument.Load(ndsProjectFile);
            Console.WriteLine($"Expanding RAM size in overlay table for overlay {Id}...");
            var overlayTableEntry = ndsProjectFileDocument.Root.Element("RomInfo").Element("ARM9Ovt").Elements()
                .First(o => o.Attribute("Id").Value == $"{Id}");
            overlayTableEntry.Element("RamSize").Value = $"{Data.Count}";
            ndsProjectFileDocument.Save(ndsProjectFile);
        }
    }
}
