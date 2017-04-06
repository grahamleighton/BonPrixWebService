using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace BonPrixWebService.Controllers
{
    public class ProductDataController : ApiController
    {
        // GET api/<controller>
        public Task<HttpResponseMessage> Get()
        {
            OpenXMLController newapi = new OpenXMLController();

            return newapi.Get();
        }
        // GET api/<controller>/5
        public Task<HttpResponseMessage> Get(int id)
        {
            OpenXMLController newapi = new OpenXMLController();

            return newapi.Get(id);
        }
        public Task<HttpResponseMessage> Post([FromBody]string value)
        {
//            var currentContext = System.Web.HttpContext.Current;
//            int rl = currentContext.Request.ContentLength;
            OpenXMLController newapi = new OpenXMLController();
            newapi.setCaller("productdata");
           
            return newapi.Post2();


        }


        // PUT: api/DispatchNotification2/5
        public Task<HttpResponseMessage> Put(int id, [FromBody]string value)
        {
            OpenXMLController newapi = new OpenXMLController();

            return newapi.Put(id, value);

        }

        // DELETE: api/DispatchNotification2/5
        public Task<HttpResponseMessage> Delete(int id)
        {
            OpenXMLController newapi = new OpenXMLController();

            return newapi.Delete(id);

        }
    }
}