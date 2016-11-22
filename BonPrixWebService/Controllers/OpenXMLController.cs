using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
namespace BonPrixWebService.Controllers
{
    public class OpenXMLController : ApiController
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
        /// <summary>
        /// Accepts XML to process
        /// </summary>
//        public Task<HttpResponseMessage> Post([FromBody]string value)
        public Task<HttpResponseMessage> Post()
        {
            String xmlName = "";
            String localFile = "";
            HttpResponseMessage theResponse;

            HttpStatusCode statusCodeBAD = HttpStatusCode.BadRequest;
            HttpStatusCode statusCodeOK = HttpStatusCode.OK;

            try
            {
                   
                MemoryStream ms = new MemoryStream(System.Web.HttpContext.Current.Request.ContentLength);
                HttpContext.Current.Request.InputStream.CopyTo(ms);


                localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/failure/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + "_non_xml.xml";
                FileStream file = new FileStream(localFile, FileMode.Create);
                ms.WriteTo(file);
                file.Close();
                ms.Close();
                ms.Dispose();
                file.Dispose();
                  
            }
            catch(Exception e)
            {
                theResponse = new HttpResponseMessage(statusCodeBAD);
                theResponse.Content = new StringContent(xmlName + "\nXML Error\n\n" + e.Message + "\n\nOriginating : " + System.Web.HttpContext.Current.Request.UserHostAddress + "\n\n" + e.StackTrace );

                return Task.FromResult(theResponse);

            }


            try
            {

                /*
                 *  get the actual request XML as a string so we can see what request it is
                 * 
                 */

                var bodyText = "";
                try
                {
                    using (var reader = new StreamReader(HttpContext.Current.Request.InputStream))
                    {
                        reader.BaseStream.Seek(0, SeekOrigin.Begin);
                        bodyText = reader.ReadToEnd(); 
                        if (bodyText.Length == 0)
                        {
                            theResponse = new HttpResponseMessage(statusCodeBAD);
                            theResponse.Content = new StringContent(xmlName + "\nXML Error 1\n\nEmpty file\n\nOriginating : " + System.Web.HttpContext.Current.Request.UserHostAddress);

                            return Task.FromResult(theResponse);

                        }

                    }
                }
                catch(Exception e)
                {
                    theResponse = new HttpResponseMessage(statusCodeBAD);
                    theResponse.Content = new StringContent(xmlName + "\nXML Error 2\n\n" + e.Message + "\n\nLength : " + System.Web.HttpContext.Current.Request.ContentLength     + "\nOriginating : " + System.Web.HttpContext.Current.Request.UserHostAddress);
                    try
                    {
                        localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/failure/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + "_non_xml.xml";
                        System.Web.HttpContext.Current.Request.SaveAs(localFile, false);
                    }
                    catch (Exception E)
                    {

                    }

                    return Task.FromResult(theResponse);

                }


                /*
                                if ( ! bodyText.Contains("<xml") && !bodyText.Contains("<?xml"))
                                {
                                    localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/failure/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + "_non_xml.xml";
                                    System.Web.HttpContext.Current.Request.SaveAs(localFile, false);

                                    theResponse = new HttpResponseMessage(statusCodeBAD);
                                    theResponse.Content = new StringContent(xmlName + "\nXML Error\n\nNot an XML file\n\nOriginating : " + System.Web.HttpContext.Current.Request.UserHostAddress);

                                    return Task.FromResult(theResponse);

                                }
                */

                int i = 0;
                int j = 0;
                // try to parse the very last end tag. This should look like </endpoint> or </ns1:endpoint>
                
                i = bodyText.LastIndexOf("<");
                j = bodyText.LastIndexOf(">");

                if (i > 0 && j > 0 && j > i + 2)
                {
                    xmlName = bodyText.Substring(i + 2, j - i - 2);
                    // strip away the special characters
                    xmlName = xmlName.Replace("</", "");
                    xmlName = xmlName.Replace(">", "");

                    xmlName = xmlName.Replace(">", "");
                    // strip away any namespace adapters
                    i = xmlName.IndexOf(":");
                    if (i > 0)
                    {
                        xmlName = xmlName.Substring(i + 1, xmlName.Length - (i + 1));
                    }
                    // xmlName should now contain just the actual request e.g. "endpoint"
                }

                // OK we have some XML , lets loose validate it by parsing it , this will 
                // trap any tag type errors
                XDocument theDocument;
                try
                {
                    theDocument = XDocument.Parse(bodyText);
              

                    xmlName = theDocument.Root.Name.LocalName;

                     
                }
                catch (Exception e)
                {
                    // Save the file in the Failure folder on the server

                    localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/failure/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";

                    using (var reader = new StreamReader(HttpContext.Current.Request.InputStream))
                    {
                        reader.BaseStream.Seek(0, SeekOrigin.Begin);
                        System.IO.File.WriteAllText(localFile, reader.ReadToEnd());
                        //                        System.Web.HttpContext.Current.Request.SaveAs(localFile, false);
                    }

                    theResponse = new HttpResponseMessage(statusCodeBAD);
                    theResponse.Content = new StringContent(xmlName + "\nXML Error 3\n\n" + e.Message + "\n\nOriginating : " + System.Web.HttpContext.Current.Request.UserHostAddress);

                    return Task.FromResult(theResponse);

                }

                if (xmlName.Length > 0)
                {
                    /*
                     * parse the name of the xml file , see if we have an XSD
                    *  look for an XSD with this xmlName in the filename to validate against 
                    * 
                    */
                    String schemaXSDFile = System.Web.HttpContext.Current.Server.MapPath("/xsd/" + xmlName + ".xsd");
                    if (File.Exists(schemaXSDFile))
                    {
                        XmlSchemaSet schemaSet = new XmlSchemaSet();

                        schemaSet.Add(null, schemaXSDFile);
                        XmlReaderSettings settings = new XmlReaderSettings();
                        settings.ValidationType = ValidationType.Schema;
                        settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;


                        var msgs = new List<string>();

                        try
                        {
                            theDocument.Validate(schemaSet, (s2, e) => msgs.Add(e.Message));
                        }
                        catch (Exception e)
                        {
                            // Save the file in the Failure folder on the server

                            localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/failure/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
                            using (var reader = new StreamReader(HttpContext.Current.Request.InputStream))
                            {
                                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                                System.IO.File.WriteAllText(localFile, reader.ReadToEnd());
                                //                        System.Web.HttpContext.Current.Request.SaveAs(localFile, false);
                            }

                            theResponse = new HttpResponseMessage(statusCodeBAD);
                            theResponse.Content = new StringContent(xmlName + "\nXML Error 4\n\n" + e.Message + "\n\n");

                            return Task.FromResult(theResponse);

                        }
                        /*
                         * If it failed (msgs.Count > 0 ) then write out the mesages back to the browser / client and return BadRequest
                         * If it worked return OK response
                         */
                        if (msgs.Count == 0)
                        {

                            // Save the file in the Success folder on the server

                            localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/success/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
                            System.IO.File.WriteAllText(localFile, bodyText);
                            theResponse = new HttpResponseMessage(statusCodeOK);
                            theResponse.Content = new StringContent(xmlName + "\n\n\nParsed OK");

                            return Task.FromResult(theResponse);
                        }
                        else
                        {
                            String resp = "";
                            /*
                             * Write each message out to the server ,typically if ot faiols there is one the first one produced anyway 
                             */

                            foreach (var m in msgs)
                            {
                                resp = resp + "\n" + m;
                            }

                            // Save the file in the Failure folder on the server

                            localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/failure/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
                            using (var reader = new StreamReader(HttpContext.Current.Request.InputStream))
                            {
                                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                                System.IO.File.WriteAllText(localFile, reader.ReadToEnd());
                                //                        System.Web.HttpContext.Current.Request.SaveAs(localFile, false);
                            }

                            theResponse = new HttpResponseMessage(statusCodeBAD);
                            theResponse.Content = new StringContent(xmlName + "\nXML Error 5\n\n" + resp + "\n\n");

                            return Task.FromResult(theResponse);
                        }

                    }
                }

                /*
                 *  To be here we have parsed the XML but cannot find a matching XSD to validate against ,so we use a generic
                 *  one purely to trap missing/mismatching end tags etc called "generic.xsd"
                 * 
                 */

                String schemaXSDGFile = System.Web.HttpContext.Current.Server.MapPath("/xsd/generic.xsd");
                if (File.Exists(schemaXSDGFile))
                {
                    XmlSchemaSet schemaSet = new XmlSchemaSet();

                    schemaSet.Add(null, schemaXSDGFile);
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.ValidationType = ValidationType.Schema;
                    settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;

                    var msgs = new List<string>();
                    try
                    {
                        theDocument.Validate(schemaSet, (s2, e) => msgs.Add(e.Message));

                        if (msgs.Count == 0)
                        {
                            /*
                             * No errors found , so return the OK response and some arbitrary text to say it worked 
                             * save the XML in the success folder
                             */
                            theResponse = new HttpResponseMessage(statusCodeOK);
                            theResponse.Content = new StringContent(xmlName + "\n\n\nParsed OK against generic xsd");

                            localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/success/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";

                            using (var reader = new StreamReader(HttpContext.Current.Request.InputStream))
                            {
                                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                                System.IO.File.WriteAllText(localFile, reader.ReadToEnd());
                                //                        System.Web.HttpContext.Current.Request.SaveAs(localFile, false);
                            }


                            return Task.FromResult(theResponse);
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    catch (Exception E)
                    {
                        String resp = "";

                        foreach (var m in msgs)
                        {
                            resp = resp + "\n" + m;
                        }

                        localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/failure/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
                        System.Web.HttpContext.Current.Request.SaveAs(localFile, false);
                        theResponse = new HttpResponseMessage(statusCodeBAD);
                        theResponse.Content = new StringContent(xmlName + "\nXML Error against generic XSD\n\n" + resp + "\n\n");

                        return Task.FromResult(theResponse);

                    }



                }

            }
            catch (Exception e)
            {
            }

            /*
             *  To be here means we don't have a specific or a generic xsd to load 
             *  No option but to class it as successful and save to success folder
             * 
             */
            localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/success/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
            System.Web.HttpContext.Current.Request.SaveAs(localFile, false);
            theResponse = new HttpResponseMessage(statusCodeOK);
            theResponse.Content = new StringContent(xmlName + "\nXML Not Parsed\n");

            return Task.FromResult(theResponse);



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
