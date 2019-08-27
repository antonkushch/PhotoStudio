using PhotoStudioDiploma.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoStudioDiploma.Exceptions
{
    public class GrantAccessToFolderException : Exception
    {
        public GrantAccessToFolderException() { }

        public GrantAccessToFolderException(string msg) : base(msg) { }
    }

    public class DuplicateGrantingError
    {
        private ApplicationUser User { get; set; }
        private PhotographerFolder Folder { get; set; }

        public DuplicateGrantingError() { }

        public DuplicateGrantingError(ApplicationUser user, PhotographerFolder folder)
        {
            User = user;
            Folder = folder;
        }

        public override string ToString()
        {
            return $"User '{User.Email}' already has access to folder '{Folder.Name}' of photographer '{Folder.ApplicationUser.Name} {Folder.ApplicationUser.Surname}'";
        }
    }

    public class ErrorSink<T>
    {
        public List<T> ErrorsList { get; set; }

        public ErrorSink()
        {
            ErrorsList = new List<T>();
        }

        public void AddNewError(T error)
        {
            ErrorsList.Add(error);
        }

        public bool HasErrors()
        {
            return ErrorsList.Count > 0;
        }
    }
}