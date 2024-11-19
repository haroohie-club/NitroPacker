using System;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Serialization;
using HaroohieClub.NitroPacker.Nitro.Gx;

namespace HaroohieClub.NitroPacker.Nitro.Card;

public class RomBanner
    {
        public RomBanner() { }

        public RomBanner(EndianBinaryReaderEx er)
        {
            Header = new(er);
            Banner = new(er);
        }

        public void Write(EndianBinaryWriterEx er)
        {
            Header.CRC16_v1 = Banner.GetCrc();
            Header.Write(er);
            Banner.Write(er);
        }

        public BannerHeader Header { get; set; }

        public class BannerHeader
        {
            public BannerHeader() { }

            public BannerHeader(EndianBinaryReaderEx er)
            {
                er.ReadObject(this);
            }

            public void Write(EndianBinaryWriterEx er)
            {
                er.WriteObject(this);
            }

            public byte Version { get; set; }
            public byte ReservedA { get; set; }

            [JsonIgnore]
            public ushort CRC16_v1 { get; set; }

            [ArraySize(28)]
            public byte[] ReservedB { get; set; }
        }

        public BannerV1 Banner { get; set; }

        public class BannerV1
        {
            public BannerV1() { }

            public BannerV1(EndianBinaryReader er)
            {
                Image = er.Read<byte>(32 * 32 / 2);
                Pltt = er.Read<byte>(16 * 2);
                GameName = new string[6];
                GameName[0] = er.ReadString(Encoding.Unicode, 128).Replace("\0", "");
                GameName[1] = er.ReadString(Encoding.Unicode, 128).Replace("\0", "");
                GameName[2] = er.ReadString(Encoding.Unicode, 128).Replace("\0", "");
                GameName[3] = er.ReadString(Encoding.Unicode, 128).Replace("\0", "");
                GameName[4] = er.ReadString(Encoding.Unicode, 128).Replace("\0", "");
                GameName[5] = er.ReadString(Encoding.Unicode, 128).Replace("\0", "");
            }

            public void Write(EndianBinaryWriter er)
            {
                er.Write(Image, 0, 32 * 32 / 2);
                er.Write(Pltt, 0, 16 * 2);
                foreach (string s in GameName) er.Write(GameName[0].PadRight(128, '\0'), Encoding.Unicode, false);
            }

            [ArraySize(32 * 32 / 2)]
            public byte[] Image { get; set; }

            [ArraySize(16 * 2)]
            public byte[] Pltt { get; set; }

            [JsonIgnore]
            [XmlIgnore]
            public string[] GameName { get; set; } //6, 128 chars (UTF16-LE)

            [XmlElement("GameName")]
            [JsonPropertyName("GameName")]
            public string[] Base64GameName
            {
                get
                {
                    string[] b = new string[6];
                    for (int i = 0; i < 6; i++)
                    {
                        b[i] = Convert.ToBase64String(Encoding.Unicode.GetBytes(GameName[i]));
                    }

                    return b;
                }
                set
                {
                    GameName = new string[6];
                    for (int i = 0; i < 6; i++)
                    {
                        GameName[i] = Encoding.Unicode.GetString(Convert.FromBase64String(value[i]));
                    }
                }
            }

            public ushort GetCrc()
            {
                byte[] data = new byte[2080];
                Array.Copy(Image, data, 512);
                Array.Copy(Pltt, 0, data, 512, 32);
                Array.Copy(Encoding.Unicode.GetBytes(GameName[0].PadRight(128, '\0')), 0, data, 544, 256);
                Array.Copy(Encoding.Unicode.GetBytes(GameName[1].PadRight(128, '\0')), 0, data, 544 + 256, 256);
                Array.Copy(Encoding.Unicode.GetBytes(GameName[2].PadRight(128, '\0')), 0, data, 544 + 256 * 2, 256);
                Array.Copy(Encoding.Unicode.GetBytes(GameName[3].PadRight(128, '\0')), 0, data, 544 + 256 * 3, 256);
                Array.Copy(Encoding.Unicode.GetBytes(GameName[4].PadRight(128, '\0')), 0, data, 544 + 256 * 4, 256);
                Array.Copy(Encoding.Unicode.GetBytes(GameName[5].PadRight(128, '\0')), 0, data, 544 + 256 * 5, 256);
                return Crc16.GetCrc16(data);
            }

            public Rgba8Bitmap GetIcon() => GxUtil.DecodeChar(Image, Pltt, ImageFormat.Pltt16, 32, 32, true);
        }
    }