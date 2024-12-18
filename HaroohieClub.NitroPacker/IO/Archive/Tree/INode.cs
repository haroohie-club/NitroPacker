using System.Collections.Generic;

namespace HaroohieClub.NitroPacker.IO.Archive.Tree;

internal interface INode
{
    public string Name { get; set; }
    public string Path { get; set; }
    public ushort Id { get; set; }
    public INode Parent { get; set; }
    public IList<INode> Children { get; set; }
}