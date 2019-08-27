using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using PhotoStudioDiploma.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PhotoStudioDiploma.Controllers
{
    public class HomeController : Controller
    {
        //ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            //var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
            //var role1 = new IdentityRole { Name = "photographer" };
            //var role2 = new IdentityRole { Name = "client" };

            //roleManager.Create(role1);
            //roleManager.Create(role2);
            return View();
        }

        //public ActionResult About()
        //{
        //    ViewBag.Message = "Photo Studio description page.";

        //    return View();
        //}

        //public ActionResult Contact()
        //{
        //    ViewBag.Message = "Contact us.";

        //    return View();
        //}
    }
}