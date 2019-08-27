using PhotoStudioDiploma.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoStudioDiploma.Models
{
    public class DropboxFolderGrantedToClient
    {
        public int FolderId { get; set; }
        public string FolderThumbnail { get; set; }
        public string FolderName { get; set; }
        public ApplicationUserDTO Photographer { get; set; }

        public List<DropboxFileGrantedToClient> Files { get; set; }
        

        private int _numberOfFiles;
        public int NumberOfFiles
        {
            get { return Files == null ? 0 : Files.Count; }
            set { _numberOfFiles = value; }
        }

        private string _photographerFullName;
        public string PhotographerFullName
        {
            get { return Photographer == null ? "Unknown" : $"{Photographer.Name} {Photographer.Surname} - {Photographer.Email}"; }
            set { _photographerFullName = value; }
        }

        public string Goto { get; set; }
    }
    
    public class DropboxFileGrantedToClient
    {
        public int PhotographerFileId { get; set; }
        public string Name { get; set; }
        public byte[] ThumbnailImage { get; set; }
        public long Size { get; set; }
        public string LinkToFile { get; set; }

        public bool ToDownload { get; set; }
    }

    public class DropboxFileGrantedToClientViewModel
    {
        public DropboxFileGrantedToClient File { get; set; }
        public int FolderId { get; set; }
    }

    public class GrantedFoldersToClientViewModel
    {
        public string ImageThumbnail { get; set; }
        public string FolderName { get; set; }
        public string ContentName { get; set; }
        public string OwnerPhotographer { get; set; }
        public string NumberOfFiles { get; set; }
        public string Goto { get; set; }
        public long Size { get; set; }
        public List<DropboxFolderGrantedToClient> Folders { get; set; }
        public List<DropboxFileGrantedToClient> Files { get; set; }

        public GrantedFoldersToClientViewModel()
        {
            Folders = new List<DropboxFolderGrantedToClient>();
        }
    }

    public class GoToGrantedFolderViewModel
    {
        public int FolderId { get; set; }
    }
}