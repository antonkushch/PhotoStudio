using PhotoStudioDiploma.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PhotoStudioDiploma.Models
{
    public class GrantAccessToFoldersViewModel
    {
        public IEnumerable<SelectListItem> AllDropboxFolders { get; set; }

        [Required(ErrorMessage = "Select at least one folder.")]
        private List<int> _selectedDropboxFolders;
        [Required(ErrorMessage = "Select at least one folder.")]
        public List<int> SelectedDropboxFolders
        {
            get { return _selectedDropboxFolders; }
            set { _selectedDropboxFolders = value; }
        }
        
        private List<string> _foundAppUsers;
        [Required(ErrorMessage = "Select at least one user.")]
        public List<string> FoundAppUsers
        {
            get { return _foundAppUsers; }
            set { _foundAppUsers = value; }
        }
    }

    public class GrantedFoldersViewModel
    {
        public List<DropboxFolder> DropboxFolders { get; set; }

        public GrantedFoldersViewModel()
        {
            DropboxFolders = new List<DropboxFolder>();
        }
    }
}