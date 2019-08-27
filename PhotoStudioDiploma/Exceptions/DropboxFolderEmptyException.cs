using PhotoStudioDiploma.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoStudioDiploma.Exceptions
{
    public class DropboxFolderEmptyException : Exception
    {
        public DropboxFolderGrantedToClient FolderInfo { get; private set; }

        public DropboxFolderEmptyException() { }

        public DropboxFolderEmptyException(string msg) : base(msg) { }

        public DropboxFolderEmptyException(DropboxFolderGrantedToClient folderInfo)
        {
            FolderInfo = folderInfo;
        }
    }
}