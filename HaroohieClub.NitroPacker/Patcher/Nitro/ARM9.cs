// This code is heavily based on code Gericom wrote for ErmiiBuild

using System;
using System.Collections.Generic;
using System.Linq;

namespace HaroohieClub.NitroPacker.Patcher.Nitro;

/// <summary>
/// Object representing the ARM9 binary
/// </summary>
public class ARM9
{
    private readonly uint _ramAddress;
    private readonly List<byte> _staticData;
    private readonly uint _start_ModuleParamsOffset;
    private readonly CRT0.ModuleParams _start_ModuleParams;

    private readonly List<CRT0.AutoLoadEntry> _autoLoadList;

    /// <summary>
    /// Create an ARM9 object from binary data in memory (the module params offset is dynamically determined)
    /// </summary>
    /// <param name="data">The ARM9 binary data</param>
    /// <param name="ramAddress">The address at which the ARM9 binary is loaded into RAM</param>
    public ARM9(byte[] data, uint ramAddress)
        : this(data, ramAddress, FindModuleParams(data)) { }

    /// <summary>
    /// Create an ARM9 object from binary data in memory while specifying the module params offset
    /// </summary>
    /// <param name="data">The ARM9 binary data</param>
    /// <param name="ramAddress">The address at which the ARM9 binary is loaded into RAM</param>
    /// <param name="moduleParamsOffset">The offset in the data where the module params are located</param>
    public ARM9(byte[] data, uint ramAddress, uint moduleParamsOffset)
    {
        //Unimportant static footer! Use it for _start_ModuleParamsOffset and remove it.
        if (BitConverter.ToUInt32(data.Skip(data.Length - 0x0C).Take(4).ToArray()) == 0xDEC00621)
        {
            moduleParamsOffset = BitConverter.ToUInt32(data.Skip(data.Length - 8).Take(4).ToArray());
            data = data.Take(data.Length - 0x0C).ToArray();
        }

        _ramAddress = ramAddress;
        _start_ModuleParamsOffset = moduleParamsOffset;
        _start_ModuleParams = new(data, moduleParamsOffset);
        if (_start_ModuleParams.CompressedStaticEnd != 0)
        {
            _start_ModuleParams = new(data, moduleParamsOffset);
        }

        _staticData = data.Take((int)(_start_ModuleParams.AutoLoadStart - ramAddress)).ToList();

        _autoLoadList = [];
        uint nr = (_start_ModuleParams.AutoLoadListEnd - _start_ModuleParams.AutoLoadListOffset) / 0xC;
        uint offset = _start_ModuleParams.AutoLoadStart - ramAddress;
        for (int i = 0; i < nr; i++)
        {
            var entry = new CRT0.AutoLoadEntry(data, _start_ModuleParams.AutoLoadListOffset - ramAddress + (uint)i * 0xC);
            entry.Data = data.Skip((int)offset).Take((int)entry.Size).ToList();
            _autoLoadList.Add(entry);
            offset += entry.Size;
        }
    }

    /// <summary>
    /// Get the raw ARM9 binary
    /// </summary>
    /// <returns>A byte array of the ARM9 binary data</returns>
    public byte[] GetBytes()
    {
        List<byte> bytes = [];
        bytes.AddRange(_staticData);
        _start_ModuleParams.AutoLoadStart = (uint)bytes.Count + _ramAddress;
        foreach (CRT0.AutoLoadEntry autoLoad in _autoLoadList)
        {
            bytes.AddRange(autoLoad.Data);
        }
        _start_ModuleParams.AutoLoadListOffset = (uint)bytes.Count + _ramAddress;
        foreach (CRT0.AutoLoadEntry autoLoad in _autoLoadList)
        {
            bytes.AddRange(autoLoad.GetEntryBytes());
        }
        _start_ModuleParams.AutoLoadListEnd = (uint)bytes.Count + _ramAddress;
        List<byte> moduleParamsBytes = _start_ModuleParams.GetBytes();
        bytes.RemoveRange((int)_start_ModuleParamsOffset, moduleParamsBytes.Count);
        bytes.InsertRange((int)_start_ModuleParamsOffset, moduleParamsBytes);
        return bytes.ToArray();
    }

    internal void AddAutoLoadEntry(uint address, byte[] data)
    {
        _autoLoadList.Add(new(address, data));
    }

    internal void WriteBytes(uint address, byte[] bytes)
    {
        _staticData.RemoveRange((int)(address - _ramAddress), bytes.Length);
        _staticData.InsertRange((int)(address - _ramAddress), bytes);
    }

    internal bool WriteU16LE(uint address, ushort value)
    {
        if (address > _ramAddress && address < _start_ModuleParams.AutoLoadStart)
        {
            _staticData.RemoveRange((int)(address - _ramAddress), 2);
            _staticData.InsertRange((int)(address - _ramAddress), BitConverter.GetBytes(value));
            return true;
        }
        foreach (CRT0.AutoLoadEntry v in _autoLoadList)
        {
            if (address > v.Address && address < (v.Address + v.Size))
            {
                v.Data.RemoveRange((int)(address - _ramAddress), 2);
                v.Data.InsertRange((int)(address - _ramAddress), BitConverter.GetBytes(value));
                return true;
            }
        }
        return false;
    }

    internal uint ReadU32LE(uint address)
    {
        if (address > _ramAddress && address < _start_ModuleParams.AutoLoadStart)
        {
            return BitConverter.ToUInt32(_staticData.ToArray(), (int)(address - _ramAddress));
        }
        foreach (CRT0.AutoLoadEntry v in _autoLoadList)
        {
            if (address > v.Address && address < (v.Address + v.Size))
            {
                return BitConverter.ToUInt32(v.Data.ToArray(), (int)(address - v.Address));
            }
        }
        return 0xFFFFFFFF;
    }

    internal bool WriteU32LE(uint address, uint value)
    {
        if (address > _ramAddress && address < _start_ModuleParams.AutoLoadStart)
        {
            _staticData.RemoveRange((int)(address - _ramAddress), 4);
            _staticData.InsertRange((int)(address - _ramAddress), BitConverter.GetBytes(value));
            return true;
        }
        foreach (CRT0.AutoLoadEntry v in _autoLoadList)
        {
            if (address > v.Address && address < (v.Address + v.Size))
            {
                v.Data.RemoveRange((int)(address - _ramAddress), 4);
                v.Data.InsertRange((int)(address - _ramAddress), BitConverter.GetBytes(value));
                return true;
            }
        }
        return false;
    }

    private static uint FindModuleParams(byte[] data)
    {
        return (uint)(data.IndexOfSequence(new byte[] { 0x21, 0x06, 0xC0, 0xDE, 0xDE, 0xC0, 0x06, 0x21 }) - 0x1C);
    }
}