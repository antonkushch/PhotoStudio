using Dropbox.Api;
using PhotoStudioDiploma.DALModels;
using PhotoStudioDiploma.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Web;

namespace PhotoStudioDiploma.Services
{
    public interface IRegistrationService
    {
        Task<ApplicationUser> RegisterUser(RegisterViewModel user);
        Task<bool> DropboxAuthenticationSecondStep(ApplicationUser user, string code, string state, string redirectUri);
    }

    public class RegistrationService : IRegistrationService
    {
        private ApplicationUserManager userManager;
        private ApplicationSignInManager signInManager;
        private readonly ApplicationDbContext db;
        
        public RegistrationService(ApplicationUserManager userManager, ApplicationSignInManager signInManager, ApplicationDbContext db)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.db = db;
        }

        public async Task<bool> DropboxAuthenticationSecondStep(ApplicationUser user, string code, string state, string redirectUri)
        {
            try
            {
                if (user.PhotographerInfo.ConnectState != state)
                {
                    return false;
                }

                var response = await DropboxOAuth2Helper.ProcessCodeFlowAsync(
                    code,
                    MvcApplication.AppKey,
                    MvcApplication.AppSecret,
                    redirectUri);

                user.PhotographerInfo.DropboxAccessToken = response.AccessToken;
                await userManager.UpdateAsync(user);
                                
                await signInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<ApplicationUser> RegisterUser(RegisterViewModel user)
        {
            switch (user.RegTypeEnum)
            {
                case RegTypeEnum.Client:
                    var appUser = new ApplicationUser { UserName = user.Username, Name = user.Name, Surname = user.Surname, Email = user.Email };
                    var result = await userManager.CreateAsync(appUser, user.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(appUser.Id, "client");
                        var clientInfo = new ClientInfo { ApplicationUserId = appUser.Id, ApplicationUser = appUser, ClientInteger = 5 };
                        appUser.ClientInfo = clientInfo;
                        await userManager.UpdateAsync(appUser);

                        await signInManager.SignInAsync(appUser, isPersistent: false, rememberBrowser: false);
                        
                        return appUser;
                    }
                    return null;
                case RegTypeEnum.Photographer:
                    var appUser1 = new ApplicationUser { UserName = user.Username, Name = user.Name, Surname = user.Surname, Email = user.Email };
                    var result1 = await userManager.CreateAsync(appUser1, user.Password);
                    if (result1.Succeeded)
                    {
                        await userManager.AddToRoleAsync(appUser1.Id, "photographer");
                        var photographerInfo = new PhotographerInfo { ApplicationUserId = appUser1.Id, ApplicationUser = appUser1, ConnectState = Guid.NewGuid().ToString("N") };
                        appUser1.PhotographerInfo = photographerInfo;
                        await userManager.UpdateAsync(appUser1);

                        await signInManager.SignInAsync(appUser1, isPersistent: false, rememberBrowser: false);

                        return appUser1;
                    }
                    return null;
                default:
                    return null;
            }
        }
    }
}