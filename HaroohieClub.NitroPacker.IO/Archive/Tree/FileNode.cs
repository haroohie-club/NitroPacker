using System.Collections.Generic;

namespace HaroohieClub.NitroPacker.IO.Archive.Tree;

public class FileNode(string name, INode parent, ushort id, string path) : INode
{
    public string Name { get; set; } = name;
    public string Path { get; set; } = path;
    public ushort Id { get; set; } = id;
    public INode Parent { get; set; } = parent;
    public IList<INode> Children { get; set; } = null;
}