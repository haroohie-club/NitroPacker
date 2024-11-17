using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace HaroohieClub.NitroPacker.Core;

/// <summary>
/// NitroPacker base project file
/// </summary>
[Serializable]
[XmlRoot("EFEProject")]
public abstract class ProjectFile
{
    /// <summary>
    /// Empty constructor
    /// </summary>
    public ProjectFile() { }
    /// <summary>
    /// Creates project file with name
    /// </summary>
    /// <param name="projectName">The name of the project</param>
    public ProjectFile(string projectName) { ProjectName = projectName; }

    /// <summary>
    /// The name of the project
    /// </summary>
    [XmlAttribute("Name")]
    public string ProjectName { get; set; }

    /// <summary>
    /// The class of the project
    /// </summary>
    [XmlAttribute("Class")]
    public string ProjectClass { get; set; }

    /// <summary>
    /// Constructs project from byte array
    /// </summary>
    /// <typeparam name="T">Type of project</typeparam>
    /// <param name="data">XML project data to deserialize</param>
    /// <returns>A project of type T</returns>
    public static T FromByteArray<T>(byte[] data)
    {
        var s = new XmlSerializer(typeof(T));
        return (T)s.Deserialize(new MemoryStream(data));
    }

    /// <summary>
    /// Gets the type of the project
    /// </summary>
    /// <param name="data">XML project data to read</param>
    /// <returns>The type of the project</returns>
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

    /// <summary>
    /// Writes the project to a byte array in memory
    /// </summary>
    /// <returns>The resulting project XML as a byte array</returns>
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