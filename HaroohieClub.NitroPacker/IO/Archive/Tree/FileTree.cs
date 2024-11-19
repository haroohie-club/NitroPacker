using System.IO;

namespace HaroohieClub.NitroPacker.IO.Archive.Tree;

public class FileTree
{
    public DirectoryNode Root { get; init; }

    public FileTree()
    {
    }
    
    public FileTree(Archive archive, ushort rootId, ushort fileId)
    {
        Root = CreateDirectoryNode(archive, null, Archive.RootPath, ref rootId, ref fileId);
    }

    private DirectoryNode CreateDirectoryNode(Archive archive, DirectoryNode parent, string path, ref ushort dirId, ref ushort fileId)
    {
        DirectoryNode thisNode = new(Path.GetFileName(path), path, parent, dirId++, fileId);

        foreach (string file in archive.EnumerateFiles(path, false))
        {
            thisNode.Children.Add(new FileNode(file, thisNode, fileId++, path));
        }
        
        foreach (string directory in archive.EnumerateDirectories(path, true))
        {
            thisNode.Children.Add(CreateDirectoryNode(archive, thisNode, directory, ref dirId, ref fileId));
        }

        return thisNode;
    }
}