using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PhotoStudioDiploma.Controllers.CustomAttributes
{
    public class EmailConfirmedAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var userId = filterContext.HttpContext.User.Identity.GetUserId();
            
            var httpContext = filterContext.HttpContext;
            var userManager = httpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            var user = userManager.FindByIdAsync(userId);

            if (!user.Result.EmailConfirmed)
            {
                httpContext.Response.Redirect("/Account/EmailConfirmationNeeded", false);
            }
        }
    }
}