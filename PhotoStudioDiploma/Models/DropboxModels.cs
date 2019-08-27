using PhotoStudioDiploma.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoStudioDiploma.Models
{
    public class DropboxFolderContentsViewModel
    {
        public string FolderPath { get; set; }
        public string FolderName { get; set; }
        public string ContentName { get; set; }
        public int Depth { get; set; }
        public UInt64 FileSize { get; set; }
        public string ImageThumbnail { get; set; }
        public List<DropboxFolder> DropboxFolders { get; set; }
        public List<DropboxFile> DropboxFiles { get; set; }
        public bool IsFolder { get; set; }
        public string Goto { get; set; }

        public List<DropboxFolder> PreviousFolders { get; set; }
    }

    public class DropboxFileViewModel
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public int Depth { get; set; }
        public long FileSize { get; set; }
        public string LinkToFile { get; set; }

        public List<DropboxFolder> PreviousFolders { get; set; }
    }

    public class DropboxFolderGotoViewModel
    {
        public string FolderPath { get; set; }
        public int FolderDepth { get; set; }

        public DropboxFolderGotoViewModel()
        {
            FolderPath = "/";
            FolderDepth = 0;
        }
    }
}