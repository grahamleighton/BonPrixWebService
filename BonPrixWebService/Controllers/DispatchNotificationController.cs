using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace BonPrixWebService.Controllers
{
    public class DispatchNotificationController : ApiController
    {
        // GET: api/DispatchNotification2
        public Task<HttpResponseMessage> Get()
        {
            OpenXMLController newapi = new OpenXMLController();

            return newapi.Get();
        }

        // GET: api/DispatchNotification2/5
        public Task<HttpResponseMessage> Get(int id)
        {
            OpenXMLController newapi = new OpenXMLController();

            return newapi.Get(id);
        }

        // POST: api/DispatchNotification2
        public Task<HttpResponseMessage> Post([FromBody]string value)
        {
            OpenXMLController newapi = new OpenXMLController();

            return newapi.Post();

            
        }

        // PUT: api/DispatchNotification2/5
        public Task<HttpResponseMessage> Put(int id, [FromBody]string value)
        {
            OpenXMLController newapi = new OpenXMLController();

            return newapi.Put(id,value);

        }

        // DELETE: api/DispatchNotification2/5
        public Task<HttpResponseMessage> Delete(int id)
        {
            OpenXMLController newapi = new OpenXMLController();

            return newapi.Delete (id);

        }
    }
}
