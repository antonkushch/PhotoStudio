using Dropbox.Api;
using PhotoStudioDiploma.DTO;
using PhotoStudioDiploma.Exceptions;
using PhotoStudioDiploma.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using System.Diagnostics;
using Microsoft.AspNet.Identity.Owin;
using Dropbox.Api.Sharing;
using System.Web.Mvc;
using PhotoStudioDiploma.Services.SyncDropboxAccountManager;
using PhotoStudioDiploma.Services.FileExtensionsManager;
using System.IO;

namespace PhotoStudioDiploma.Services
{
    public interface IDropboxAccountService
    {
        Task<string> GetDropboxAccountInfo(ApplicationUser currUser);
        Task<DropboxAccountFileSystem> GetContentsOfDropboxAccount(ApplicationUser currUser);
        DropboxAccountFileSystem GetDropboxAccountFileSystem();
        Task AddDropboxContentsToDB(ApplicationUser currUser);
        //DropboxFolder GetRootFolder(ApplicationUser currUser);
        DropboxFolderGrantedToClient GetFolderGrantedToClient(int folderId);
        DropboxFolder GetFolder(ApplicationUser currUser, string folderpath, int depth);
        //DropboxFile GetFile(int fileId);
        Task<DropboxFile> GetFile(ApplicationUser currUser, int fileId);
        List<DropboxFolder> GetPreviousFolders(ApplicationUser currUser, string folderpath, bool isPathToFolder = true);
        List<DropboxFolder> GetAllFoldersWithGrantedUsers(ApplicationUser currUser);
        IEnumerable<SelectListItem> GetSelectListItemOfAllFolders(ApplicationUser currUser);

        // for client
        List<DropboxFolderGrantedToClient> GetFoldersGrantedToClient(ApplicationUser currUser);
        DropboxFolderGrantedToClient GetFolderGrantedToClient(ApplicationUser currUser, int folderId);
        Task<DropboxFileGrantedToClient> GetClientFile(ApplicationUser currUser, int fileId);
        int GetFolderId(int fileId);
        ApplicationUserDTO GetFolderOwner(int folderId);
        Task DownloadFiles(ApplicationUser photographer, List<DropboxFileGrantedToClient> files);
        Task<Dictionary<byte[], string>> GetFilesByteArrs(ApplicationUser photographer, List<DropboxFileGrantedToClient> files);

        // synchronizing dropbox account
        Task<SyncDropboxAccount> SyncDropboxAccountContents(ApplicationUser currUser);
        Task<bool> CheckIfSyncWithDropboxIsNeeded(ApplicationUser currUser);
    }

    public class DropboxAccountService : IDropboxAccountService
    {
        private DropboxAccountFileSystem dropboxFileSystem = new DropboxAccountFileSystem();
        private readonly ApplicationDbContext db;
        private FileExtensions fileExtensionsHelper;

        public DropboxAccountService()
        {
            db = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
            db.Configuration.ProxyCreationEnabled = true;
            db.Configuration.LazyLoadingEnabled = true;
            fileExtensionsHelper = new FileExtensions();
        }

        public DropboxAccountFileSystem GetDropboxAccountFileSystem()
        {
            if (dropboxFileSystem.RootFolder.Files.Count == 0 && dropboxFileSystem.RootFolder.Folders.Count == 0)
            {
                throw new DropboxFolderEmptyException("Your Dropbox account is empty.");
            }
            else
            {
                return dropboxFileSystem;
            }
        }

        public async Task<DropboxAccountFileSystem> /*Task<List<string>>*/ GetContentsOfDropboxAccount(ApplicationUser currUser)
        {
            if (currUser.PhotographerInfo != null && currUser.PhotographerInfo.DropboxAccessToken != null)
            {
                using (var dbx = new DropboxClient(currUser.PhotographerInfo.DropboxAccessToken))
                {
                    var allFiles = new List<string>();
                    //return await FindRecursive(dbx, string.Empty, allFiles);
                    await FindRecursive(dbx, string.Empty, dropboxFileSystem.RootFolder);
                }
            }

            return GetDropboxAccountFileSystem();
        }

        private async Task FindRecursive(DropboxClient dbx, string filepath, DropboxFolder folder)
        {
            var list = await dbx.Files.ListFolderAsync(filepath);

            foreach (var item in list.Entries.Where(i => i.IsFile))
            {
                if (fileExtensionsHelper.IsAllowedFileExtension(item.PathLower))
                {
                    var thumbnailResult = await dbx.Files.GetThumbnailAsync(item.PathLower);
                    var imageByteArr = await thumbnailResult.GetContentAsByteArrayAsync();
                    DropboxFile dropboxFile = new DropboxFile(item.PathLower, item.Name, Convert.ToInt64(item.AsFile.Size), folder.Depth, imageByteArr);
                    folder.AddFile(dropboxFile);
                }
            }

            int newDepth = folder.Depth;
            newDepth++;
            foreach (var item in list.Entries.Where(i => i.IsFolder))
            {
                DropboxFolder dropboxFolder = new DropboxFolder(item.PathLower, item.Name, newDepth);
                folder.AddFolder(dropboxFolder);
                await FindRecursive(dbx, item.PathLower, dropboxFolder);
            }
        }

        public async Task<string> GetDropboxAccountInfo(ApplicationUser currUser)
        {
            string accountInfo = string.Empty;
            if (currUser.PhotographerInfo != null && currUser.PhotographerInfo.DropboxAccessToken != null)
            {
                using (var dbx = new DropboxClient(currUser.PhotographerInfo.DropboxAccessToken))
                {
                    var full = await dbx.Users.GetCurrentAccountAsync();
                    accountInfo = $"{full.Name.DisplayName} - {full.Email}";
                }
            }
            return accountInfo;
        }
        
        public async Task AddDropboxContentsToDB(ApplicationUser currUser)
        {
            if (currUser.PhotographerInfo != null && currUser.PhotographerInfo.DropboxAccessToken != null)
            {
                using (var dbx = new DropboxClient(currUser.PhotographerInfo.DropboxAccessToken))
                {
                    var allFiles = new List<string>();
                    //return await FindRecursive(dbx, string.Empty, allFiles);
                    await TraverseRecursive(dbx, string.Empty, dropboxFileSystem.RootFolder, currUser);
                }
            }
        }

        private async Task TraverseRecursive(DropboxClient dbx, string filepath, DropboxFolder folder, ApplicationUser currUser)
        {
            var list = await dbx.Files.ListFolderAsync(filepath);

            string dropboxFolderId;
            if (filepath == string.Empty)
            {
                dropboxFolderId = "";
            }
            else
            {
                var folderMetadata = await dbx.Files.GetMetadataAsync(filepath);
                dropboxFolderId = folderMetadata.AsFolder.Id;
            }

            // adding folder to table PhotographerFolders
            var photoFolder = new PhotographerFolder
            {
                Name = folder.Name,
                Depth = folder.Depth,
                Path = folder.Path,
                DropboxCursor = list.Cursor,
                DropboxFolderId = dropboxFolderId,
                ApplicationUserId = currUser.Id,
                ApplicationUser = currUser,
                PhotographerFiles = new List<PhotographerFile>()
            };
            db.PhotographerFolders.Add(photoFolder);
            db.SaveChanges();
            
            foreach (var item in list.Entries.Where(i => i.IsFile))
            {
                if (fileExtensionsHelper.IsAllowedFileExtension(item.PathLower))
                {
                    long fileSize = Convert.ToInt64(item.AsFile.Size);

                    var thumbnailResult = await dbx.Files.GetThumbnailAsync(item.PathLower);
                    var imageByteArr = await thumbnailResult.GetContentAsByteArrayAsync();
                    DropboxFile dropboxFile = new DropboxFile(item.PathLower, item.Name, fileSize, folder.Depth, imageByteArr);
                    folder.AddFile(dropboxFile);

                    // adding file to table PhotographerFiles
                    var photoFile = new PhotographerFile
                    {
                        Name = item.Name,
                        Size = fileSize,
                        Depth = folder.Depth,
                        Path = item.PathLower,
                        ThumbnailImage = imageByteArr,
                        DropboxFileId = item.AsFile.Id,
                        PhotographerFolderId = photoFolder.PhotographerFolderId,
                        PhotographerFolder = photoFolder
                    };
                    db.PhotographerFiles.Add(photoFile);
                    db.SaveChanges();
                    photoFolder.PhotographerFiles.Add(photoFile);
                }
            }
            db.SaveChanges();

            currUser.PhotographerFolders.Add(photoFolder);

            int newDepth = folder.Depth;
            newDepth++;
            foreach (var item in list.Entries.Where(i => i.IsFolder))
            {
                DropboxFolder dropboxFolder = new DropboxFolder(item.PathLower, item.Name, newDepth);
                folder.AddFolder(dropboxFolder);

                await TraverseRecursive(dbx, item.PathLower, dropboxFolder, currUser);
            }
        }

        private PhotographerFolder GetFolder(int folderId)
        {
            var folder = db.PhotographerFolders.SingleOrDefault(f => f.PhotographerFolderId == folderId);
            if(folder == null)
            {
                throw new ContentNotFoundException("This folder is empty.");
            }

            return folder;
        }

        public DropboxFolderGrantedToClient GetFolderGrantedToClient(int folderId)
        {
            var dbFolder = GetFolder(folderId);
            if(dbFolder.PhotographerFiles == null || dbFolder.PhotographerFiles.Count == 0)
            {
                throw new DropboxFolderEmptyException(new DropboxFolderGrantedToClient { FolderId = folderId, FolderName = dbFolder.Name, Photographer = new ApplicationUserDTO { Name = dbFolder.ApplicationUser.Name, Surname = dbFolder.ApplicationUser.Surname, Email = dbFolder.ApplicationUser.Email } });
            }

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<PhotographerFile, DropboxFileGrantedToClient>();
                cfg.CreateMap<PhotographerFolder, DropboxFolderGrantedToClient>().ForMember(dest => dest.Files, opt => opt.MapFrom(src => src.PhotographerFiles))
                                                                                    .ForMember(dest => dest.Photographer, opt => opt.MapFrom(src => src.ApplicationUser))
                                                                                    .ForMember(dest => dest.FolderName, opt => opt.MapFrom(src => src.Name))
                                                                                    .ForMember(dest => dest.FolderId, opt => opt.MapFrom(src => src.PhotographerFolderId));
            });

            var mapper = config.CreateMapper();
            var folder = mapper.Map<PhotographerFolder, DropboxFolderGrantedToClient>(dbFolder);

            return folder;
        }

        public DropboxFolderGrantedToClient GetFolderGrantedToClient(ApplicationUser currUser, int folderId)
        {
            var dbFolder = GetFolder(folderId);

            if (!currUser.GrantedFolders.Contains(dbFolder))
            {
                throw new NoAccessToContentException("Access to folder denied.");
            }

            if (dbFolder.PhotographerFiles == null || dbFolder.PhotographerFiles.Count == 0)
            {
                throw new DropboxFolderEmptyException(new DropboxFolderGrantedToClient { FolderId = folderId, FolderName = dbFolder.Name, Photographer = new ApplicationUserDTO { Name = dbFolder.ApplicationUser.Name, Surname = dbFolder.ApplicationUser.Surname, Email = dbFolder.ApplicationUser.Email } });
            }

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<PhotographerFile, DropboxFileGrantedToClient>();
                cfg.CreateMap<PhotographerFolder, DropboxFolderGrantedToClient>().ForMember(dest => dest.Files, opt => opt.MapFrom(src => src.PhotographerFiles))
                                                                                    .ForMember(dest => dest.Photographer, opt => opt.MapFrom(src => src.ApplicationUser))
                                                                                    .ForMember(dest => dest.FolderName, opt => opt.MapFrom(src => src.Name))
                                                                                    .ForMember(dest => dest.FolderId, opt => opt.MapFrom(src => src.PhotographerFolderId));
            });

            var mapper = config.CreateMapper();
            var folder = mapper.Map<PhotographerFolder, DropboxFolderGrantedToClient>(dbFolder);

            return folder;
        }

        public ApplicationUserDTO GetFolderOwner(int folderId)
        {
            var dbFolder = GetFolder(folderId);
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ApplicationUser, ApplicationUserDTO>();
            });
            var mapper = config.CreateMapper();
            var folderOwner = mapper.Map<ApplicationUser, ApplicationUserDTO>(dbFolder.ApplicationUser);

            return folderOwner;
        }

        private string GetFolderPathWithoutFolderName(string folderPath)
        {
            int folderNameIndex = folderPath.LastIndexOf('/');
            return folderNameIndex == 0 ? folderPath.Substring(0, folderNameIndex + 1) : folderPath.Substring(0, folderNameIndex);
        }

        public DropboxFolder GetFolder(ApplicationUser currUser, string folderpath, int depth)
        {
            var folder = currUser.PhotographerFolders.FirstOrDefault(c => c.ApplicationUserId == currUser.Id && c.Depth == depth && c.Path == folderpath);
            var nestedFolders = currUser.PhotographerFolders.Where(c => c.ApplicationUserId == currUser.Id && c.Depth == depth + 1 && GetFolderPathWithoutFolderName(c.Path) == folderpath).ToList();

            if (folder.PhotographerFiles.Count == 0 && nestedFolders.Count() == 0)
            {
                throw new DropboxFolderEmptyException($"The dropbox folder {folder.Path} is empty");
            }

            if (folder == null)
            {
                return null;
            }

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<PhotographerFile, DropboxFile>();
                cfg.CreateMap<PhotographerFolder, DropboxFolder>().ForMember(dest => dest.Files, opt => opt.MapFrom(src => src.PhotographerFiles));
            });

            var config2 = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<PhotographerFolder, DropboxFolder>();
            });

            var mapper = config.CreateMapper();
            var root = mapper.Map<PhotographerFolder, DropboxFolder>(folder);

            var mapper2 = config2.CreateMapper();
            var nested = mapper2.Map<IEnumerable<PhotographerFolder>, List<DropboxFolder>>(nestedFolders);

            root.Folders = nested;
            
            return root;
        }

        // for photographer
        public List<DropboxFolder> GetAllFoldersWithGrantedUsers(ApplicationUser currUser)
        {
            var allDBFolders = currUser.PhotographerFolders.Where(c => c.ApplicationUserId == currUser.Id && c.GrantedUsers.Count != 0).ToList();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<PhotographerFile, DropboxFile>();
                cfg.CreateMap<ApplicationUser, ApplicationUserDTO>();
                cfg.CreateMap<PhotographerFolder, DropboxFolder>().ForMember(dest => dest.Files, opt => opt.MapFrom(src => src.PhotographerFiles))
                                                                    .ForMember(dest => dest.GrantedUsers, opt => opt.MapFrom(src => src.GrantedUsers));
            });

            var mapper = config.CreateMapper();
            var allFolders = mapper.Map<IEnumerable<PhotographerFolder>, List<DropboxFolder>>(allDBFolders);

            return allFolders;
        }

        public IEnumerable<SelectListItem> GetSelectListItemOfAllFolders(ApplicationUser currUser)
        {
            var allFolders = currUser.PhotographerFolders.Where(c => c.ApplicationUserId == currUser.Id).ToList();
            var selectListItemOfFolders = allFolders.Select(o => new SelectListItem
            {
                Text = o.Path,
                Value = o.PhotographerFolderId.ToString()
            });

            return selectListItemOfFolders;
        }

        public List<DropboxFolder> GetPreviousFolders(ApplicationUser currUser, string folderpath, bool isPathToFolder = true)
        {
            var allFolders = currUser.PhotographerFolders.Where(c => c.ApplicationUserId == currUser.Id).ToList();

            var previousDbFolders = new List<PhotographerFolder>();

            // adding current folder
            if (isPathToFolder)
            {
                if (allFolders.Any(c => c.Path == folderpath))
                {
                    previousDbFolders.Add(allFolders.First(c => c.Path == folderpath));
                }
            }

            // adding previous folders
            while(folderpath != "/")
            {
                var tempPrevFolderPath = GetFolderPathWithoutFolderName(folderpath);
                if(allFolders.Any(c => c.Path == tempPrevFolderPath))
                {
                    previousDbFolders.Add(allFolders.First(c => c.Path == tempPrevFolderPath));
                }
                folderpath = tempPrevFolderPath;
            }

            var config2 = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<PhotographerFolder, DropboxFolder>();
            });

            var mapper = config2.CreateMapper();
            var previousFolders = mapper.Map<List<PhotographerFolder>, List<DropboxFolder>>(previousDbFolders);

            var orderedPrevFolders = previousFolders.OrderBy(c => c.Depth);

            return orderedPrevFolders.ToList();
        }

        public async Task<DropboxFile> GetFile(ApplicationUser currUser, int fileId)
        {
            var dbfile = db.PhotographerFiles.SingleOrDefault(c => c.PhotographerFileId == fileId);

            if(dbfile == null)
            {
                return null;
            }

            string linkToFile = string.Empty;
            if (currUser.PhotographerInfo != null && currUser.PhotographerInfo.DropboxAccessToken != null)
            {
                using (var dbx = new DropboxClient(currUser.PhotographerInfo.DropboxAccessToken))
                {
                    var linkMetadata = await dbx.Files.GetTemporaryLinkAsync(dbfile.Path);
                    
                    linkToFile = linkMetadata.Link;
                }
            }

            DropboxFile file = new DropboxFile
            {
                Name = dbfile.Name,
                Path = dbfile.Path,
                Size = dbfile.Size,
                Depth = dbfile.Depth,
                LinkToFile = linkToFile
            };

            return file;
        }

        // for client
        public List<DropboxFolderGrantedToClient> GetFoldersGrantedToClient(ApplicationUser currUser)
        {
            var grantedFolders = currUser.GrantedFolders.ToList();

            if(grantedFolders == null || grantedFolders.Count == 0)
            {
                throw new NoFoldersGrantedToClientException("You have 0 folders granted to you");
            }

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<PhotographerFile, DropboxFileGrantedToClient>();
                cfg.CreateMap<ApplicationUser, ApplicationUserDTO>();
                cfg.CreateMap<PhotographerFolder, DropboxFolderGrantedToClient>().ForMember(dest => dest.Files, opt => opt.MapFrom(src => src.PhotographerFiles))
                                                                                    .ForMember(dest => dest.Photographer, opt => opt.MapFrom(src => src.ApplicationUser))
                                                                                    .ForMember(dest => dest.FolderName, opt => opt.MapFrom(src => src.Name))
                                                                                    .ForMember(dest => dest.FolderId, opt => opt.MapFrom(src => src.PhotographerFolderId));
            });

            var mapper = config.CreateMapper();
            var folders = mapper.Map<IEnumerable<PhotographerFolder>, List<DropboxFolderGrantedToClient>>(grantedFolders);

            return folders;
        }

        public async Task<DropboxFileGrantedToClient> GetClientFile(ApplicationUser currUser, int fileId)
        {
            var dbfile = db.PhotographerFiles.SingleOrDefault(c => c.PhotographerFileId == fileId);
            if(dbfile == null)
            {
                throw new ContentNotFoundException("File not found.");
            }

            var dbfolder = dbfile.PhotographerFolder;
            if (!currUser.GrantedFolders.Contains(dbfolder))
            {
                // this user doesn't have access to folder that has a file with id == fileId
                throw new NoAccessToContentException("Access to file denied.");
            }

            string linkToFile = string.Empty;
            var ownerPhotographer = dbfolder.ApplicationUser;

            if (ownerPhotographer.PhotographerInfo != null && ownerPhotographer.PhotographerInfo.DropboxAccessToken != null)
            {
                using (var dbx = new DropboxClient(ownerPhotographer.PhotographerInfo.DropboxAccessToken))
                {
                    var linkMetadata = await dbx.Files.GetTemporaryLinkAsync(dbfile.Path);
                    linkToFile = linkMetadata.Link;
                }
            }

            DropboxFileGrantedToClient file = new DropboxFileGrantedToClient
            {
                Name = dbfile.Name,
                LinkToFile = linkToFile,
                Size = dbfile.Size
            };

            return file;
        }

        public int GetFolderId(int fileId)
        {
            return db.PhotographerFiles.SingleOrDefault(c => c.PhotographerFileId == fileId).PhotographerFolderId;
        }

        public async Task<SyncDropboxAccount> SyncDropboxAccountContents(ApplicationUser currUser)
        {
            SyncDropboxAccount syncDropboxAccount = new SyncDropboxAccount(db, currUser);
            var synced = await syncDropboxAccount.SyncDropboxAccountContents();
            return synced;
        }

        public async Task<bool> CheckIfSyncWithDropboxIsNeeded(ApplicationUser currUser)
        {
            SyncDropboxAccount syncDropboxAccount = new SyncDropboxAccount(db, currUser);
            var syncNeeded = await syncDropboxAccount.IsSyncWithDropboxNeeded();

            return syncNeeded;
        }

        public async Task DownloadFiles(ApplicationUser photographer, List<DropboxFileGrantedToClient> files)
        {
            using (var dbx = new DropboxClient(photographer.PhotographerInfo.DropboxAccessToken))
            {
                foreach(var photo in files)
                {
                    using (var response = await dbx.Files.DownloadAsync(db.PhotographerFiles.Single(ph => ph.PhotographerFileId == photo.PhotographerFileId).Path))
                    {
                        using(var filestream = File.Create($"C:\\diploma\\{photo.Name}"))
                        {
                            (await response.GetContentAsStreamAsync()).CopyTo(filestream);
                        }
                    }
                }
            }
        }

        public async Task<Dictionary<byte[], string>> GetFilesByteArrs(ApplicationUser photographer, List<DropboxFileGrantedToClient> files)
        {
            Dictionary<byte[], string> dict = new Dictionary<byte[], string>();
            using (var dbx = new DropboxClient(photographer.PhotographerInfo.DropboxAccessToken))
            {
                foreach (var photo in files)
                {
                    using (var response = await dbx.Files.DownloadAsync(db.PhotographerFiles.Single(ph => ph.PhotographerFileId == photo.PhotographerFileId).Path))
                    {
                        var bytearr = await response.GetContentAsByteArrayAsync();
                        dict.Add(bytearr, photo.Name);
                    }
                }
            }
            return dict;
        }
    }
}