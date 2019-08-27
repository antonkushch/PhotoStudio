using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace PhotoStudioDiploma.Models
{
    public class SyncDropboxAccountViewModel
    {
        public string Message { get; set; }
        public bool SyncNeeded { get; set; }
        public List<PhotographerFolder> SyncedFolders { get; set; }

        private string _syncedFoldersMessage;
        public string SyncedFoldersMessage
        {
            get
            {
                if (SyncedFolders.Count == 0)
                    return "";
                StringBuilder sb = new StringBuilder();
                foreach(var syncedFolder in SyncedFolders)
                {
                    sb.Append($"'{syncedFolder.Path}' ");
                }
                return $"Folders: {sb.ToString()}have been synced.";
            }
        }
    }
}