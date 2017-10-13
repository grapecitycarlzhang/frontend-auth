using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GrapeLeaf.FrontEnd.Filters;

namespace GrapeLeaf.FrontEnd.Controllers
{
    public class HomeController : Controller
    {
        [Authorize]
        [FrontEndConfig]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
