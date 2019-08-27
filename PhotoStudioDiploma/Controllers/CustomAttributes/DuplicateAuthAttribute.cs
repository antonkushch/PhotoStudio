using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PhotoStudioDiploma.Controllers.CustomAttributes
{
    public class DuplicateAuthAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var httpContext = filterContext.HttpContext;
            var identity = httpContext.User.Identity;

            if (identity.IsAuthenticated)
            {
                httpContext.Response.Redirect("/Account/Error", false);
            }
        }
    }
}