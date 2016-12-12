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
        public Task<HttpResponseMessage>  Get()
        {
            return Post();
        }

        // GET: api/DispatchNotification2/5
        public Task<HttpResponseMessage> Get(int id)
        {
            return Post();
        }

        // POST: api/DispatchNotification2
        /// <summary>
        /// Accepts XML to process
        /// </summary>
//        public Task<HttpResponseMessage> Post([FromBody]string value)


        private Task <HttpResponseMessage> PostForm(string formdata)
        {
            string fdata = "";

            fdata = formdata.Replace("thisXML=","");
            fdata = fdata.Trim();

            /*
          * Note : the max size of content that can be posted is governed in the web.config file
          * see entry for <httpRuntime targetFramework="4.5" maxRequestLength ="24576" />
          * maxrequestlength is in kb.
          *
          * Note : IIS manager might also have a setting for the maxAllowedContentLength so check that too
          * though in this caseit worked anyway
          */


            String xmlName = "";
            String localFile = "";
            HttpResponseMessage theResponse;

            HttpStatusCode statusCodeBAD = HttpStatusCode.BadRequest;
            HttpStatusCode statusCodeOK = HttpStatusCode.OK;
            var infomsgs = new List<string>();
            var errs = new List<string>();
            XDocument rsp;

            statusCodeBAD = statusCodeOK;
            try
            {

                /*
                 *  get the actual request XML as a string so we can see what request it is
                 * 
                 */

                try
                {
                        if (fdata.Length == 0)
                        {
                            infomsgs.Clear();
                            errs.Clear();
                            errs.Add("Empty File Sent");

                            rsp = createResponse("", xmlName, false, infomsgs, errs);

                            theResponse = new HttpResponseMessage(statusCodeBAD);

                            theResponse.Content = new StringContent(rsp.ToString());

                            return Task.FromResult(theResponse);

                        }
                }
                catch (Exception e)
                {
                    theResponse = new HttpResponseMessage(statusCodeBAD);
                    errs.Clear();
                    infomsgs.Clear();

                    errs.Add(e.Message);

                    rsp = createResponse("", xmlName, false, infomsgs, errs);

                    theResponse.Content = new StringContent(rsp.ToString());

                    try
                    {
                        localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/failure/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + "_non_xml.xml";
                        System.IO.File.WriteAllText(localFile, fdata);
                        
                    }
                    catch (Exception E)
                    {

                    }

                    return Task.FromResult(theResponse);

                }


                // OK we have some XML , lets loose validate it by parsing it , this will 
                // trap any tag type errors
                XDocument theDocument;

                string thisAdjustmentIndicator = "";
                try
                {
                    theDocument = XDocument.Parse(fdata);


                    foreach (XElement element in theDocument.Descendants("AdjustmentIndicator"))
                    {
                        thisAdjustmentIndicator = element.Value;
                    }

                    xmlName = theDocument.Root.Name.LocalName;
                }
                catch (Exception e)
                {
                    // Save the file in the Failure folder on the server

                    localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/failure/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";

                    System.IO.File.WriteAllText(localFile, fdata);

                    theResponse = new HttpResponseMessage(statusCodeBAD);

                    errs.Clear();
                    infomsgs.Clear();

                    errs.Add(e.Message);
                    infomsgs.Add("General Parse Error");

                    rsp = createResponse("None", xmlName, false, infomsgs, errs);

                    theResponse.Content = new StringContent(rsp.ToString());

                    return Task.FromResult(theResponse);

                }

                if (String.IsNullOrEmpty(thisAdjustmentIndicator))
                {
                    thisAdjustmentIndicator = "C";
                }

                if (xmlName.Length > 0)
                {
                    /*
                     * parse the name of the xml file , see if we have an XSD
                    *  look for an XSD with this xmlName in the filename to validate against 
                    * 
                    */
                    String schemaXSDFile = "";
                    if (thisAdjustmentIndicator == "D")
                        schemaXSDFile = System.Web.HttpContext.Current.Server.MapPath("/xsd/" + xmlName + "_Delete.xsd");
                    else
                        schemaXSDFile = System.Web.HttpContext.Current.Server.MapPath("/xsd/" + xmlName + "_Add.xsd");

                    if (!File.Exists(schemaXSDFile))
                    {
                        // Default to the none appended version 
                        schemaXSDFile = System.Web.HttpContext.Current.Server.MapPath("/xsd/" + xmlName + ".xsd");
                    }

                    if (File.Exists(schemaXSDFile))
                    {
                        XmlSchemaSet schemaSet = new XmlSchemaSet();

                        schemaSet.Add(null, schemaXSDFile);
                        XmlReaderSettings settings = new XmlReaderSettings();
                        settings.ValidationType = ValidationType.Schema;
                        settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;

                        try
                        {
                            theDocument.Validate(schemaSet, (s2, e) => errs.Add(e.Message));
                        }
                        catch (Exception e)
                        {
                            // Save the file in the Failure folder on the server

                            localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/failure/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
                                System.IO.File.WriteAllText(localFile, fdata );

                            theResponse = new HttpResponseMessage(statusCodeBAD);

                            errs.Clear();
                            infomsgs.Clear();

                            errs.Add(e.Message);
                            rsp = createResponse(Path.GetFileNameWithoutExtension(schemaXSDFile), xmlName, false, infomsgs, errs);

                            theResponse.Content = new StringContent(rsp.ToString());

                            return Task.FromResult(theResponse);

                        }
                        /*
                         * If it failed (msgs.Count > 0 ) then write out the mesages back to the browser / client and return BadRequest
                         * If it worked return OK response
                         */
                        if (errs.Count == 0)
                        {

                            // Save the file in the Success folder on the server

                            localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/success/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
                            System.IO.File.WriteAllText(localFile, fdata );
                            theResponse = new HttpResponseMessage(statusCodeOK);

                            infomsgs.Clear();

                            rsp = createResponse(Path.GetFileNameWithoutExtension(schemaXSDFile), xmlName, true, infomsgs, errs);

                            theResponse.Content = new StringContent(rsp.ToString());

                            return Task.FromResult(theResponse);
                        }
                        else
                        {

                            // Save the file in the Failure folder on the server

                            localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/failure/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
                                System.IO.File.WriteAllText(localFile, fdata);

                            infomsgs.Clear();
                            rsp = createResponse(Path.GetFileNameWithoutExtension(schemaXSDFile), xmlName, false, infomsgs, errs);
                            theResponse = new HttpResponseMessage(statusCodeBAD);
                            theResponse.Content = new StringContent(rsp.ToString());


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

                    try
                    {
                        errs.Clear();
                        theDocument.Validate(schemaSet, (s2, e) => errs.Add(e.Message));

                        if (errs.Count == 0)
                        {
                            /*
                             * No errors found , so return the OK response and some arbitrary text to say it worked 
                             * save the XML in the success folder
                             */
                            theResponse = new HttpResponseMessage(statusCodeOK);
                            infomsgs.Clear();
                            rsp = createResponse("Generic", xmlName, true, infomsgs, errs);

                            theResponse.Content = new StringContent(rsp.ToString());

                            localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/success/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";

                            System.IO.File.WriteAllText(localFile, fdata);


                            return Task.FromResult(theResponse);
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    catch (Exception E)
                    {
                        localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/failure/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
                        System.IO.File.WriteAllText(localFile, fdata);
                        theResponse = new HttpResponseMessage(statusCodeBAD);

                        infomsgs.Clear();

                        rsp = createResponse("Generic", xmlName, false, infomsgs, errs);

                        theResponse.Content = new StringContent(rsp.ToString());

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

            infomsgs.Clear();
            errs.Clear();

            infomsgs.Add("No XSD Match to Validate XML");

            rsp = createResponse("NONE", xmlName, true, infomsgs, errs);

            theResponse.Content = new StringContent(rsp.ToString());

            return Task.FromResult(theResponse);



        }
        public Task<HttpResponseMessage> Post()
        {

            /*
             * Note : the max size of content that can be posted is governed in the web.config file
             * see entry for <httpRuntime targetFramework="4.5" maxRequestLength ="24576" />
             * maxrequestlength is in kb.
             *
             * Note : IIS manager might also have a setting for the maxAllowedContentLength so check that too
             * though in this caseit worked anyway
             */





            String pathToFailure = "../xml/failure/";
            String pathToSuccess = "../xml/success/";
            String pathToXSD = "../xsd/";


            String xmlName = "";
            String localFile = "";
            HttpResponseMessage theResponse;

            HttpStatusCode statusCodeBAD = HttpStatusCode.BadRequest;
            HttpStatusCode statusCodeOK = HttpStatusCode.OK;
            var infomsgs = new List<string>();
            var errs = new List<string>();
            XDocument rsp;
            /*
                        XDocument test = createResponse("TestXSD", "MyXMLName", true, infomsgs, errs);
                        theResponse = new HttpResponseMessage(statusCodeOK);
                        theResponse.Content = new StringContent(test.ToString());


                        return Task.FromResult(theResponse);
            */

            if (HttpContext.Current.Request.ContentLength > int.MaxValue)
            {
                infomsgs.Clear();
                errs.Clear();
                errs.Add("XML is too large to process");

                rsp = createResponse("", xmlName, false, infomsgs, errs);

                theResponse = new HttpResponseMessage(statusCodeBAD);

                theResponse.Content = new StringContent(rsp.ToString());

                return Task.FromResult(theResponse);

            }

            statusCodeBAD = statusCodeOK;
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
                        bodyText = bodyText.Replace("thisXML=", "").Trim() ;

                        if (bodyText.Length == 0)
                        {
                            infomsgs.Clear();
                            errs.Clear();
                            errs.Add("Empty File Sent");

                            rsp = createResponse("",xmlName , false, infomsgs, errs);

                            theResponse = new HttpResponseMessage(statusCodeBAD);

                            theResponse.Content = new StringContent(rsp.ToString());

                            return Task.FromResult(theResponse);

                        }

                    }
                }
                catch(Exception e)
                {
                    theResponse = new HttpResponseMessage(statusCodeBAD);
                    errs.Clear();
                    infomsgs.Clear();

                    errs.Add(e.Message);

                    rsp = createResponse("", xmlName, false, infomsgs, errs);

                    theResponse.Content = new StringContent(rsp.ToString());

                    try
                    {
                        localFile = System.Web.HttpContext.Current.Server.MapPath(pathToFailure) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + "_non_xml.xml";
                        System.Web.HttpContext.Current.Request.SaveAs(localFile, false);
                    }
                    catch (Exception E)
                    {
                        errs.Clear();
                        errs.Add(E.Message);
                        infomsgs.Clear();
                        infomsgs.Add(localFile);
                        infomsgs.Add("Position 1");
                        XDocument esp = createResponse("", "", false, infomsgs, errs);
                        HttpResponseMessage eresp = new HttpResponseMessage(HttpStatusCode.BadRequest);
                        eresp.Content = new StringContent(esp.ToString());

                        return Task.FromResult(eresp);


                    }

                    return Task.FromResult(theResponse);

                }


                // OK we have some XML , lets loose validate it by parsing it , this will 
                // trap any tag type errors
                XDocument theDocument;

                string thisAdjustmentIndicator = "";
                try
                {
                    theDocument = XDocument.Parse(bodyText);


                    foreach ( XElement element in theDocument.Descendants("AdjustmentIndicator") )
                    {
                        thisAdjustmentIndicator  = element.Value;
                    }
                    
                    xmlName = theDocument.Root.Name.LocalName;
                }
                catch (Exception e)
                {
                    // Save the file in the Failure folder on the server

                    localFile = System.Web.HttpContext.Current.Server.MapPath(pathToFailure) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";

                    using (var reader = new StreamReader(HttpContext.Current.Request.InputStream))
                    {
                        reader.BaseStream.Seek(0, SeekOrigin.Begin);
                        try
                        {
                            System.IO.File.WriteAllText(localFile, bodyText);
                        }
                        catch (Exception E)
                        {
                            errs.Clear();
                            errs.Add(E.Message);
                            infomsgs.Clear();
                            infomsgs.Add(localFile);
                            infomsgs.Add("Position 2");
                            XDocument esp = createResponse("", "", false, infomsgs, errs);
                            HttpResponseMessage eresp = new HttpResponseMessage(HttpStatusCode.BadRequest);
                            eresp.Content = new StringContent(esp.ToString());

                            return Task.FromResult(eresp);

                        }
                    }

                    theResponse = new HttpResponseMessage(statusCodeBAD);

                    errs.Clear();
                    infomsgs.Clear();

                    errs.Add(e.Message);
                    infomsgs.Add("General Parse Error");

                    rsp = createResponse("None", xmlName ,false, infomsgs, errs);
                 
                    theResponse.Content = new StringContent(rsp.ToString());

                    return Task.FromResult(theResponse);

                }

                if ( String.IsNullOrEmpty(thisAdjustmentIndicator) )
                {
                    thisAdjustmentIndicator = "C";
                }

                if (xmlName.Length > 0)
                {
                    /*
                     * parse the name of the xml file , see if we have an XSD
                    *  look for an XSD with this xmlName in the filename to validate against 
                    * 
                    */
                    String schemaXSDFile = "";
                    if ( thisAdjustmentIndicator == "D" )
                        schemaXSDFile = System.Web.HttpContext.Current.Server.MapPath(pathToXSD + xmlName + "_Delete.xsd");
                    else
                        schemaXSDFile = System.Web.HttpContext.Current.Server.MapPath(pathToXSD + xmlName + "_Add.xsd");

                    if (! File.Exists(schemaXSDFile))
                    {
                        // Default to the none appended version 
                        schemaXSDFile = System.Web.HttpContext.Current.Server.MapPath(pathToXSD + xmlName + ".xsd");
                    }

                    if (File.Exists(schemaXSDFile))
                    {
                        XmlSchemaSet schemaSet = new XmlSchemaSet();

                        schemaSet.Add(null, schemaXSDFile);
                        XmlReaderSettings settings = new XmlReaderSettings();
                        settings.ValidationType = ValidationType.Schema;
                        settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;

                        try
                        {
                            theDocument.Validate(schemaSet, (s2, e) => errs.Add(e.Message));
                        }
                        catch (Exception e)
                        {
                            // Save the file in the Failure folder on the server

                            localFile = System.Web.HttpContext.Current.Server.MapPath(pathToFailure) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
                            using (var reader = new StreamReader(HttpContext.Current.Request.InputStream))
                            {
                                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                                try
                                {
                                    System.IO.File.WriteAllText(localFile, bodyText);
                                }
                                catch (Exception E)
                                {

                                    errs.Clear();
                                    errs.Add(E.Message);
                                    infomsgs.Clear();
                                    infomsgs.Add(localFile);
                                    infomsgs.Add("Position 3");
                                    XDocument esp = createResponse("", "", false, infomsgs, errs);
                                    HttpResponseMessage eresp = new HttpResponseMessage(HttpStatusCode.BadRequest);
                                    eresp.Content = new StringContent(esp.ToString());

                                    return Task.FromResult(eresp);
                                }

                            }

                            theResponse = new HttpResponseMessage(statusCodeBAD);

                            errs.Clear();
                            infomsgs.Clear();

                            errs.Add(e.Message);
                            rsp = createResponse(Path.GetFileNameWithoutExtension(schemaXSDFile), xmlName , false, infomsgs, errs);

                            theResponse.Content = new StringContent(rsp.ToString());

                            return Task.FromResult(theResponse);

                        }
                        /*
                         * If it failed (msgs.Count > 0 ) then write out the mesages back to the browser / client and return BadRequest
                         * If it worked return OK response
                         */
                        if (errs.Count == 0)
                        {

                            // Save the file in the Success folder on the server

                            localFile = System.Web.HttpContext.Current.Server.MapPath(pathToSuccess) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";

                            try
                            {
                                System.IO.File.WriteAllText(localFile, bodyText);
                            }
                            catch (Exception E)
                            {

                                errs.Clear();
                                errs.Add(E.Message);
                                infomsgs.Clear();
                                infomsgs.Add(localFile);
                                infomsgs.Add("Position 4");
                                XDocument esp = createResponse("", "", false, infomsgs, errs);
                                HttpResponseMessage eresp = new HttpResponseMessage(HttpStatusCode.BadRequest);
                                eresp.Content = new StringContent(esp.ToString());

                                return Task.FromResult(eresp);
                            }

                            theResponse = new HttpResponseMessage(statusCodeOK);

                            infomsgs.Clear();

                            rsp = createResponse(Path.GetFileNameWithoutExtension(schemaXSDFile), xmlName , true, infomsgs, errs);

                            theResponse.Content = new StringContent(rsp.ToString()); 

                            return Task.FromResult(theResponse);
                        }
                        else
                        {

                            // Save the file in the Failure folder on the server

                            localFile = System.Web.HttpContext.Current.Server.MapPath(pathToFailure) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
                            using (var reader = new StreamReader(HttpContext.Current.Request.InputStream))
                            {
                                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                                try
                                {
                                    System.IO.File.WriteAllText(localFile, bodyText);
                                }
                                catch (Exception E)
                                {
                                    errs.Clear();
                                    errs.Add(E.Message);
                                    infomsgs.Clear();
                                    infomsgs.Add(localFile);
                                    infomsgs.Add("Position 5");
                                    XDocument esp = createResponse("", "", false, infomsgs, errs);
                                    HttpResponseMessage eresp = new HttpResponseMessage(HttpStatusCode.BadRequest);
                                    eresp.Content = new StringContent(esp.ToString());

                                    return Task.FromResult(eresp);
                                }

                            }

                            infomsgs.Clear();
                            rsp = createResponse(Path.GetFileNameWithoutExtension(schemaXSDFile), xmlName, false, infomsgs, errs);
                            theResponse = new HttpResponseMessage(statusCodeBAD);
                            theResponse.Content = new StringContent(rsp.ToString() );


                            return Task.FromResult(theResponse);
                        }

                    }
                }

                /*
                 *  To be here we have parsed the XML but cannot find a matching XSD to validate against ,so we use a generic
                 *  one purely to trap missing/mismatching end tags etc called "generic.xsd"
                 * 
                 */

                String schemaXSDGFile = System.Web.HttpContext.Current.Server.MapPath(pathToXSD + "generic.xsd");
                if (File.Exists(schemaXSDGFile))
                {
                    XmlSchemaSet schemaSet = new XmlSchemaSet();

                    schemaSet.Add(null, schemaXSDGFile);
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.ValidationType = ValidationType.Schema;
                    settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;

                    try
                    {
                        errs.Clear();
                        theDocument.Validate(schemaSet, (s2, e) => errs.Add(e.Message));

                        if (errs.Count == 0)
                        {
                            /*
                             * No errors found , so return the OK response and some arbitrary text to say it worked 
                             * save the XML in the success folder
                             */
                            theResponse = new HttpResponseMessage(statusCodeOK);
                            infomsgs.Clear();
                            rsp = createResponse("Generic", xmlName , true, infomsgs, errs);

                            theResponse.Content = new StringContent(rsp.ToString());

                            localFile = System.Web.HttpContext.Current.Server.MapPath(pathToSuccess) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";

                            //                            using (var reader = new StreamReader(HttpContext.Current.Request.InputStream))
                            //                            {
                            //                                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                            //                                var httpdata = reader.ReadToEnd();


                            try
                            {
                                System.IO.File.WriteAllText(localFile, bodyText);
                            }
                            catch (Exception E)
                            {

                                errs.Clear();
                                errs.Add(E.Message);
                                infomsgs.Clear();
                                infomsgs.Add(localFile);
                                infomsgs.Add("Position 6");
                                XDocument esp = createResponse("", "", false, infomsgs, errs);
                                HttpResponseMessage eresp = new HttpResponseMessage(HttpStatusCode.BadRequest);
                                eresp.Content = new StringContent(esp.ToString());

                                return Task.FromResult(eresp);
                            }
                            //                              System.Web.HttpContext.Current.Request.SaveAs(localFile, false);
                            //                        System.Web.HttpContext.Current.Request.SaveAs(localFile, false);
                            //                            }


                            return Task.FromResult(theResponse);
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    catch (Exception E)
                    {
                        localFile = System.Web.HttpContext.Current.Server.MapPath(pathToFailure) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";

                        try
                        {
                            System.Web.HttpContext.Current.Request.SaveAs(localFile, false);
                        }
                        catch (Exception E2)
                        {
                            errs.Clear();
                            errs.Add(E2.Message);
                            infomsgs.Clear();
                            infomsgs.Add(localFile);
                            infomsgs.Add("Position 7");
                            XDocument esp = createResponse("", "", false, infomsgs, errs);
                            HttpResponseMessage eresp = new HttpResponseMessage(HttpStatusCode.BadRequest);
                            eresp.Content = new StringContent(esp.ToString());

                            return Task.FromResult(eresp);
                        }
                        theResponse = new HttpResponseMessage(statusCodeBAD);

                        infomsgs.Clear();

                        rsp = createResponse("Generic", xmlName , false, infomsgs, errs);

                        theResponse.Content = new StringContent(rsp.ToString());

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
            localFile = System.Web.HttpContext.Current.Server.MapPath(pathToSuccess) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";

            try
            {
                System.Web.HttpContext.Current.Request.SaveAs(localFile, false);
            }
            catch (Exception E)
            {
                errs.Clear();
                errs.Add(E.Message);
                infomsgs.Clear();
                infomsgs.Add(localFile);
                infomsgs.Add("Position 8");
                XDocument esp = createResponse("", "", false, infomsgs, errs);
                HttpResponseMessage eresp = new HttpResponseMessage(HttpStatusCode.BadRequest);
                eresp.Content = new StringContent(esp.ToString());

                return Task.FromResult(eresp);

            }
            theResponse = new HttpResponseMessage(statusCodeOK);

            infomsgs.Clear();
            errs.Clear();

            infomsgs.Add("No XSD Match to Validate XML");

            rsp = createResponse("NONE", xmlName , true,  infomsgs, errs);

            theResponse.Content = new StringContent(rsp.ToString() );

            return Task.FromResult(theResponse);



        }

        private XDocument createResponse(string XSDName , string XMLName,  bool XMLValid , List<String> infomsgs , List<String> errmsgs)
        {

            XDocument theDoc = new XDocument();
            XElement root = new XElement("ROOT");

            root.Add(new XElement("XSD", XSDName ));
            root.Add(new XElement("XMLTYPE", XMLName));
            root.Add(new XElement("LENGTH", System.Web.HttpContext.Current.Request.ContentLength));
            root.Add(new XElement("ORIGIN", System.Web.HttpContext.Current.Request.UserHostAddress));
            root.Add(new XElement("TIME", DateTime.Now.ToUniversalTime()  ));

            theDoc.Add(root);
            if ( XMLValid )
                root.Add(new XElement("RESULT", "OK"));
            else
                root.Add(new XElement("RESULT", "FAIL"));

            if (infomsgs.Count > 0)
            {
                XElement info_root = new XElement("INFORMATION");

                foreach (var m in infomsgs)
                {
                    info_root.Add(new XElement ("INFO", m.ToString()) );
                }
                root.Add(info_root);
            }

            if (errmsgs.Count > 0)
            {
                XElement err_root = new XElement("ERRORS");

                foreach (var m in errmsgs)
                {
                    err_root.Add(new XElement( "ERROR", m.ToString()) );
                }
                root.Add(err_root);
            }


            return theDoc;

        }

        // PUT: api/DispatchNotification2/5
        public Task<HttpResponseMessage> Put(int id, [FromBody]string value)
        {
            return Post();

        }

        // DELETE: api/DispatchNotification2/5
        public Task<HttpResponseMessage>  Delete(int id)
        {
            return Post();
        }
    }
}
