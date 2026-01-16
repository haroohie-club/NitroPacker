using System;
using System.IO;
using System.Text;

namespace HaroohieClub.NitroPacker.IO.Compression;

// Decompression taken from https://github.com/OpenKH/OpenKh/blob/master/OpenKh.Ddd/Utils/BLZ.cs
// Licensed under Apache License 2.0: https://github.com/OpenKH/OpenKh/blob/master/LICENSE
// Compression taken from https://github.com/R-YaTian/TinkeDSi/blob/main/Plugins/DSDecmp/DSDecmp/Formats/LZOvl.c
// Licensed under GPL 3.0 https://github.com/R-YaTian/TinkeDSi/blob/main/LICENSE
internal class Blz
{
    public static byte[] Decompress(byte[] data)
    {
        MemoryStream stream = new(data);
        return Decompress(stream, data.Length);
    }

    public static byte[] Decompress(Stream stream, int fileLength, bool printWarnings = false)
    {
        if (fileLength < 8)
            //TODO: better exception type(s) throughout this function.
            throw new("Bad File Length!");

        byte[] packedBytes = new byte[fileLength];
        if (stream.Read(packedBytes) < fileLength)
        {
            throw new("Unexpected end of file!");
        }

        var memoryStream = new MemoryStream(packedBytes);
        var binaryReader = new BinaryReader(memoryStream, Encoding.ASCII, true);

        memoryStream.Seek(-4, SeekOrigin.End);
        int extraBytes = binaryReader.ReadInt32();
        if (extraBytes == 0)
        {
            // No compression, copy in to out, minus last 4 bytes
            byte[] outBuffer = new byte[fileLength - 4];
            Buffer.BlockCopy(packedBytes, 0, outBuffer, 0, fileLength - 4);
            return outBuffer;
        }

        memoryStream.Seek(-5, SeekOrigin.End);
        int headerLength = (int)binaryReader.ReadByte();
        memoryStream.Seek(-8, SeekOrigin.End);
        int encodedLength = binaryReader.ReadInt32() & 0x00FFFFFF;
        int keepLength = fileLength - encodedLength;
        int dataLength = encodedLength - headerLength;
        int unpackedLength = keepLength + encodedLength + extraBytes;

        byte[] unpackedBytes = new byte[unpackedLength];

        Buffer.BlockCopy(packedBytes, 0, unpackedBytes, 0, keepLength);

        byte[] packedData = new byte[dataLength];
        byte[] decompressedData = new byte[dataLength + extraBytes + headerLength];

        Buffer.BlockCopy(packedBytes, keepLength, packedData, 0, dataLength);
        Array.Reverse(packedData);
        var packedReader = new BinaryReader(new MemoryStream(packedData), Encoding.ASCII, true);

        memoryStream.Seek(-headerLength, SeekOrigin.End);
        byte mask = 0, flags = 0;
        int bytesToWrite = unpackedLength - keepLength;
        int bytesRead = 0, bytesWritten = 0;
        while (bytesWritten < bytesToWrite)
        {
            mask >>= 1;
            if (mask == 0)
            {
                if (bytesRead >= encodedLength)
                {
                    throw new("Unexpected end of data while reading flag byte!");
                }
                flags = packedReader.ReadByte();
                bytesRead++;
                mask = 0x80;
            }

            if ((flags & mask) > 0)
            {
                if (bytesRead + 1 >= encodedLength)
                {
                    throw new("Unexpected end of data while reading decompression token!");
                }

                ushort info = (ushort)((packedReader.ReadByte() << 8) | packedReader.ReadByte());
                bytesRead += 2;

                int length = (info >> 12) + 3;
                if (bytesWritten + length > bytesToWrite)
                {
                    if (printWarnings)
                    {
                        Console.WriteLine("WARN: Final decompression token longer than remaining bytes to be written. Output may be truncated.");
                    }
                    length = bytesToWrite - bytesWritten;
                }
                int displacement = (info & 0xFFF) + 3;
                Buffer.BlockCopy(decompressedData, bytesWritten - displacement, decompressedData, bytesWritten, length);
                bytesWritten += length;
            }
            else
            {
                if (bytesRead == encodedLength)
                {
                    throw new("Unexpected end of data while reading literal byte!");
                }

                byte b = packedReader.ReadByte();
                bytesRead++;
                decompressedData[bytesWritten++] = b;
            }
        }

        Array.Reverse(decompressedData);
        Buffer.BlockCopy(decompressedData, 0, unpackedBytes, keepLength, bytesToWrite);

        return unpackedBytes;
    }

    public byte[] BLZ_Encode(byte[] raw_buffer, bool arm9)
    {
        byte[] pak_buffer, new_buffer;
        uint raw_len, pak_len, new_len;

        raw_len = (uint)raw_buffer.Length;

        pak_buffer = null;
        pak_len = BLZ_MAXIM + 1;

        new_buffer = BLZ_Code(raw_buffer, raw_len, out new_len, arm9);
        if (new_len < pak_len)
        {
            pak_buffer = new_buffer;
            pak_len = new_len;
        }

        if (pak_buffer.Length != pak_len)
        {
            byte[] retbuf = new byte[pak_len];
            for (int i = 0; i < pak_len; ++i)
            {
                retbuf[i] = pak_buffer[i];
            }
            pak_buffer = retbuf;
        }

        return pak_buffer;
    }

    private static void SEARCH(ref uint l, ref uint p, ref byte[] raw_buffer, ref uint raw, ref uint raw_end, ref uint max, ref uint pos, ref uint len)
    {
        l = BLZ_THRESHOLD;

        max = raw >= BLZ_N ? BLZ_N : raw;
        for (pos = 3; pos <= max; pos++)
        {
            for (len = 0; len < BLZ_F; len++)
            {
                if (raw + len == raw_end) break;
                if (len >= pos) break;
                if (raw_buffer[raw + len] != raw_buffer[raw + len - pos]) break;
            }

            if (len > l)
            {
                p = pos;
                if ((l = len) == BLZ_F) break;
            }
        }
    }

    public const uint BLZ_SHIFT = 1;          // bits to shift
    public const byte BLZ_MASK = 0x80;       // bits to check:
    // ((((1 << BLZ_SHIFT) - 1) << (8 - BLZ_SHIFT)

    public const uint BLZ_THRESHOLD = 2;          // max number of bytes to not encode
    public const uint BLZ_N = 0x1002;     // max offset ((1 << 12) + 2)
    public const uint BLZ_F = 0x12;       // max coded ((1 << 4) + BLZ_THRESHOLD)

    public const uint RAW_MINIM = 0x00000000; // empty file, 0 bytes
    public const uint RAW_MAXIM = 0x00FFFFFF; // 3-bytes length, 16MB - 1

    public const uint BLZ_MINIM = 0x00000004; // header only (empty RAW file)
    public const uint BLZ_MAXIM = 0x01400000; // 0x0120000A, padded to 20MB:

    private static bool lookAhead = false;
    /// <summary>
    /// Sets the flag that determines if "LZ-CUE" method should be used when compressing
    /// with the LZ-Ovl format. The default is false, which is what is used in the original
    /// implementation.
    /// </summary>
    public static bool LookAhead
    {
        set { lookAhead = value; }
    }

    private static void BLZ_Invert(byte[] buffer, uint start, uint length)
    {
        byte ch;
        uint bottom = start + length - 1;

        while (start < bottom)
        {
            ch = buffer[start];
            buffer[start++] = buffer[bottom];
            buffer[bottom--] = ch;
        }
    }

    private static byte[] Memory(int length, int size)
    {
        return new byte[length * size];
    }

    public byte[] BLZ_Code(byte[] raw_buffer, uint raw_len, out uint new_len, bool arm9)
    {
        byte[] pak_buffer;
        uint pak, raw, raw_end, flg = 0;
        byte[] tmp;
        uint pak_len, inc_len, hdr_len, enc_len, len = 0, pos = 0, max = 0;
        uint len_best = 0, pos_best = 0, len_next = 0, pos_next = 0, len_post = 0, pos_post = 0;
        uint pak_tmp, raw_tmp, raw_new;
        byte mask;
        uint bytes_saved = 0, total_bytes_saved = 0, best_total_saved = 0;
        uint best_pak_tmp = 0, best_raw_tmp = 0, best_flg = 0;

        pak_tmp = 0;
        raw_tmp = raw_len;

        pak_len = raw_len + ((raw_len + 7) / 8) + 11;
        pak_buffer = Memory((int)pak_len, 1);

        raw_new = raw_len;
        if (arm9)
        {
            if (raw_len < 0x4000)
            {
                Console.WriteLine("WARNING: ARM9 must be greater than 16KB, switch [arm9] disabled");
            }
            else
            {
                raw_new -= 0x4000;
            }
        }

        BLZ_Invert(raw_buffer, 0, raw_len);

        pak = 0;
        raw = 0;
        raw_end = raw_new;

        mask = 0;

        while (raw < raw_end)
        {
            mask = (byte)(((uint)mask) >> ((int)BLZ_SHIFT));

            if (mask == 0)
            {
                total_bytes_saved += bytes_saved;
                total_bytes_saved = total_bytes_saved > 0 ? total_bytes_saved - 1 : 0;
                if (total_bytes_saved > best_total_saved)
                {
                    best_total_saved = total_bytes_saved;
                    best_pak_tmp = pak_tmp;
                    best_raw_tmp = raw_tmp;
                    best_flg = flg;
                }
                flg = pak++;
                pak_buffer[flg] = 0;
                mask = BLZ_MASK;
                bytes_saved = 0;
            }

            SEARCH(ref len_best, ref pos_best, ref raw_buffer, ref raw, ref raw_end, ref max, ref pos, ref len);

            // LZ-CUE optimization start
            if (lookAhead)
            {
                if (len_best > BLZ_THRESHOLD)
                {
                    if (raw + len_best < raw_end)
                    {
                        raw += len_best;
                        SEARCH(ref len_next, ref pos_next, ref raw_buffer, ref raw, ref raw_end, ref max, ref pos, ref len);
                        raw -= len_best - 1;
                        SEARCH(ref len_post, ref pos_post, ref raw_buffer, ref raw, ref raw_end, ref max, ref pos, ref len);
                        raw--;

                        if (len_next <= BLZ_THRESHOLD) len_next = 1;
                        if (len_post <= BLZ_THRESHOLD) len_post = 1;

                        if (len_best + len_next <= 1 + len_post) len_best = 1;
                    }
                }
            }
            // LZ-CUE optimization end

            pak_buffer[flg] <<= 1;
            if (len_best > BLZ_THRESHOLD)
            {
                raw += len_best;
                pak_buffer[flg] |= 1;
                pak_buffer[pak] = (byte)(((len_best - (BLZ_THRESHOLD + 1)) << 4) | ((pos_best - 3) >> 8));
                pak++;
                pak_buffer[pak] = (byte)((pos_best - 3) & 0xFF);
                pak++;
                bytes_saved += len_best - 2;
            }
            else
            {
                pak_buffer[pak] = raw_buffer[raw];
                pak++;
                raw++;
            }

            if (pak + raw_len - raw <= pak_tmp + raw_tmp)
            {
                pak_tmp = pak;
                raw_tmp = raw_len - raw;
            }
        }

        total_bytes_saved += bytes_saved;
        if (total_bytes_saved > best_total_saved)
        {
            best_total_saved = total_bytes_saved;
            best_pak_tmp = pak_tmp;
            best_raw_tmp = raw_tmp;
            best_flg = flg;
        }

        while ((mask != 0) && (mask != 1))
        {
            mask = (byte)(((uint)mask) >> ((int)BLZ_SHIFT));
            pak_buffer[flg] <<= 1;
        }

        bool has_raw_tmp = (arm9 && best_raw_tmp > 0x4000) || (!arm9 && best_raw_tmp > 0);

        if (has_raw_tmp && pak_buffer[best_flg] > 0)
        {
            var flag_byte = pak_buffer[best_flg];
            while ((flag_byte & 1) == 0) // treat trailing literals as uncompressed data
            {
                best_raw_tmp += 1;
                best_pak_tmp -= 1;
                flag_byte >>= 1;
            }
        }

        raw_tmp = best_raw_tmp;
        pak_tmp = best_pak_tmp;
        pak_len = pak;

        BLZ_Invert(raw_buffer, 0, raw_len);
        BLZ_Invert(pak_buffer, 0, pak_len);

        if ((pak_tmp == 0) || (raw_len + 4 < ((pak_tmp + raw_tmp + 3) & -4) + 8))
        {
            pak = 0;
            raw = 0;
            raw_end = raw_len;

            while (raw < raw_end)
            {
                pak_buffer[pak] = raw_buffer[raw];
                pak++;
                raw++;
            }

            while ((pak & 3) != 0)
            {
                pak_buffer[pak] = 0;
                pak++;
            }

            pak_buffer[pak] = 0;
            pak_buffer[pak + 1] = 0;
            pak_buffer[pak + 2] = 0;
            pak_buffer[pak + 3] = 0;
            pak += 4;
        }
        else
        {
            tmp = Memory((int)(raw_tmp + pak_tmp + 11), 1);

            for (len = 0; len < raw_tmp; len++)
                tmp[len] = raw_buffer[len];

            for (len = 0; len < pak_tmp; len++)
                tmp[raw_tmp + len] = pak_buffer[len + pak_len - pak_tmp];

            pak_buffer = tmp;
            pak = raw_tmp + pak_tmp;

            enc_len = pak_tmp;
            hdr_len = 8;
            inc_len = raw_len - pak_tmp - raw_tmp;

            while ((pak & 3) != 0)
            {
                pak_buffer[pak] = 0xFF;
                pak++;
                hdr_len++;
            }

            byte[] tmpbyte = BitConverter.GetBytes(enc_len + hdr_len);
            tmpbyte.CopyTo(pak_buffer, pak);
            pak += 3;
            pak_buffer[pak] = (byte)hdr_len;
            pak++;
            tmpbyte = BitConverter.GetBytes(inc_len - hdr_len);
            tmpbyte.CopyTo(pak_buffer, pak);
            pak += 4;
        }

        new_len = pak;

        return (pak_buffer);
    }
}