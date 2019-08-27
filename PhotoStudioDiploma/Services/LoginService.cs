using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using PhotoStudioDiploma.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace PhotoStudioDiploma.Services
{
    public interface ILoginService
    {
        Task<SignInStatus> LoginUser(LoginViewModel user);
    }

    public class LoginService : ILoginService
    {
        private ApplicationUserManager userManager;
        private ApplicationSignInManager signInManager;

        public LoginService(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        public async Task<SignInStatus> LoginUser(LoginViewModel user)
        {
            var appUser = await userManager.FindByEmailAsync(user.Email);
            if(!VerifyCorrectPasswordAndEmail(appUser, user.Password))
            {
                return SignInStatus.Failure;
            }
            var result = await signInManager.PasswordSignInAsync(appUser.UserName, user.Password, user.RememberMe, shouldLockout: false);
            return result;
        }

        private bool VerifyCorrectPasswordAndEmail(ApplicationUser user, string password)
        {
            if (user == null)
            {
                return false;
            }
            PasswordHasher passwordHasher = new PasswordHasher();
            var passVerificationResult = passwordHasher.VerifyHashedPassword(user.PasswordHash, password);
            if (passVerificationResult == PasswordVerificationResult.Success)
            {
                return true;
            }
            return false;
        }
    }
}