using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.ModelBinding;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Ninject.Activation;
using Ninject.Modules;
using Ninject.Web.Common;
using PhotoStudioDiploma.Controllers;
using PhotoStudioDiploma.Models;
using PhotoStudioDiploma.Services;

namespace PhotoStudioDiploma.Util
{
    public class NinjectRegistrations : NinjectModule
    {
        public override void Load()
        {
            Bind<IRegistrationService>().To<RegistrationService>();
            Bind<ILoginService>().To<LoginService>();
            Bind<IProfileService>().To<ProfileService>();
            Bind<IDropboxAccountService>().To<DropboxAccountService>();
            Bind<IGrantAccessService>().To<GrantAccessService>();
            Bind<IUserStore<ApplicationUser>>().To<UserStore<ApplicationUser>>();
            Bind<IAuthenticationManager>().ToMethod(c => HttpContext.Current.GetOwinContext().Authentication).InRequestScope();
            Bind<ApplicationUserManager>().ToMethod(c => HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>()).InRequestScope();
        }

        private static T GetOwinInjection<T>(IContext context) where T : class
        {
            var contextBase = new HttpContextWrapper(HttpContext.Current);
            return contextBase.GetOwinContext().Get<T>();
        }
    }
}