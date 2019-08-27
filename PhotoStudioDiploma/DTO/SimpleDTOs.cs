using PhotoStudioDiploma.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoStudioDiploma.DALModels
{
    public class AccountManager
    {
        public ApplicationUserManager UserManager { get; set; }
        public ApplicationSignInManager SignInManager { get; set; }
    }

    public class DropboxAuth
    {
        public string Code { get; set; }
        public string State { get; set; }
    }

    public class LoginDTO
    {
        public LoginViewModel LoginViewModel { get; set; }
    }

    public class RegisterDTO
    {
        public RegisterViewModel RegisterViewModel { get; set; }
    }
}