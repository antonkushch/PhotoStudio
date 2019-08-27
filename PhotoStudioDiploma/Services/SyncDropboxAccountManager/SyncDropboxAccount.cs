using Dropbox.Api;
using Dropbox.Api.Files;
using PhotoStudioDiploma.DTO;
using PhotoStudioDiploma.Exceptions;
using PhotoStudioDiploma.Models;
using PhotoStudioDiploma.Services.FileExtensionsManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace PhotoStudioDiploma.Services.SyncDropboxAccountManager
{
    public class SyncDropboxAccount
    {
        private ApplicationDbContext Db { get; set; }
        private ApplicationUser CurrUser { get; set; }
        private DropboxClient Dbx { get; set; }
        public List<PhotographerFolder> SyncedDropboxFolders { get; set; }
        private FileExtensions fileExtensionsHelper;
        private List<Metadata> RedundantMetadata { get; set; }

        private bool _syncNeeded;
        public bool SyncNeeded
        {
            get { return SyncedDropboxFolders.Count > 0; }
            set { _syncNeeded = value; }
        }
        
        public SyncDropboxAccount(ApplicationDbContext db, ApplicationUser currUser)
        {
            Db = db;
            CurrUser = currUser;
            SyncedDropboxFolders = new List<PhotographerFolder>();
            fileExtensionsHelper = new FileExtensions();
            RedundantMetadata = new List<Metadata>();
        }

        private bool IsFolder(string path)
        {
            var folder = CurrUser.PhotographerFolders.SingleOrDefault(f => f.Path == path);
            return folder == null ? false : true;
        }

        private PhotographerFile GetFile(PhotographerFolder folder, string path)
        {
            var file = folder.PhotographerFiles.Single(f => f.Path == path);
            return file;
        }

        private PhotographerFolder GetFolder(string path)
        {
            var folder = CurrUser.PhotographerFolders.Single(f => f.Path == path);
            return folder;
        }

        private bool IsDirectSubfolder(PhotographerFolder rootFolder, PhotographerFolder folder)
        {
            if (rootFolder.Path.Length >= folder.Path.Length)
                return false;

            int slashIndex = folder.Path.LastIndexOf('/');
            if (slashIndex == 0)
                slashIndex = 1;

            return rootFolder.Path == folder.Path.Substring(0, slashIndex);
        }

        private bool IsSubfolder(PhotographerFolder rootFolder, PhotographerFolder folder)
        {
            if (rootFolder.Path.Length >= folder.Path.Length)
                return false;
            bool issub;
            if (rootFolder.Path.Length == 1)
            {
                issub = folder.Path[0] == '/';
            }
            else
            {
                issub = folder.Path[rootFolder.Path.Length] == '/';
            }
            return rootFolder.Path == folder.Path.Substring(0, rootFolder.Path.Length) && issub;
        }

        private List<PhotographerFolder> GetSubfolders(PhotographerFolder currFolder)
        {
            var folderTree = CurrUser.PhotographerFolders.Where(f => IsSubfolder(currFolder, f) == true).ToList();
            return folderTree;
        }

        private async Task RenamePath(List<PhotographerFolder> subfolders, Metadata renamedFolder, Metadata deletedFolder)
        {
            var renamedFolderPath = renamedFolder.PathLower;
            var originalFolderPath = deletedFolder.PathLower;

            foreach(var subfolder in subfolders)
            {
                var newSubfolderPath = renamedFolder.PathLower + subfolder.Path.Substring(deletedFolder.PathLower.Length);
                subfolder.Path = newSubfolderPath;
                var listSubFolder = await Dbx.Files.ListFolderAsync(newSubfolderPath);
                subfolder.DropboxCursor = listSubFolder.Cursor;

                foreach(var subfolderFile in subfolder.PhotographerFiles)
                {
                    var newSubfolderFilePath = newSubfolderPath + "/" + subfolderFile.Name;
                    subfolderFile.Path = newSubfolderFilePath;
                    Db.Entry(subfolderFile).State = System.Data.Entity.EntityState.Modified;
                }
            }

            Db.SaveChanges();
        }

        private void ManageChangedFile(PhotographerFolder folder, Metadata deletedItem, ListFolderResult changesInFolder)
        {
            var dbFile = GetFile(folder, deletedItem.AsDeleted.PathLower);
            var fileDropboxId = dbFile.DropboxFileId;
            var newFiles = changesInFolder.Entries.Where(item => item.IsFile);
            if (newFiles.Count() == 0)
            {
                // this means that the file was just deleted
                // need to delete file with "dropbox id" = fileDropboxId from db !!!
                Db.PhotographerFiles.Remove(dbFile);
                Db.SaveChanges();

                RedundantMetadata.Add(deletedItem);
                //changesInFolder.Entries.Remove(deletedItem);
                // return or break, dunno
                return;
            }
            var existingFile = newFiles.SingleOrDefault(f => f.AsFile.Id == fileDropboxId);
            if (existingFile != null) // it is a renamed file
            {
                // rename file with "dropbox id" = fileDropboxId in folder.PhotographerFiles collection
                dbFile.Name = existingFile.Name;
                dbFile.Path = existingFile.PathLower;
                Db.SaveChanges();

                // removing file from changesInFolder
                RedundantMetadata.AddRange(new List<Metadata> { deletedItem, existingFile });
                //changesInFolder.Entries.Remove(deletedItem);
                //changesInFolder.Entries.Remove(existingFile);
            }
            else // it is a deleted file
            {
                // need to delete file with "dropbox id" = fileDropboxId from db !!!
                Db.PhotographerFiles.Remove(dbFile);
                Db.SaveChanges();

                RedundantMetadata.Add(deletedItem);
                //changesInFolder.Entries.Remove(deletedItem);
            }
        }

        private async Task ManageChangedFolder(PhotographerFolder folder, Metadata deletedItem, ListFolderResult changesInFolder)
        {
            var dbFolder = GetFolder(deletedItem.AsDeleted.PathLower);
            var folderDropboxId = dbFolder.DropboxFolderId;
            var newFolders = changesInFolder.Entries.Where(item => item.IsFolder);
            if (newFolders.Count() == 0)
            {
                // this means that the folder was just deleted
                // need to delete folder with "dropbox id" = folderDropboxId from db !!!
                // also delete all subfolders of deleted folder
                var allSubFolders = GetSubfolders(dbFolder);
                allSubFolders.Add(dbFolder);
                Db.PhotographerFolders.RemoveRange(allSubFolders);
                Db.SaveChanges();

                RedundantMetadata.Add(deletedItem);
                //changesInFolder.Entries.Remove(deletedItem);
                // return or break, dunno
                return;
            }
            var existingFolder = newFolders.SingleOrDefault(f => f.AsFolder.Id == folderDropboxId);
            if (existingFolder != null) // it is a renamed folder
            {
                // TODO
                // foreach nested file and folder change path to new path
                var allSubFolders = GetSubfolders(dbFolder);
                await RenamePath(allSubFolders, existingFolder, deletedItem);
                // TODO

                // rename folder with "dropbox id" = folderDropboxId in CurrUser.PhotographerFolders collection and assign new cursor
                var listSubFolder = await Dbx.Files.ListFolderAsync(existingFolder.PathLower);
                dbFolder.DropboxCursor = listSubFolder.Cursor;
                dbFolder.Name = existingFolder.Name;
                dbFolder.Path = existingFolder.PathLower;
                foreach(var dbFile in dbFolder.PhotographerFiles)
                {
                    var newfolderFilePath = existingFolder.PathLower + "/" + dbFile.Name;
                    dbFile.Path = newfolderFilePath;
                    Db.Entry(dbFile).State = System.Data.Entity.EntityState.Modified;
                }
                
                Db.SaveChanges();

                // removing folder from changesInFolder
                RedundantMetadata.AddRange(new List<Metadata> { deletedItem, existingFolder });
                //changesInFolder.Entries.Remove(deletedItem);
                //changesInFolder.Entries.Remove(existingFolder);
            }
            else // it is a deleted folder
            {
                // need to delete folder with "dropbox id" = fileDropboxId from db !!!
                // also delete all subfolders of deleted folder
                var allSubFolders = GetSubfolders(dbFolder);
                allSubFolders.Add(dbFolder);
                Db.PhotographerFolders.RemoveRange(allSubFolders);
                Db.SaveChanges();

                RedundantMetadata.Add(deletedItem);
                //changesInFolder.Entries.Remove(deletedItem);
            }
        }

        private string GetFolderPathWithoutFolderName(string folderPath)
        {
            int folderNameIndex = folderPath.LastIndexOf('/');
            return folderNameIndex == 0 ? folderPath.Substring(0, folderNameIndex + 1) : folderPath.Substring(0, folderNameIndex);
        }

        private async Task AddFileFromDropboxToDB(Metadata file, PhotographerFolder destFolder)
        {
            long fileSize = Convert.ToInt64(file.AsFile.Size);
            var thumbnailResult = await Dbx.Files.GetThumbnailAsync(file.PathLower);
            var imageByteArr = await thumbnailResult.GetContentAsByteArrayAsync();

            var photoFile = new PhotographerFile
            {
                Name = file.Name,
                Size = fileSize,
                Depth = destFolder.Depth,
                Path = file.PathLower,
                ThumbnailImage = imageByteArr,
                DropboxFileId = file.AsFile.Id,
                PhotographerFolderId = destFolder.PhotographerFolderId,
                PhotographerFolder = destFolder
            };

            destFolder.PhotographerFiles.Add(photoFile);
            Db.SaveChanges();
        }

        private async Task AddFolderBranchFromDropboxToDB(Metadata folder, int depth)
        {
            var folderContents = await Dbx.Files.ListFolderAsync(folder.PathLower);

            var photoFolder = new PhotographerFolder
            {
                Name = folder.Name,
                Depth = depth,
                Path = folder.PathLower,
                DropboxCursor = folderContents.Cursor,
                DropboxFolderId = folder.AsFolder.Id,
                ApplicationUserId = CurrUser.Id,
                ApplicationUser = CurrUser,
                PhotographerFiles = new List<PhotographerFile>()
            };
            Db.PhotographerFolders.Add(photoFolder);
            Db.SaveChanges();

            foreach (var item in folderContents.Entries.Where(i => i.IsFile))
            {
                if (fileExtensionsHelper.IsAllowedFileExtension(item.PathLower))
                {
                    long fileSize = Convert.ToInt64(item.AsFile.Size);

                    var thumbnailResult = await Dbx.Files.GetThumbnailAsync(item.PathLower);
                    var imageByteArr = await thumbnailResult.GetContentAsByteArrayAsync();
                    //DropboxFile dropboxFile = new DropboxFile(item.PathLower, item.Name, fileSize, folder.Depth, imageByteArr);
                    //folder.AddFile(dropboxFile);

                    // adding file to table PhotographerFiles
                    var photoFile = new PhotographerFile
                    {
                        Name = item.Name,
                        Size = fileSize,
                        Depth = depth,
                        Path = item.PathLower,
                        ThumbnailImage = imageByteArr,
                        DropboxFileId = item.AsFile.Id,
                        PhotographerFolderId = photoFolder.PhotographerFolderId,
                        PhotographerFolder = photoFolder
                    };
                    Db.PhotographerFiles.Add(photoFile);
                    Db.SaveChanges();
                    photoFolder.PhotographerFiles.Add(photoFile);
                }
            }
            Db.SaveChanges();

            CurrUser.PhotographerFolders.Add(photoFolder);

            int newDepth = depth;
            newDepth++;
            foreach (var item in folderContents.Entries.Where(i => i.IsFolder))
            {
                await AddFolderBranchFromDropboxToDB(item, newDepth);
            }
        }

        private async Task SyncDropboxFolder(string folderpath)
        {
            var folder = CurrUser.PhotographerFolders.SingleOrDefault(f => f.Path == folderpath);
            if (folder == null)
            {
                // folder doesnt exist
                return;
            }

            RedundantMetadata.Clear();

            var changesInFolder = await Dbx.Files.ListFolderContinueAsync(folder.DropboxCursor);
            // update current folder "dropbox cursor" in db (changesInFolder.Cursor)
            folder.DropboxCursor = changesInFolder.Cursor;
            Db.SaveChanges();
            
            if (changesInFolder.Entries.Count == 0)
            {
                // this folder is synched with dropbox account
                // syncing subfolders if needed
                if (folderpath == "/")
                    folderpath = string.Empty;
                var subfolders = await Dbx.Files.ListFolderAsync(folderpath);
                foreach(var subfolder in subfolders.Entries.Where(subf => subf.IsFolder))
                {
                    await SyncDropboxFolder(subfolder.PathLower);
                }
                return;
            }

            // adding to list of synced folders
            SyncedDropboxFolders.Add(folder);

            foreach (var deletedItem in changesInFolder.Entries.Where(c => c.IsDeleted))
            {
                var pathDeleted = deletedItem.AsDeleted.PathLower;
                if (!IsFolder(pathDeleted)) // it is a deleted or renamed file
                {
                    ManageChangedFile(folder, deletedItem, changesInFolder);
                }
                else // it is a deleted or renamed folder
                {
                    await ManageChangedFolder(folder, deletedItem, changesInFolder);
                }
            }

            foreach (var redundantMet in RedundantMetadata)
            {
                changesInFolder.Entries.Remove(redundantMet);
            }

            // first iterating to all subfolders of current folder to check for changes
            var nestedFolders = CurrUser.PhotographerFolders.Where(f => IsDirectSubfolder(folder, f) == true).ToList();
            foreach(var nestedFolder in nestedFolders)
            {
                await SyncDropboxFolder(nestedFolder.Path);
            }

            // adding items that are left in changesInFolder to db
            foreach(var newfile in changesInFolder.Entries.Where(e => e.IsFile))
            {
                await AddFileFromDropboxToDB(newfile, folder);
            }

            foreach (var newFolder in changesInFolder.Entries.Where(e => e.IsFolder))
            {
                await AddFolderBranchFromDropboxToDB(newFolder, folder.Depth + 1);
            }
        }

        private async Task CheckIfSyncWithDropboxIsNeeded(string folderpath)
        {
            var folder = CurrUser.PhotographerFolders.SingleOrDefault(f => f.Path == folderpath);
            if (folder == null)
            {
                // folder doesnt exist
                throw new SyncNeededException("Sync is needed.");
            }

            var changesInFolder = await Dbx.Files.ListFolderContinueAsync(folder.DropboxCursor);

            if (changesInFolder.Entries.Count == 0)
            {
                if (folderpath == "/")
                    folderpath = string.Empty;
                var subfolders = await Dbx.Files.ListFolderAsync(folderpath);
                foreach (var subfolder in subfolders.Entries.Where(subf => subf.IsFolder))
                {
                    await CheckIfSyncWithDropboxIsNeeded(subfolder.PathLower);
                }
            }
            else
            {
                throw new SyncNeededException("Sync is needed.");
            }
        }

        public async Task<SyncDropboxAccount> SyncDropboxAccountContents()
        {
            if (CurrUser.PhotographerInfo != null && CurrUser.PhotographerInfo.DropboxAccessToken != null)
            {
                using (var dbx = new DropboxClient(CurrUser.PhotographerInfo.DropboxAccessToken))
                {
                    Dbx = dbx;
                    await SyncDropboxFolder("/");
                }

                return this;
            }
            return null;
        }

        public async Task<bool> IsSyncWithDropboxNeeded()
        {
            if (CurrUser.PhotographerInfo != null && CurrUser.PhotographerInfo.DropboxAccessToken != null)
            {
                using (var dbx = new DropboxClient(CurrUser.PhotographerInfo.DropboxAccessToken))
                {
                    Dbx = dbx;
                    try
                    {
                        await CheckIfSyncWithDropboxIsNeeded("/");
                    }
                    catch (SyncNeededException)
                    {
                        // sync is needed
                        return true;
                    }

                    // sync is not needed
                    return false;
                }
            }
            return false;
        }
    }
}