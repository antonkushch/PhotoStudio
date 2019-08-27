using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;

namespace PhotoStudioDiploma.Services.FileExtensionsManager
{
    public class FileExtensions
    {
        private static readonly ReadOnlyCollection<string> _fileExtensionsWhiteList = new ReadOnlyCollection<string>(
        new[]
        {
            ".jpg",
            ".jpeg",
            ".png"
        });

        public static ReadOnlyCollection<string> FileExtensionsWhiteList
        {
            get { return _fileExtensionsWhiteList; }
        }

        private string GetFileExtension(string filepath)
        {
            int beginExtension = filepath.LastIndexOf(".");
            var extension = filepath.Substring(beginExtension);
            return extension;
        }

        public bool IsAllowedFileExtension(string fileName)
        {
            return _fileExtensionsWhiteList.Contains(GetFileExtension(fileName));
        }
    }
}