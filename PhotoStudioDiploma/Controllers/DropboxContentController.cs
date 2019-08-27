using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
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
    public class DropboxContentController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private IDropboxAccountService dropboxAccountService;

        public DropboxContentController()
        {
        }

        public DropboxContentController(IDropboxAccountService dropboxAccountService)
        {
            this.dropboxAccountService = dropboxAccountService;
        }

        public DropboxContentController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
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

        [HttpGet]
        [Authorize(Roles = "photographer")]
        public async Task<ActionResult> DropboxAccountContents()
        {
            DropboxFolderGotoViewModel model = new DropboxFolderGotoViewModel();
            if (TempData["folderContents"] != null)
            {
                model = TempData["folderContents"] as DropboxFolderGotoViewModel;
            }

            var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            DropboxFolder dropboxFileSystem;
            try
            {
                dropboxFileSystem = dropboxAccountService.GetFolder(currUser, model.FolderPath, model.FolderDepth);
            }
            catch (DropboxFolderEmptyException ex)
            {
                return PartialView("_EmptyFolderPartial", new DropboxFolderContentsViewModel { FolderPath = "/" });
            }

            var prevFolders = dropboxAccountService.GetPreviousFolders(currUser, model.FolderPath);
            DropboxFolderContentsViewModel dropboxContents = new DropboxFolderContentsViewModel
            {
                FolderPath = dropboxFileSystem.Path,
                FolderName = dropboxFileSystem.Name,
                DropboxFolders = dropboxFileSystem.Folders,
                DropboxFiles = dropboxFileSystem.Files,
                PreviousFolders = prevFolders
            };

            return View(dropboxContents);
        }

        [HttpPost]
        public ActionResult DropboxAccountContents(DropboxFolderGotoViewModel model)
        {
            TempData["folderContents"] = model;
            var redirectToUrl = Url.Action("DropboxAccountContents", "DropboxContent");
            return Json(new { localUrl = redirectToUrl }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<PartialViewResult> InnerDropboxAccountContents(DropboxFolderGotoViewModel model)
        {
            var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            DropboxFolder dropboxFileSystem;
            List<DropboxFolder> prevFolders;
            try
            {
                dropboxFileSystem = dropboxAccountService.GetFolder(currUser, model.FolderPath, model.FolderDepth);
            }
            catch (DropboxFolderEmptyException ex)
            {
                prevFolders = dropboxAccountService.GetPreviousFolders(currUser, model.FolderPath);
                DropboxFolderContentsViewModel dropboxContentsVar = new DropboxFolderContentsViewModel
                {
                    FolderPath = model.FolderPath,
                    PreviousFolders = prevFolders
                };

                return PartialView("_EmptyFolderPartial", dropboxContentsVar);
            }

            prevFolders = dropboxAccountService.GetPreviousFolders(currUser, model.FolderPath);
            DropboxFolderContentsViewModel dropboxContents = new DropboxFolderContentsViewModel
            {
                FolderPath = dropboxFileSystem.Path,
                FolderName = dropboxFileSystem.Name,
                DropboxFolders = dropboxFileSystem.Folders,
                DropboxFiles = dropboxFileSystem.Files,
                PreviousFolders = prevFolders
            };

            return PartialView("_FolderPartial", dropboxContents);
        }

        [HttpGet]
        public async Task<ActionResult> ViewDropboxFile(int dropboxFileId)
        {
            var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            var dropboxFile = await dropboxAccountService.GetFile(currUser, dropboxFileId);

            if (dropboxFile == null)
            {
                ViewBag.FileNotFound = "File not found.";
                return View();
            }

            DropboxFileViewModel fileVM = new DropboxFileViewModel
            {
                FileName = dropboxFile.Name,
                FilePath = dropboxFile.Path,
                FileSize = dropboxFile.Size,
                Depth = dropboxFile.Depth,
                LinkToFile = dropboxFile.LinkToFile,
                PreviousFolders = dropboxAccountService.GetPreviousFolders(currUser, dropboxFile.Path, false)
            };

            return View(fileVM);
        }

        [HttpGet]
        [EmailConfirmed]
        public async Task<ActionResult> GrantedFoldersToMe()
        {
            var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            List<DropboxFolderGrantedToClient> grantedFolders;
            try
            {
                grantedFolders = dropboxAccountService.GetFoldersGrantedToClient(currUser);
            }
            catch (NoFoldersGrantedToClientException ex)
            {
                // do handling of situation when client has 0 granted to him folders
                return View(new GrantedFoldersToClientViewModel());
            }

            GrantedFoldersToClientViewModel model = new GrantedFoldersToClientViewModel
            {
                Folders = grantedFolders
            };

            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> ViewGrantedToClientFolder(int folderId)
        {
            var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            DropboxFolderGrantedToClient folder;
            try
            {
                folder = dropboxAccountService.GetFolderGrantedToClient(currUser, folderId);
            }
            catch (NoAccessToContentException ex)
            {
                return View("Error");
            }
            catch (DropboxFolderEmptyException ex)
            {
                folder = new DropboxFolderGrantedToClient
                {
                    Photographer = ex.FolderInfo.Photographer,
                    FolderName = ex.FolderInfo.FolderName
                };
                return View("ViewGrantedToClientFolder", folder);
            }
            catch (ContentNotFoundException ex)
            {
                return View("Error");
            }
            
            return View("ViewGrantedToClientFolder", folder);
        }

        public ActionResult DownloadByteArr(byte[] stream, string filename)
        {
            return File(stream, "image/jpg", filename);
        }

        [HttpPost]
        public async Task<ActionResult> DownloadChosenPhotos(DropboxFolderGrantedToClient model)
        {
            var selectedPhotos = model.Files.Where(ph => ph.ToDownload).ToList();

            if(selectedPhotos.Count == 0)
            {
                return View("_DropboxFolderGrantedToClientPartial", model);
            }

            var photographer = await UserManager.FindByIdAsync(model.Photographer.Id);
            
            //await dropboxAccountService.DownloadFiles(photographer, selectedPhotos);
            var bytearrs = await dropboxAccountService.GetFilesByteArrs(photographer, selectedPhotos);

            //return File(bytearrs.FirstOrDefault().Key, "image/jpg", bytearrs.FirstOrDefault().Value);

            using(var memoryStream = new MemoryStream())
            {
                using(var ziparchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach(var photo in bytearrs)
                    {
                        var zipEntry = ziparchive.CreateEntry(photo.Value, CompressionLevel.Fastest);
                        using(var zipStream = zipEntry.Open())
                        {
                            zipStream.Write(photo.Key, 0, photo.Key.Length);
                        }
                    }
                }

                return File(memoryStream.ToArray(), "application/zip", "Photos.zip");
            }
        }

        [HttpGet]
        public async Task<ActionResult> ViewGrantedToClientFile(int dropboxFileId)
        {
            var currUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            DropboxFileGrantedToClient dropboxFile;
            try
            {
                dropboxFile = await dropboxAccountService.GetClientFile(currUser, dropboxFileId);
            }
            catch (NoAccessToContentException ex)
            {
                return View("Error");
            }
            catch (ContentNotFoundException ex)
            {
                return View("Error");
            }

            DropboxFileGrantedToClientViewModel model = new DropboxFileGrantedToClientViewModel
            {
                File = dropboxFile,
                FolderId = dropboxAccountService.GetFolderId(dropboxFileId)
            };

            return View("ViewGrantedToClientFile", model);
        }
    }
}