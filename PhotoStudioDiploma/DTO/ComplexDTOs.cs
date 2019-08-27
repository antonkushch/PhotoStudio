using PhotoStudioDiploma.DALModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoStudioDiploma.DTO
{
    public class DropboxAuthSecondStep : AccountManager
    {
        public string Code { get; set; }
        public string State { get; set; }
    }


}