using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Ninject;
using Ninject.Activation;
using Ninject.Modules;
using Ninject.Web.Mvc;
using PhotoStudioDiploma.Models;
using PhotoStudioDiploma.Util;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace PhotoStudioDiploma
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static string AppKey { get; private set; }

        public static string AppSecret { get; private set; }

        protected void Application_Start()
        {
            InitializeAppKeyAndSecret();
            
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // injecting dependencies
            NinjectModule registrations = new NinjectRegistrations();
            var kernel = new StandardKernel(registrations);
            DependencyResolver.SetResolver(new NinjectDependencyResolver(kernel));
            
            //kernel.Bind<IUserStore<ApplicationUser>>().To<UserStore<ApplicationUser>>();
            //kernel.Bind<UserManager<ApplicationUser>>().ToSelf();
            kernel.Unbind<ModelValidatorProvider>();
        }

        private void InitializeAppKeyAndSecret()
        {
            var appKey = WebConfigurationManager.AppSettings["DropboxAppKey"];
            var appSecret = WebConfigurationManager.AppSettings["DropboxAppSecret"];

            AppKey = appKey;
            AppSecret = appSecret;
        }
    }
}
