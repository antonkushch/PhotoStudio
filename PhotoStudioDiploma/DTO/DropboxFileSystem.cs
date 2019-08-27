using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoStudioDiploma.DTO
{
    public class DropboxAccountFileSystem
    {
        public DropboxFolder RootFolder;

        public DropboxAccountFileSystem()
        {
            RootFolder = new DropboxFolder();
        }

        public void AddFolder(DropboxFolder folder)
        {
            RootFolder.Folders.Add(folder);
        }

        public void AddFile(DropboxFile file)
        {
            RootFolder.Files.Add(file);
        }
    }

    public class DropboxFolder
    {
        public int PhotographerFolderId { get; set; }
        public int Depth { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public List<DropboxFile> Files { get; set; }
        public List<DropboxFolder> Folders { get; set; }

        //
        public ApplicationUserDTO OwnerPhotographer { get; set; }
        public List<ApplicationUserDTO> GrantedUsers { get; set; }
        //

        public DropboxFolder()
        {
            Path = "/";
            Name = "";
            Depth = 0;
            Files = new List<DropboxFile>();
            Folders = new List<DropboxFolder>();
        }

        public DropboxFolder(string path, string name, int depth)
        {
            Path = path;
            Name = name;
            Depth = depth;
            Files = new List<DropboxFile>();
            Folders = new List<DropboxFolder>();
        }

        public void AddFolder(DropboxFolder folder)
        {
            Folders.Add(folder);
        }

        public void AddFile(DropboxFile file)
        {
            Files.Add(file);
        }
    }

    public class DropboxFile
    {
        public int PhotographerFileId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public byte[] ThumbnailImage { get; set; }
        public long Size { get; set; }
        public int Depth { get; set; }
        public string LinkToFile { get; set; }

        public DropboxFile()
        {
            Name = "";
            Path = "/";
            ThumbnailImage = new byte[1];
            Size = 0;
            Depth = 0;
        }

        public DropboxFile(string path, string name, long size, int depth, byte[] imageByteArr)
        {
            Path = path;
            Name = name;
            Size = size;
            Depth = depth;
            ThumbnailImage = imageByteArr;
        }
    }
}