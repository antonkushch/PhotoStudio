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
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private IRegistrationService registrationService;
        private ILoginService loginService;
        private IProfileService profileService;
        private IDropboxAccountService dropboxAccountService;
        
        public AccountController()
        {
        }

        public AccountController(IRegistrationService registrationService, ILoginService loginService, IProfileService profileService, IDropboxAccountService dropboxAccountService)
        {
            this.registrationService = registrationService;
            this.loginService = loginService;
            this.profileService = profileService;
            this.dropboxAccountService = dropboxAccountService;
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
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

        [AllowAnonymous]
        public ActionResult UsersView()
        {
            var users = UserManager.Users;
            return View("Users", users);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> DeleteUser(string userId)
        {
            var user = await UserManager.FindByIdAsync(userId);            
            await UserManager.DeleteAsync(user);
            
            var users = UserManager.Users;
            return View("Users", users);
        }

        [HttpGet]
        public ActionResult EmailConfirmationNeeded()
        {
            return View();
        }

        [HttpGet]
        public async Task<ActionResult> ResendEmailConfirmation()
        {
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            // sending account confirmation email
            string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
            var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
            await UserManager.SendEmailAsync(user.Id, "[Photo Studio] Please confirm your account", "Confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

            return View("ConfirmEmailRequest");
        }

        [HttpGet]
        public ActionResult Error()
        {
            return View();
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        [DuplicateAuth]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            var result = await loginService.LoginUser(model);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        [DuplicateAuth]
        public ActionResult Register()
        {
            RegisterViewModel model = new RegisterViewModel();
            model.RegisterTypeList = GetRegisterTypes();
            return View(model);
        }

        private List<RegisterType> GetRegisterTypes()
        {
            return new List<RegisterType>
            {
                new RegisterType { Id = 1, RegTypeEnum = RegTypeEnum.Client },
                new RegisterType { Id = 2, RegTypeEnum = RegTypeEnum.Photographer }
            };
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var registeredUser = await registrationService.RegisterUser(model);
                if (registeredUser == null)
                {
                    AddErrors("Error registering a user.");
                    model.RegisterTypeList = GetRegisterTypes();
                    return View(model);
                }

                if(registeredUser.ClientInfo != null)
                {
                    // sending account confirmation email
                    string code = await UserManager.GenerateEmailConfirmationTokenAsync(registeredUser.Id);
                    var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = registeredUser.Id, code = code }, protocol: Request.Url.Scheme);
                    await UserManager.SendEmailAsync(registeredUser.Id, "[Photo Studio] Please confirm your account", "Confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    return View("ConfirmEmailRequest");
                }
                else
                {
                    var redirect = DropboxOAuth2Helper.GetAuthorizeUri(
                        OAuthResponseType.Code,
                        MvcApplication.AppKey,
                        RedirectUri,
                        registeredUser.PhotographerInfo.ConnectState);

                    // redirecting to dropbox page of authorization
                    return Redirect(redirect.ToString());
                }
            }

            // If we got this far, something failed, redisplay form
            model.RegisterTypeList = GetRegisterTypes();
            return View(model);
        }

        // second step in integration with dropbox (getting access token)
        public async Task<ActionResult> Auth(string code, string state)
        {
            var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            var result = await registrationService.DropboxAuthenticationSecondStep(currUser, code, state, RedirectUri);

            if (!result)
            {
                AddErrors("Error connecting to Dropbox.");
                return View("Register", ReinitializeModel());
            }

            await dropboxAccountService.AddDropboxContentsToDB(currUser);

            return RedirectToAction("Index", "Home");
        }
        
        private RegisterViewModel ReinitializeModel()
        {
            RegisterViewModel model = new RegisterViewModel();
            model.RegisterTypeList = GetRegisterTypes();
            return model;
        }

        [HttpGet]
        public async Task<ActionResult> UserProfile()
        {
            var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            ProfileViewModel model = new ProfileViewModel
            {
                Name = currUser.Name,
                Surname = currUser.Surname,
                Username = currUser.UserName,
                Email = currUser.Email,
                RegisterType = profileService.GetRegistrationType(currUser),
                DropboxAccountInfo = await dropboxAccountService.GetDropboxAccountInfo(currUser)
            };
            
            return View(model);
        }

        //[HttpGet]
        //public async Task<ActionResult> DropboxAccountContents()
        //{
        //    var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
        //    DropboxFolder dropboxFileSystem;
        //    try
        //    {
        //        dropboxFileSystem = dropboxAccountService.GetFolder(currUser, "/", 0);
        //    }
        //    catch (DropboxFolderEmptyException ex)
        //    {
        //        return PartialView("_EmptyFolderPartial", new DropboxFolderContentsViewModel { FolderPath = "/" });
        //    }

        //    var prevFolders = dropboxAccountService.GetPreviousFolders(currUser, "/");
        //    DropboxFolderContentsViewModel dropboxContents = new DropboxFolderContentsViewModel
        //    {
        //        FolderPath = dropboxFileSystem.Path,
        //        FolderName = dropboxFileSystem.Name,
        //        DropboxFolders = dropboxFileSystem.Folders,
        //        DropboxFiles = dropboxFileSystem.Files,
        //        PreviousFolders = prevFolders
        //    };

        //    return View(dropboxContents);
        //}

        //[HttpGet]
        //[Authorize(Roles = "photographer")]
        //public async Task<ActionResult> DropboxAccountContents()
        //{
        //    DropboxFolderGotoViewModel model = new DropboxFolderGotoViewModel();
        //    if(TempData["folderContents"] != null)
        //    {
        //        model = TempData["folderContents"] as DropboxFolderGotoViewModel;
        //    }

        //    var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
        //    DropboxFolder dropboxFileSystem;
        //    try
        //    {
        //        dropboxFileSystem = dropboxAccountService.GetFolder(currUser, model.FolderPath, model.FolderDepth);
        //    }
        //    catch (DropboxFolderEmptyException ex)
        //    {
        //        return PartialView("_EmptyFolderPartial", new DropboxFolderContentsViewModel { FolderPath = "/" });
        //    }

        //    var prevFolders = dropboxAccountService.GetPreviousFolders(currUser, model.FolderPath);
        //    DropboxFolderContentsViewModel dropboxContents = new DropboxFolderContentsViewModel
        //    {
        //        FolderPath = dropboxFileSystem.Path,
        //        FolderName = dropboxFileSystem.Name,
        //        DropboxFolders = dropboxFileSystem.Folders,
        //        DropboxFiles = dropboxFileSystem.Files,
        //        PreviousFolders = prevFolders
        //    };

        //    return View(dropboxContents);
        //}

        //[HttpPost]
        //public ActionResult DropboxAccountContents(DropboxFolderGotoViewModel model)
        //{
        //    //var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());

        //    //DropboxFolder dropboxFileSystem = dropboxAccountService.GetFolder(currUser, model.FolderPath, model.FolderDepth);

        //    //var prevFolders = dropboxAccountService.GetPreviousFolders(currUser, model.FolderPath);
        //    //DropboxFolderContentsViewModel dropboxContents = new DropboxFolderContentsViewModel
        //    //{
        //    //    FolderPath = dropboxFileSystem.Path,
        //    //    FolderName = dropboxFileSystem.Name,
        //    //    DropboxFolders = dropboxFileSystem.Folders,
        //    //    DropboxFiles = dropboxFileSystem.Files,
        //    //    PreviousFolders = prevFolders
        //    //};
        //    TempData["folderContents"] = model;
        //    var redirectToUrl = Url.Action("DropboxAccountContents", "Account");
        //    return Json(new { localUrl = redirectToUrl }, JsonRequestBehavior.AllowGet);
        //}

        //[HttpPost]
        //public async Task<ActionResult> DropboxAccountContents(DropboxFolderGotoViewModel model)
        //{
        //    var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
        //    DropboxFolder dropboxFileSystem;
        //    try
        //    {
        //        dropboxFileSystem = dropboxAccountService.GetFolder(currUser, model.FolderPath, model.FolderDepth);
        //    }
        //    catch(DropboxFolderEmptyException ex)
        //    {
        //        return PartialView("_EmptyFolderPartial", new DropboxFolderContentsViewModel { FolderPath = "/" });
        //    }

        //    var prevFolders = dropboxAccountService.GetPreviousFolders(currUser, model.FolderPath);
        //    DropboxFolderContentsViewModel dropboxContents = new DropboxFolderContentsViewModel
        //    {
        //        FolderPath = dropboxFileSystem.Path,
        //        FolderName = dropboxFileSystem.Name,
        //        DropboxFolders = dropboxFileSystem.Folders,
        //        DropboxFiles = dropboxFileSystem.Files,
        //        PreviousFolders = prevFolders
        //    };

        //    var redirectToUrl = Url.Action("DropboxAccountContents", "Account"/*, dropboxContents*/);

        //    return Json(new { localUrl = redirectToUrl }, JsonRequestBehavior.AllowGet);
        //    //return Redirect("/Account/DropboxAccountContents");
        //}

        //[HttpPost]
        //public async Task<PartialViewResult> InnerDropboxAccountContents(DropboxFolderGotoViewModel model)
        //{
        //    var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
        //    DropboxFolder dropboxFileSystem;
        //    List<DropboxFolder> prevFolders;
        //    try
        //    {
        //        dropboxFileSystem = dropboxAccountService.GetFolder(currUser, model.FolderPath, model.FolderDepth);
        //    }
        //    catch (DropboxFolderEmptyException ex)
        //    {
        //        prevFolders = dropboxAccountService.GetPreviousFolders(currUser, model.FolderPath);
        //        DropboxFolderContentsViewModel dropboxContentsVar = new DropboxFolderContentsViewModel
        //        {
        //            FolderPath = model.FolderPath,
        //            PreviousFolders = prevFolders
        //        };
                
        //        return PartialView("_EmptyFolderPartial", dropboxContentsVar);
        //    }

        //    prevFolders = dropboxAccountService.GetPreviousFolders(currUser, model.FolderPath);
        //    DropboxFolderContentsViewModel dropboxContents = new DropboxFolderContentsViewModel
        //    {
        //        FolderPath = dropboxFileSystem.Path,
        //        FolderName = dropboxFileSystem.Name,
        //        DropboxFolders = dropboxFileSystem.Folders,
        //        DropboxFiles = dropboxFileSystem.Files,
        //        PreviousFolders = prevFolders
        //    };

        //    return PartialView("_FolderPartial", dropboxContents);
        //}

        //[HttpGet]
        //public async Task<ActionResult> ViewDropboxFile(int dropboxFileId)
        //{
        //    var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
        //    var dropboxFile = await dropboxAccountService.GetFile(currUser, dropboxFileId);

        //    if(dropboxFile == null)
        //    {
        //        ViewBag.FileNotFound = "File not found.";
        //        return View();
        //    }

        //    DropboxFileViewModel fileVM = new DropboxFileViewModel
        //    {
        //        FileName = dropboxFile.Name,
        //        FilePath = dropboxFile.Path,
        //        FileSize = dropboxFile.Size,
        //        Depth = dropboxFile.Depth,
        //        LinkToFile = dropboxFile.LinkToFile,
        //        PreviousFolders = dropboxAccountService.GetPreviousFolders(currUser, dropboxFile.Path, false)
        //    };

        //    return View(fileVM);
        //}

        //private async Task<GrantAccessToFoldersViewModel> ReinitializeUsersAndFolders(GrantAccessToFoldersViewModel model)
        //{
        //    var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
        //    var grantAccessVM = new GrantAccessToFoldersViewModel
        //    {
        //        AllDropboxFolders = dropboxAccountService.GetSelectListItemOfAllFolders(currUser),
        //        FoundAppUsers = model.FoundAppUsers == null ? new List<string>() : model.FoundAppUsers
        //    };

        //    return grantAccessVM;
        //}

        //[HttpGet]
        //public async Task<ActionResult> GrantAccessToFolders()
        //{
        //    return View(await ReinitializeUsersAndFolders(new GrantAccessToFoldersViewModel()));
        //}

        //[HttpPost]
        //public async Task<ActionResult> GrantAccessToFolders(GrantAccessToFoldersViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var grantAccessDTO = new GrantAccessDTO
        //        {
        //            CurrUser = await UserManager.FindByIdAsync(User.Identity.GetUserId()),
        //            Folders = model.SelectedDropboxFolders,
        //            Users = model.FoundAppUsers
        //        };

        //        try
        //        {
        //            var errorSink = grantAccessService.GrantAccessToFolders(grantAccessDTO);
        //            if (errorSink.HasErrors())
        //            {
        //                foreach(var error in errorSink.ErrorsList)
        //                {
        //                    AddErrors(error.ToString());
        //                }
        //                return View(await ReinitializeUsersAndFolders(model));
        //            }
        //        }
        //        catch(GrantAccessToFolderException ex)
        //        {
        //            AddErrors(ex.Message);
        //            return View(await ReinitializeUsersAndFolders(model));
        //        }

        //        //foreach(var userId in model.SelectedAppUsers)
        //        //{
        //        //    profileService.GetFoldersUserHasAccessTo(await UserManager.FindByIdAsync(userId));
        //        //}

        //        //foreach(var folderId in model.SelectedDropboxFolders)
        //        //{
        //        //    profileService.GetUsersThatHaveAccessToFolder(await UserManager.FindByIdAsync(User.Identity.GetUserId()), folderId);
        //        //}
                
        //        return RedirectToAction("SuccessfulGranting");
        //    }
        //    else
        //    {
        //        return View(await ReinitializeUsersAndFolders(model));
        //    }
        //}

        //[HttpGet]
        //public ActionResult SuccessfulGranting()
        //{
        //    return View();
        //}

        //[HttpGet]
        //public async Task<ActionResult> GrantedFolders()
        //{
        //    var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
        //    var grantedFoldersVM = new GrantedFoldersViewModel
        //    {
        //        DropboxFolders = dropboxAccountService.GetAllFoldersWithGrantedUsers(currUser)
        //    };

        //    return View(grantedFoldersVM);
        //}

        //[HttpGet]
        //[EmailConfirmed]
        //public async Task<ActionResult> GrantedFoldersToMe()
        //{
        //    var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());

        //    List<DropboxFolderGrantedToClient> grantedFolders;
        //    try
        //    {
        //        grantedFolders = dropboxAccountService.GetFoldersGrantedToClient(currUser);
        //    }
        //    catch(NoFoldersGrantedToClientException ex)
        //    {
        //        // do handling of situation when client has 0 granted to him folders
        //        return View(new GrantedFoldersToClientViewModel());
        //    }

        //    GrantedFoldersToClientViewModel model = new GrantedFoldersToClientViewModel
        //    {
        //        Folders = grantedFolders
        //    };

        //    return View(model);
        //}

        //[HttpGet]
        //public async Task<ActionResult> ViewGrantedToClientFolder(int folderId)
        //{
        //    var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
        //    DropboxFolderGrantedToClient folder;
        //    try
        //    {
        //        folder = dropboxAccountService.GetFolderGrantedToClient(currUser, folderId);
        //    }
        //    catch (NoAccessToContentException ex)
        //    {
        //        return View("Error");
        //    }
        //    catch (DropboxFolderEmptyException ex)
        //    {
        //        folder = new DropboxFolderGrantedToClient
        //        {
        //            Photographer = ex.FolderInfo.Photographer,
        //            FolderName = ex.FolderInfo.FolderName
        //        };
        //        return View("ViewGrantedToClientFolder", folder);
        //    }
        //    catch (ContentNotFoundException ex)
        //    {
        //        return View("Error");
        //    }

        //    return View("ViewGrantedToClientFolder", folder);
        //}

        //[HttpPost]
        //public async Task<PartialViewResult> BrowseGrantedToClientFolder(GoToGrantedFolderViewModel folderModel)
        //{
        //    var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());

        //    DropboxFolderGrantedToClient folder;
        //    try
        //    {
        //        folder = dropboxAccountService.GetFolderGrantedToClient(folderModel.FolderId);
        //    }
        //    catch(DropboxFolderEmptyException ex)
        //    {
        //        GrantedFoldersToClientViewModel emptyModel = new GrantedFoldersToClientViewModel
        //        {
        //            OwnerPhotographer = ex.FolderInfo.PhotographerFullName,
        //            FolderName = ex.FolderInfo.FolderName
        //        };
        //        return PartialView("_EmptyGrantedFolderPartial", emptyModel);
        //    }
        //    catch (ContentNotFoundException ex)
        //    {
        //        return PartialView("Error");
        //    }

        //    GrantedFoldersToClientViewModel model = new GrantedFoldersToClientViewModel
        //    {
        //        FolderName = folder.FolderName,
        //        OwnerPhotographer = folder.PhotographerFullName,
        //        Files = folder.Files
        //    };

        //    return PartialView("_ClientGrantedFolderPartial", model);
        //}

        //[HttpPost]
        //public ActionResult SearchUser(ApplicationUserDTO user)
        //{
        //    var appUser = UserManager.FindByEmail(user.Email);
        //    if(appUser != null)
        //    {
        //        return Json(new { success = true, userEmail = appUser.Email, userId = appUser.Id });
        //    }
        //    else
        //    {
        //        return Json(new { success = false });
        //    }
        //}

        //[HttpGet]
        //public async Task<ActionResult> ViewGrantedToClientFile(int dropboxFileId)
        //{
        //    var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());

        //    DropboxFileGrantedToClient dropboxFile;
        //    try
        //    {
        //        dropboxFile = await dropboxAccountService.GetClientFile(currUser, dropboxFileId);
        //    }
        //    catch(NoAccessToContentException ex)
        //    {
        //        return View("Error");
        //    }
        //    catch(ContentNotFoundException ex)
        //    {
        //        return View("Error");
        //    }

        //    DropboxFileGrantedToClientViewModel model = new DropboxFileGrantedToClientViewModel
        //    {
        //        File = dropboxFile,
        //        FolderId = dropboxAccountService.GetFolderId(dropboxFileId)
        //    };

        //    return View("ViewGrantedToClientFile", model);
        //}

        //[HttpGet]
        //public ActionResult RevokeGrant(string folderPath, string userEmail)
        //{
        //    ConfirmRevokeGrantViewModel model = new ConfirmRevokeGrantViewModel
        //    {
        //        FolderString = folderPath,
        //        UserEmailString = userEmail,
        //        Message = $"Are you sure you want to revoke grant of folder '{folderPath}' to user '{userEmail}'"
        //    };

        //    return PartialView("_ConfirmRevokeGrantPartial", model);
        //}

        //[HttpPost]
        //public async Task<ActionResult> RevokeGrantConfirm(string folderPath, string userEmail)
        //{
        //    var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
        //    try
        //    {
        //        grantAccessService.RevokeGrantToFolder(currUser, folderPath, userEmail);
        //    }
        //    catch(ContentNotFoundException ex)
        //    {
        //        return Json(new { success = false, errorMsg = ex.Message }, JsonRequestBehavior.AllowGet);
        //    }

        //    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        //}

        [HttpGet]
        public ActionResult SyncDropbox()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> SyncDropbox(SyncDropboxAccountViewModel model)
        {
            var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            var synced = await dropboxAccountService.SyncDropboxAccountContents(currUser);

            SyncDropboxAccountViewModel resModel;
            if (synced.SyncNeeded)
            {
                resModel = new SyncDropboxAccountViewModel { SyncNeeded = synced.SyncNeeded, Message = "Folders synchronized", SyncedFolders = synced.SyncedDropboxFolders };
            }
            else
            {
                resModel = new SyncDropboxAccountViewModel { SyncNeeded = synced.SyncNeeded, Message = "Sync is not needed", SyncedFolders = synced.SyncedDropboxFolders };
            }

            return View(resModel);
        }

        [HttpGet]
        public async Task<ActionResult> CheckSyncWithDropbox()
        {
            var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            Stopwatch methodExecTime = Stopwatch.StartNew(); 
            var syncNeed = await dropboxAccountService.CheckIfSyncWithDropboxIsNeeded(currUser);
            methodExecTime.Stop();

            var elapsedTime = methodExecTime.Elapsed.TotalSeconds;

            return Json(new { syncNeeded = syncNeed, execTime = elapsedTime }, JsonRequestBehavior.AllowGet);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }
        
        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }
        
        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
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

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}