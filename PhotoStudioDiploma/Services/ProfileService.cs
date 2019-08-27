using PhotoStudioDiploma.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace PhotoStudioDiploma.Services
{
    public interface IProfileService
    {
        string GetRegistrationType(ApplicationUser user);
        IEnumerable<SelectListItem> GetSelectListItemOfAllUsers(ApplicationUser currUser);

        // temp test
        int GetFoldersUserHasAccessTo(ApplicationUser user);
        int GetUsersThatHaveAccessToFolder(ApplicationUser user, int folderId);
        // temp test
    }

    public class ProfileService : IProfileService
    {
        private ApplicationUserManager userManager;

        public ProfileService(ApplicationUserManager userManager)
        {
            this.userManager = userManager;
        }

        public string GetRegistrationType(ApplicationUser user)
        {
            string regType = string.Empty;
            if (user.ClientInfo == null && user.PhotographerInfo != null)
            {
                regType = "Photographer";
            }
            else if (user.PhotographerInfo == null && user.ClientInfo != null)
            {
                regType = "Client";
            }
            else if (user.ClientInfo != null && user.PhotographerInfo != null)
            {
                regType = "Photographer and Client";
            }
            else
            {
                regType = "Nobody";
            }
            return regType;
        }

        public IEnumerable<SelectListItem> GetSelectListItemOfAllUsers(ApplicationUser currUser)
        {
            var allUsers = userManager.Users.Where(c => c.Id != currUser.Id).ToList();
            
            var selectListItemOfUsers = allUsers.Select(o => new SelectListItem
            {
                Text = $"{o.Name} {o.Surname} - {o.Email}",
                Value = o.Id
            });

            return selectListItemOfUsers;
        }

        public int GetFoldersUserHasAccessTo(ApplicationUser user)
        {
            var grantedFolders = user.GrantedFolders.ToList();

            return 1;
        }

        public int GetUsersThatHaveAccessToFolder(ApplicationUser user, int folderId)
        {
            var folder = user.PhotographerFolders.Single(f => f.PhotographerFolderId == folderId);
            var grantedUsers = folder.GrantedUsers.ToList();

            return 1;
        }
    }
}