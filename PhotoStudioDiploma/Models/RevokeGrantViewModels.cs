using PhotoStudioDiploma.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoStudioDiploma.Models
{
    public class ConfirmRevokeGrantViewModel
    {
        public DropboxFolder Folder { get; set; }
        public ApplicationUserDTO User { get; set; }
        public string FolderString { get; set; }
        public string UserEmailString { get; set; }
        public string Message { get; set; }
    }
}