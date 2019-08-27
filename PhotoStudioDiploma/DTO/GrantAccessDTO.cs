using PhotoStudioDiploma.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoStudioDiploma.DTO
{
    public class GrantAccessDTO
    {
        public ApplicationUser CurrUser { get; set; }
        public List<int> Folders { get; set; }
        public List<string> Users { get; set; }
    }

    public class GrantAccessToUserDTO
    {
        public ApplicationUser CurrUser { get; set; }
        public List<int> Folders { get; set; }
        public string User { get; set; }
    }
}