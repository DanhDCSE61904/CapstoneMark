using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CapstoneProject.Areas.Students.Controllers
{
    public class StudentsController : Controller
    {
        // GET: Students/Students
        public ActionResult Index()
        {
            return View();
        }
    }
}