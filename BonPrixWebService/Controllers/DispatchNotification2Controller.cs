using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace BonPrixWebService.Controllers
{
    public class DispatchNotification2Controller : ApiController
    {
        // GET: api/DispatchNotification2
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/DispatchNotification2/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/DispatchNotification2
        public string Post([FromBody]string value)
        {
            System.Web.HttpContext.Current.Request.SaveAs("C:\\program files\\iis express\\xml\\text.xml", false);
            //    var task = Task.Factory.StartNew(() =>
            //      {
            var bodyStream = new StreamReader(System.Web.HttpContext.Current.Request.InputStream);
            bodyStream.BaseStream.Seek(0, SeekOrigin.Begin);
            var bodyText = bodyStream.ReadToEnd();

            XDocument document = XDocument.Parse(bodyText);

            XmlSchemaSet schemaSet = new XmlSchemaSet();

            schemaSet.Add(null, "c:\\program files\\iis express\\xsd\\DispatchNotification.xsd");
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;

            var msgs = new List<string>();
            document.Validate(schemaSet, (s, e) => msgs.Add(e.Message));
            if (msgs.Count == 0)
            {
                return bodyText;
            }
            else
            {
                string rsp = "";

                foreach (var m in msgs)
                {
                    rsp = rsp + "\n" + m;
                }

                return rsp;

            }
        }

        // PUT: api/DispatchNotification2/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/DispatchNotification2/5
        public void Delete(int id)
        {
        }
    }
}
