// This code is heavily based on code Gericom wrote for ErmiiBuild

using System;
using System.Collections.Generic;
using System.Linq;

namespace HaroohieClub.NitroPacker.Patcher.Nitro
{
	public class CRT0
	{
		public class ModuleParams
        {
            public uint AutoLoadListOffset { get; set; }
            public uint AutoLoadListEnd { get; set; }
            public uint AutoLoadStart { get; set; }
            public uint StaticBssStart { get; set; }
            public uint StaticBssEnd { get; set; }
            public uint CompressedStaticEnd { get; set; }
            public uint SDKVersion { get; set; }
            public uint NitroCodeBE { get; set; }
            public uint NitroCodeLE { get; set; }

            public ModuleParams(byte[] data, uint offset)
			{
				AutoLoadListOffset = BitConverter.ToUInt32(data.Skip((int)offset).Take(4).ToArray());
				AutoLoadListEnd = BitConverter.ToUInt32(data.Skip((int)offset + 0x04).Take(4).ToArray());
				AutoLoadStart = BitConverter.ToUInt32(data.Skip((int)offset + 0x08).Take(4).ToArray());
				StaticBssStart = BitConverter.ToUInt32(data.Skip((int)offset + 0x0C).Take(4).ToArray());
				StaticBssEnd = BitConverter.ToUInt32(data.Skip((int)offset + 0x10).Take(4).ToArray());
				CompressedStaticEnd = BitConverter.ToUInt32(data.Skip((int)offset + 0x14).Take(4).ToArray());
				SDKVersion = BitConverter.ToUInt32(data.Skip((int)offset + 0x18).Take(4).ToArray());
				NitroCodeBE = BitConverter.ToUInt32(data.Skip((int)offset + 0x1C).Take(4).ToArray());
				NitroCodeLE = BitConverter.ToUInt32(data.Skip((int)offset + 0x20).Take(4).ToArray());
			}
			public List<byte> GetBytes()
			{
				List<byte> bytes = new();
				bytes.AddRange(BitConverter.GetBytes(AutoLoadListOffset));
				bytes.AddRange(BitConverter.GetBytes(AutoLoadListEnd));
				bytes.AddRange(BitConverter.GetBytes(AutoLoadStart));
				bytes.AddRange(BitConverter.GetBytes(StaticBssStart));
				bytes.AddRange(BitConverter.GetBytes(StaticBssEnd));
				bytes.AddRange(BitConverter.GetBytes(CompressedStaticEnd));
				bytes.AddRange(BitConverter.GetBytes(SDKVersion));
				bytes.AddRange(BitConverter.GetBytes(NitroCodeBE));
				bytes.AddRange(BitConverter.GetBytes(NitroCodeLE));

				return bytes;
			}
		}

		public class AutoLoadEntry
        {
            public uint Address { get; set; }
            public uint Size { get; set; }
            public uint BssSize { get; set; }
            public List<byte> Data { get; set; }

            public AutoLoadEntry(uint address, byte[] data)
			{
				Address = address;
				Data = data.ToList();
				Size = (uint)data.Length;
				BssSize = 0;
			}
			public AutoLoadEntry(byte[] data, uint offset)
			{
				Address = BitConverter.ToUInt32(data.Skip((int)offset).Take(4).ToArray());
				Size = BitConverter.ToUInt32(data.Skip((int)offset + 0x04).Take(4).ToArray());
				BssSize = BitConverter.ToUInt32(data.Skip((int)offset + 0x08).Take(4).ToArray());
			}
			public List<byte> GetEntryBytes()
			{
				List<byte> bytes = new();
				bytes.AddRange(BitConverter.GetBytes(Address));
				bytes.AddRange(BitConverter.GetBytes(Size));
				bytes.AddRange(BitConverter.GetBytes(BssSize));

				return bytes;
			}
		}

		public static byte[] MIi_UncompressBackward(byte[] data)
		{
			uint leng = BitConverter.ToUInt32(data, data.Length - 4) + (uint)data.Length;
			byte[] Result = new byte[leng];
			Array.Copy(data, Result, data.Length);
			int offset = (int)(data.Length - (BitConverter.ToUInt32(data, data.Length - 8) >> 24));
			int dstOffs = (int)leng;
			while (true)
			{
				byte header = Result[--offset];
				for (int i = 0; i < 8; i++)
				{
					if ((header & 0x80) == 0) Result[--dstOffs] = Result[--offset];
					else
					{
						byte a = Result[--offset];
						byte b = Result[--offset];
						int offs = (((a & 0xF) << 8) | b) + 2;//+ 1;
						int length = (a >> 4) + 2;
						do
						{
							Result[dstOffs - 1] = Result[dstOffs + offs];
							dstOffs--;
							length--;
						}
						while (length >= 0);
					}
					if (offset <= (data.Length - (BitConverter.ToUInt32(data, data.Length - 8) & 0xFFFFFF))) return Result;
					header <<= 1;
				}
			}
		}
	}
}
