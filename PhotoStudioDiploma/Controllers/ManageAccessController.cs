using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Routing;
using Dropbox.Api;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using PhotoStudioDiploma.Controllers.CustomAttributes;
using PhotoStudioDiploma.DTO;
using PhotoStudioDiploma.Exceptions;
using PhotoStudioDiploma.Models;
using PhotoStudioDiploma.Services;

namespace PhotoStudioDiploma.Controllers
{
    [Authorize]
    public class ManageAccessController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private IDropboxAccountService dropboxAccountService;
        private IGrantAccessService grantAccessService;

        public ManageAccessController()
        {
        }

        public ManageAccessController(IDropboxAccountService dropboxAccountService, IGrantAccessService grantAccessService)
        {
            this.dropboxAccountService = dropboxAccountService;
            this.grantAccessService = grantAccessService;
        }

        public ManageAccessController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        private string RedirectUri
        {
            get
            {
                if (Request.Url.Host.ToLowerInvariant() == "localhost")
                {
                    return string.Format($"http://{Request.Url.Host}:{Request.Url.Port}/Account/Auth");
                }

                var builder = new UriBuilder(
                    Uri.UriSchemeHttps,
                    Request.Url.Host);

                builder.Path = "/Account/Auth";

                return builder.ToString();
            }
        }

        private async Task<GrantAccessToFoldersViewModel> ReinitializeUsersAndFolders(GrantAccessToFoldersViewModel model)
        {
            var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            var grantAccessVM = new GrantAccessToFoldersViewModel
            {
                AllDropboxFolders = dropboxAccountService.GetSelectListItemOfAllFolders(currUser),
                FoundAppUsers = model.FoundAppUsers == null ? new List<string>() : model.FoundAppUsers
            };

            return grantAccessVM;
        }

        [HttpGet]
        public async Task<ActionResult> GrantAccessToFolders()
        {
            return View(await ReinitializeUsersAndFolders(new GrantAccessToFoldersViewModel()));
        }

        [HttpPost]
        public async Task<ActionResult> GrantAccessToFolders(GrantAccessToFoldersViewModel model)
        {
            if (ModelState.IsValid)
            {
                var grantAccessDTO = new GrantAccessDTO
                {
                    CurrUser = await UserManager.FindByIdAsync(User.Identity.GetUserId()),
                    Folders = model.SelectedDropboxFolders,
                    Users = model.FoundAppUsers
                };

                try
                {
                    var errorSink = grantAccessService.GrantAccessToFolders(grantAccessDTO);
                    if (errorSink.HasErrors())
                    {
                        foreach (var error in errorSink.ErrorsList)
                        {
                            AddErrors(error.ToString());
                        }
                        return View(await ReinitializeUsersAndFolders(model));
                    }
                }
                catch (GrantAccessToFolderException ex)
                {
                    AddErrors(ex.Message);
                    return View(await ReinitializeUsersAndFolders(model));
                }

                return RedirectToAction("SuccessfulGranting");
            }
            else
            {
                return View(await ReinitializeUsersAndFolders(model));
            }
        }

        [HttpGet]
        public ActionResult SuccessfulGranting()
        {
            return View();
        }

        [HttpGet]
        public async Task<ActionResult> GrantedFolders()
        {
            var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            var grantedFoldersVM = new GrantedFoldersViewModel
            {
                DropboxFolders = dropboxAccountService.GetAllFoldersWithGrantedUsers(currUser)
            };

            return View(grantedFoldersVM);
        }

        [HttpPost]
        public ActionResult SearchUser(ApplicationUserDTO user)
        {
            var appUser = UserManager.FindByEmail(user.Email);
            if (appUser != null)
            {
                return Json(new { success = true, userEmail = appUser.Email, userId = appUser.Id });
            }
            else
            {
                return Json(new { success = false });
            }
        }

        [HttpGet]
        public ActionResult RevokeGrant(string folderPath, string userEmail)
        {
            ConfirmRevokeGrantViewModel model = new ConfirmRevokeGrantViewModel
            {
                FolderString = folderPath,
                UserEmailString = userEmail,
                Message = $"Are you sure you want to revoke grant of folder '{folderPath}' to user '{userEmail}'"
            };

            return PartialView("_ConfirmRevokeGrantPartial", model);
        }

        [HttpPost]
        public async Task<ActionResult> RevokeGrantConfirm(string folderPath, string userEmail)
        {
            var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            try
            {
                grantAccessService.RevokeGrantToFolder(currUser, folderPath, userEmail);
            }
            catch (ContentNotFoundException ex)
            {
                return Json(new { success = false, errorMsg = ex.Message }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private void AddErrors(IEnumerable<string> result)
        {
            foreach (var error in result)
            {
                ModelState.AddModelError("", error);
            }
        }

        private void AddErrors(string error)
        {
            ModelState.AddModelError("", error);
        }
    }
}