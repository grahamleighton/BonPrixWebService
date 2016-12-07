using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BonPrixWebService.Areas.Testing.Controllers
{
    public class TestingController : Controller
    {

        public ActionResult XSDView()
        {
            ViewBag.fileSuccessList = "";

            var fils = System.IO.Directory.EnumerateFiles(Server.MapPath("/xsd"));


            List<String> fls = new List<String>();

            foreach (var f in fils)
            {
               
                fls.Add("/xsd/" + Path.GetFileName(f)  );

            }


            ViewBag.vb_fls = fls;

            return View();

        }

        public ActionResult FileView()
        {
            ViewBag.fileSuccessList = "";

            var fils = System.IO.Directory.EnumerateFiles(Server.MapPath("/xml/success"));


            List<String> fls = new List<String>();
            List<String> flf = new List<String>();
            List<String> cdts = new List<String>();
            List<String> cdtf = new List<String>();




            foreach ( var f in fils )
            {
                DateTime cdt = System.IO.File.GetCreationTime(f);
                string fnm = f.Substring(Server.MapPath("").Length-3);
                fls.Add("/xml/success/" + Path.GetFileName(f)) ;
                cdts.Add(cdt.ToString("yyyy-MM-dd hh:mm:ss"));

            }

            ViewBag.fileFailureList = "";

            fils = System.IO.Directory.EnumerateFiles(Server.MapPath("/xml/failure"));

            foreach (var f in fils)
            {
                DateTime cdt = System.IO.File.GetCreationTime(f);
                string fnm = f.Substring(Server.MapPath("").Length - 3);
                flf.Add("/xml/failure/" + Path.GetFileName(f));

                cdtf.Add(cdt.ToString("yyyy-MM-dd hh:mm:ss"));
            }

            ViewBag.vb_fls = fls;
            ViewBag.vb_cdts = cdts;
            ViewBag.vb_flf = flf;
            ViewBag.vb_cdtf = cdtf;


            return View();
        }
        // GET: Testing/Testing
        public ActionResult Index()
        {
            return View();
        }

        // GET: Testing/Testing/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Testing/Testing/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Testing/Testing/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Testing/Testing/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Testing/Testing/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Testing/Testing/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Testing/Testing/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
