using System.Collections.Generic;
using System.Linq;

namespace HaroohieClub.NitroPacker.IO.Archive.Tree;

public class DirectoryNode(string name, string path, INode parent, ushort id, ushort entryFileId) : INode
{
    public string Name { get; set; } = name;
    public string Path { get; set; } = path;
    public ushort Id { get; set; } = id;
    public INode Parent { get; set; } = parent;
    public IList<INode> Children { get; set; } = [];
    public ushort EntryFileId { get; set; } = entryFileId;
    
    public DirectoryNode FindNodeFromId(ushort id)
    {
        foreach (DirectoryNode dir in Children.Where(c => c is DirectoryNode).Cast<DirectoryNode>())
        {
            if (dir.Id == id)
            {
                return dir;
            }
            DirectoryNode fromChildren = dir.FindNodeFromId(id);
            if (fromChildren is not null)
            {
                return fromChildren;
            }
        }
        
        return null;
    }
}