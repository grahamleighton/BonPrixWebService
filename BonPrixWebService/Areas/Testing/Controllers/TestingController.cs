using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace BonPrixWebService.Areas.Testing.Controllers
{
    public class TestingController : Controller
    {

        public ActionResult XSDView()
        {
            String xsdRoot = "../../xsd";
            ViewBag.fileSuccessList = "";
            List<String> fls = new List<String>();

            try
            {
                var fils = System.IO.Directory.EnumerateFiles(Server.MapPath(xsdRoot));


                foreach (var f in fils)
                {

                    fls.Add(xsdRoot +  "/" + Path.GetFileName(f));

                }
                ViewBag.vb_fls = fls;
            }
            catch (Exception E)
            {
                fls.Clear();

                ViewBag.vb_fls = "";
                ViewBag.errmsg = E.Message;
            }



            return View();

        }


        private XDocument createResponse(string XSDName, string XMLName, bool XMLValid, List<String> infomsgs, List<String> errmsgs)
        {

            XDocument theDoc = new XDocument();
            XElement root = new XElement("ROOT");

            root.Add(new XElement("XSD", XSDName));
            root.Add(new XElement("XMLTYPE", XMLName));
            root.Add(new XElement("LENGTH", System.Web.HttpContext.Current.Request.ContentLength));
            root.Add(new XElement("ORIGIN", System.Web.HttpContext.Current.Request.UserHostAddress));
            root.Add(new XElement("TIME", DateTime.Now.ToUniversalTime()));

            theDoc.Add(root);
            if (XMLValid)
                root.Add(new XElement("RESULT", "OK"));
            else
                root.Add(new XElement("RESULT", "FAIL"));

            if (infomsgs.Count > 0)
            {
                XElement info_root = new XElement("INFORMATION");

                foreach (var m in infomsgs)
                {
                    info_root.Add(new XElement("INFO", m.ToString()));
                }
                root.Add(info_root);
            }

            if (errmsgs.Count > 0)
            {
                XElement err_root = new XElement("ERRORS");

                foreach (var m in errmsgs)
                {
                    err_root.Add(new XElement("ERROR", m.ToString()));
                }
                root.Add(err_root);
            }


            return theDoc;

        }

        public ActionResult FileView()
        {
            ViewBag.fileSuccessList = "";
            String xmlRoot = "../../xml";
            try

            {
                var fils2 = System.IO.Directory.EnumerateFiles(Server.MapPath(xmlRoot + "/success"));

            }
            catch (Exception e)
            {
                ViewBag.smp = "Cannot find files in " + Server.MapPath(xmlRoot + "/success");
                ViewBag.vb_fls = "";
                ViewBag.vb_cdts = "";
                ViewBag.vb_flf =  "";
                ViewBag.vb_cdtf = "";
                return View();
            }
            var fils = System.IO.Directory.EnumerateFiles(Server.MapPath(xmlRoot + "/success"));


            List<String> fls = new List<String>();
            List<String> flf = new List<String>();
            List<String> cdts = new List<String>();
            List<String> cdtf = new List<String>();


//            ViewBag.smp = Server.MapPath("/");

            foreach ( var f in fils )
            {
                DateTime cdt = System.IO.File.GetCreationTime(f);
                string fnm = f.Substring(Server.MapPath("").Length-3);
                fls.Add(xmlRoot + "/success/" + Path.GetFileName(f)) ;
                cdts.Add(cdt.ToString("yyyy-MM-dd hh:mm:ss"));

            }

            ViewBag.fileFailureList = "";

            fils = System.IO.Directory.EnumerateFiles(Server.MapPath(xmlRoot + "/failure"));

            foreach (var f in fils)
            {
                DateTime cdt = System.IO.File.GetCreationTime(f);
                string fnm = f.Substring(Server.MapPath("").Length - 3);
                flf.Add( xmlRoot + "/failure/" + Path.GetFileName(f));

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
