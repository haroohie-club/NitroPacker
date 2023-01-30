using HaroohieClub.NitroPacker.IO;
using System;

namespace HaroohieClub.NitroPacker.Nitro.Card
{
    public class Blowfish
    {
        public const int KeyTableLength = 0x1048;

        public const int PTableEntryCount = 18;
        public const int SBoxCount = 4;
        public const int SBoxEntryCount = 256;

        private readonly uint[] _pTable;
        private readonly uint[][] _sBoxes;

        public Blowfish(uint[] pTable, uint[][] sBoxes)
        {
            if (pTable.Length != PTableEntryCount)
                throw new ArgumentException($"Size of p table should be {PTableEntryCount}", nameof(pTable));
            if (sBoxes.Length != SBoxCount)
                throw new ArgumentException($"Number of s boxes should be {SBoxCount}", nameof(sBoxes));
            for (int i = 0; i < SBoxCount; i++)
                if (sBoxes[i].Length != SBoxEntryCount)
                    throw new ArgumentException($"Size of s box {i} should be {SBoxEntryCount}", nameof(sBoxes));

            _pTable = pTable;
            _sBoxes = sBoxes;
        }

        public Blowfish(ReadOnlySpan<byte> keyTable)
        {
            if (keyTable == null)
                throw new ArgumentNullException(nameof(keyTable));
            if (keyTable.Length < KeyTableLength)
                throw new ArgumentException(nameof(keyTable));
            _pTable = IOUtil.ReadU32Le(keyTable, PTableEntryCount);
            _sBoxes = new uint[SBoxCount][];
            _sBoxes[0] = IOUtil.ReadU32Le(keyTable[0x48..], SBoxEntryCount);
            _sBoxes[1] = IOUtil.ReadU32Le(keyTable[0x448..], SBoxEntryCount);
            _sBoxes[2] = IOUtil.ReadU32Le(keyTable[0x848..], SBoxEntryCount);
            _sBoxes[3] = IOUtil.ReadU32Le(keyTable[0xC48..], SBoxEntryCount);
        }

        public void Encrypt(byte[] data, int offset, int length)
            => Encrypt(data.AsSpan(offset, length));

        public void Encrypt(Span<byte> data)
        {
            if ((data.Length & 7) != 0)
                throw new ArgumentException(nameof(data));
            for (int i = 0; i < data.Length; i += 8)
            {
                ulong val = Encrypt(IOUtil.ReadU64Le(data[i..]));
                IOUtil.WriteU64Le(data[i..], val);
            }
        }

        public ulong Encrypt(ulong val)
        {
            uint y = (uint)(val & 0xFFFFFFFF);
            uint x = (uint)(val >> 32);
            for (int i = 0; i < 16; i++)
            {
                uint z = _pTable[i] ^ x;
                uint a = _sBoxes[0][z >> 24 & 0xFF];
                uint b = _sBoxes[1][z >> 16 & 0xFF];
                uint c = _sBoxes[2][z >> 8 & 0xFF];
                uint d = _sBoxes[3][z & 0xFF];
                x = d + (c ^ b + a) ^ y;
                y = z;
            }

            return x ^ _pTable[16] | (ulong)(y ^ _pTable[17]) << 32;
        }

        public void Decrypt(byte[] src, int srcOffset, int length, byte[] dst, int dstOffset)
            => Decrypt(src.AsSpan(srcOffset, length), dst.AsSpan(dstOffset, length));

        public void Decrypt(Span<byte> data)
            => Decrypt(data, data);

        public void Decrypt(ReadOnlySpan<byte> src, Span<byte> dst)
        {
            if ((src.Length & 7) != 0)
                throw new ArgumentException(nameof(src));
            if (dst.Length < src.Length)
                throw new ArgumentException(nameof(dst));
            for (int i = 0; i < src.Length; i += 8)
            {
                ulong val = Decrypt(IOUtil.ReadU64Le(src[i..]));
                IOUtil.WriteU64Le(dst[i..], val);
            }
        }

        public ulong Decrypt(ulong val)
        {
            uint y = (uint)(val & 0xFFFFFFFF);
            uint x = (uint)(val >> 32);
            for (int i = 17; i >= 2; i--)
            {
                uint z = _pTable[i] ^ x;
                uint a = _sBoxes[0][z >> 24 & 0xFF];
                uint b = _sBoxes[1][z >> 16 & 0xFF];
                uint c = _sBoxes[2][z >> 8 & 0xFF];
                uint d = _sBoxes[3][z & 0xFF];
                x = d + (c ^ b + a) ^ y;
                y = z;
            }

            return x ^ _pTable[1] | (ulong)(y ^ _pTable[0]) << 32;
        }
    }
}