using PhotoStudioDiploma.DTO;
using PhotoStudioDiploma.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity.Owin;
using PhotoStudioDiploma.Exceptions;

namespace PhotoStudioDiploma.Services
{
    public interface IGrantAccessService
    {
        ErrorSink<DuplicateGrantingError> GrantAccessToFolders(GrantAccessDTO grantAccessDTO);
        void RevokeGrantToFolder(ApplicationUser currUser, string folderPath, string userEmail);
    }
    
    public class GrantAccessService : IGrantAccessService
    {
        private readonly ApplicationDbContext db;

        public GrantAccessService()
        {
            db = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
            db.Configuration.ProxyCreationEnabled = true;
            db.Configuration.LazyLoadingEnabled = true;
        }

        private void GrantAccessToFoldersToUser(GrantAccessToUserDTO grantAccessToUserDTO, ErrorSink<DuplicateGrantingError> errors)
        {
            var destUser = db.Users.FirstOrDefault(u => u.Email == grantAccessToUserDTO.User);
            if(destUser == null)
            {
                throw new GrantAccessToFolderException("Something went wrong when granting access.");
            }

            foreach(var folderId in grantAccessToUserDTO.Folders)
            {
                var folder = grantAccessToUserDTO.CurrUser.PhotographerFolders.SingleOrDefault(f => f.PhotographerFolderId == folderId);
                if(folder == null)
                {
                    throw new GrantAccessToFolderException("Something went wrong when granting access.");
                }

                if (destUser.GrantedFolders.Contains(folder))
                {
                    errors.AddNewError(new DuplicateGrantingError(destUser, folder));
                }
                else
                {
                    destUser.GrantedFolders.Add(folder);
                }
            }

            db.SaveChanges();
        }

        public ErrorSink<DuplicateGrantingError> GrantAccessToFolders(GrantAccessDTO grantAccessDTO)
        {
            ErrorSink<DuplicateGrantingError> errors = new ErrorSink<DuplicateGrantingError>();
            foreach(var userEmail /*userId*/ in grantAccessDTO.Users)
            {
                GrantAccessToFoldersToUser(new GrantAccessToUserDTO { CurrUser = grantAccessDTO.CurrUser, Folders = grantAccessDTO.Folders, User = userEmail }, errors);
            }

            return errors;
        }

        public void RevokeGrantToFolder(ApplicationUser currUser, string folderPath, string userEmail)
        {  
            // get folder by folderPath
            var folder = currUser.PhotographerFolders.SingleOrDefault(f => f.Path == folderPath);
            if(folder == null)
            {
                throw new ContentNotFoundException("Folder is not found in your dropbox account.");
            }

            // get user by userEmail
            var user = db.Users.SingleOrDefault(u => u.Email == userEmail);
            if(user == null)
            {
                throw new ContentNotFoundException("Destination user not found.");
            }

            // remove
            user.GrantedFolders.Remove(folder);
            db.SaveChanges();
        }
    }
}