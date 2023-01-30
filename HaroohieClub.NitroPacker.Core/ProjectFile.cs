using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace HaroohieClub.NitroPacker.Core
{
    [Serializable]
    [XmlRoot("EFEProject")]
    public abstract class ProjectFile
    {
        public ProjectFile() { }
        public ProjectFile(string projectName) { ProjectName = projectName; }

        [XmlAttribute("Name")]
        public string ProjectName { get; set; }

        [XmlAttribute("Class")]
        public string ProjectClass { get; set; }

        public static T FromByteArray<T>(byte[] data)
        {
            var s = new XmlSerializer(typeof(T));
            return (T)s.Deserialize(new MemoryStream(data));
        }

        public static Type GetProjectType(byte[] data)
        {
            var r = XmlReader.Create(new MemoryStream(data));
            r.Read();
            r.Read();
            r.Read();
            r.MoveToAttribute("Class");
            string s = r.ReadContentAsString();
            r.Close();
            return Type.GetType(s);
        }

        public byte[] Write()
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            var s = XmlSerializer.FromTypes(new[] { GetType() })[0];
            var m = new MemoryStream();
            s.Serialize(m, this, ns);
            var data = m.ToArray();
            m.Close();
            return data;
        }
    }
}
