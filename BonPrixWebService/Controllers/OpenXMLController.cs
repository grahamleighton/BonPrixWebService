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
        HttpContext currentContext;
        HttpStatusCode statusCodeBAD = HttpStatusCode.BadRequest;
        HttpStatusCode statusCodeOK = HttpStatusCode.OK;
//        String pathToFailure = "../xml/failure/";
//        String pathToSuccess = "../xml/success/";
        String pathToXSD = "../xsd/";
        String calledby = "";

        public void setCaller(String caller)
        {
            calledby = caller;
        }

        public String getPathToFailure()
        {
            String tmpCalledBy = calledby;

            if (tmpCalledBy.Trim() == "")
            {
                tmpCalledBy = "open";
            }

            String foldername = "";
            foldername = "../xml/" + tmpCalledBy;
            foldername = foldername + "/failure/";

            return foldername;
        }

        public String getPathToSuccess()
        {
            String tmpCalledBy = calledby;

            if ( tmpCalledBy.Trim() == "")
            {
                tmpCalledBy = "open";
            }
            
            String foldername = "";
            foldername = "../xml/" + tmpCalledBy;
            foldername = foldername + "/success/";

            return foldername;
        }
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

                            rsp = createResponseXMLDoc("", xmlName, false, infomsgs, errs);

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

                    rsp = createResponseXMLDoc("", xmlName, false, infomsgs, errs);

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

                    rsp = createResponseXMLDoc("None", xmlName, false, infomsgs, errs);

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
                            rsp = createResponseXMLDoc(Path.GetFileNameWithoutExtension(schemaXSDFile), xmlName, false, infomsgs, errs);

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

                            rsp = createResponseXMLDoc(Path.GetFileNameWithoutExtension(schemaXSDFile), xmlName, true, infomsgs, errs);

                            theResponse.Content = new StringContent(rsp.ToString());

                            return Task.FromResult(theResponse);
                        }
                        else
                        {

                            // Save the file in the Failure folder on the server

                            localFile = System.Web.HttpContext.Current.Server.MapPath("/xml/failure/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
                                System.IO.File.WriteAllText(localFile, fdata);

                            infomsgs.Clear();
                            rsp = createResponseXMLDoc(Path.GetFileNameWithoutExtension(schemaXSDFile), xmlName, false, infomsgs, errs);
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
                            rsp = createResponseXMLDoc("Generic", xmlName, true, infomsgs, errs);

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

                        rsp = createResponseXMLDoc("Generic", xmlName, false, infomsgs, errs);

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

            rsp = createResponseXMLDoc("NONE", xmlName, true, infomsgs, errs);

            theResponse.Content = new StringContent(rsp.ToString());

            return Task.FromResult(theResponse);



        }


        private XDocument createResponseXMLDoc(string XSDName, string XMLName, bool XMLValid, List<String> infomsgs, List<String> errmsgs)
        {

            XDocument theDoc = new XDocument();
            XElement root = new XElement("ROOT");

            //            root.Add(new XElement("XSD", XSDName ));
            //            root.Add(new XElement("XMLTYPE", XMLName));
            //            root.Add(new XElement("LENGTH",  Length));
            //            root.Add(new XElement("ORIGIN", Origin ));
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


        private Task<HttpResponseMessage> returnHttpResponse(string XSDName, string XMLName, bool XMLValid, List<String> infomsgs, List<String> errmsgs)
        {
            XDocument xdoc = createResponseXMLDoc(XSDName, XMLName, XMLValid, infomsgs, errmsgs);

            HttpResponseMessage responseMessage = new HttpResponseMessage();

            responseMessage.Content = new StringContent(xdoc.ToString());
            if (XMLValid)
            {
                responseMessage.StatusCode = HttpStatusCode.OK;
            }
            else
            {
                responseMessage.StatusCode = HttpStatusCode.BadRequest;
            }

            return Task.FromResult(responseMessage);

        }



        async public Task<HttpResponseMessage> Post()
        {

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

            var infomsgs = new List<string>();
            var errs = new List<string>();
            XDocument rsp;

            infomsgs.Clear();

            errs.Clear();
            infomsgs.Add("XML Received");

            rsp = createResponseXMLDoc("", xmlName, true, infomsgs, errs);

            theResponse = new HttpResponseMessage(statusCodeOK);

            theResponse.Content = new StringContent(rsp.ToString());


            currentContext = HttpContext.Current;

            if (currentContext.Request.ContentLength > int.MaxValue)
            {
                infomsgs.Clear();
                errs.Clear();
                errs.Add("XML is too large to process");

                rsp = createResponseXMLDoc("", xmlName, false, infomsgs, errs);

                theResponse = new HttpResponseMessage(statusCodeBAD);

                theResponse.Content = new StringContent(rsp.ToString());

                return theResponse;

            }

            if (currentContext.Request.ContentLength == 0 )
            {
                infomsgs.Clear();
                errs.Clear();
                errs.Add("No Content Found");

                rsp = createResponseXMLDoc("", xmlName, false, infomsgs, errs);

                theResponse = new HttpResponseMessage(statusCodeBAD);

                theResponse.Content = new StringContent(rsp.ToString());

                return theResponse;

            }
            var bodyText = "";
            using (var reader = new StreamReader(currentContext.Request.InputStream))
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                bodyText = reader.ReadToEnd();
                bodyText = bodyText.Replace("thisXML=", "").Trim();
                if (bodyText.Length == 0)
                {
                    infomsgs.Clear();
                    errs.Clear();
                    errs.Add("No Content Found");

                    rsp = createResponseXMLDoc("", xmlName, false, infomsgs, errs);

                    theResponse = new HttpResponseMessage(statusCodeBAD);

                    theResponse.Content = new StringContent(rsp.ToString());

                    return theResponse;
                }
                else
                {

                    try
                    {
                        string localsavepath;

                        localsavepath = currentContext.Server.MapPath("../xml/All/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_all.xml";

                        System.IO.File.WriteAllText(localsavepath, bodyText);
                    }
                    catch (Exception gen)
                    {

                    }
                    if ( bodyText.IndexOf("<") == -1 || ( bodyText.IndexOf("</") == -1  && bodyText.IndexOf("/>") == -1 ) )
                    {
                        infomsgs.Clear();
                        errs.Clear();
                        errs.Add("Content is not XML");

                        rsp = createResponseXMLDoc("", xmlName, false, infomsgs, errs);

                        theResponse = new HttpResponseMessage(statusCodeBAD);

                        theResponse.Content = new StringContent(rsp.ToString());

                        return theResponse;

                    }
                }
            }

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            Task.Run(() =>
                       {
            string xcall = calledby;
               ProcessXML(bodyText);
           });

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            return await Task.FromResult(theResponse);

        }

        private Task<HttpResponseMessage> ProcessXML( string bodyText)
        {
                
            HttpContext.Current = currentContext;
            HttpResponseMessage theResponse;
            List<String> errs = new List<String>();
            List<String> infomsgs = new List<String>();
            String localFile = "";
            String xmlName = "";

            try
            {
                string localsavepath;

                localsavepath = currentContext.Server.MapPath("../xml/All/") + DateTime.Now.ToString("yyyyMMddHHmmss") + "_all.xml";

                System.IO.File.WriteAllText(localsavepath, bodyText);
            }
            catch(Exception gen)
            {
                
            }


            statusCodeBAD = statusCodeOK;
            try
            {

                // OK we have some XML , lets loose validate it by parsing it , this will 
                // trap any tag type errors
                XDocument theDocument;

                string thisAdjustmentIndicator = "";
                try
                {
                    theDocument = XDocument.Parse(bodyText);

                    foreach (XElement element in theDocument.Descendants("AdjustmentIndicator"))
                    {
                        thisAdjustmentIndicator = element.Value;
                    }

                    xmlName = theDocument.Root.Name.LocalName;
                }
                catch (Exception e)
                {
                    // Save the file in the Failure folder on the server
                    if (xmlName == "")
                    {
                        int li_start = bodyText.LastIndexOf("<");
                        int li_finish = bodyText.LastIndexOf(">");
                        if (li_start > 0 && li_finish > 0 && li_finish > li_start)
                        {
                            String tagName = bodyText.Substring(li_start + 2, li_finish - (li_start + 2));
                            int li_colon = tagName.IndexOf(":");
                            if (li_colon > 0)
                            {
                                tagName = tagName.Substring(li_colon + 1);
                                xmlName = tagName;
                                localFile = currentContext.Server.MapPath(getPathToFailure()) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
                                try
                                {
                                    System.IO.File.WriteAllText(localFile, bodyText);
                                    xmlName = tagName;
                                }
                                catch (Exception e2)
                                {
                                    localFile = currentContext.Server.MapPath(getPathToFailure()) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_failed.xml";
                                    try
                                    {
                                        System.IO.File.WriteAllText(localFile, bodyText);
                                        xmlName = "failed";
                                    }
                                    catch (Exception e3)
                                    {

                                    }

                                }
                            }
                            else
                            {
                                if (xmlName == "")
                                {
                                    xmlName = calledby;
                                }

                                localFile = currentContext.Server.MapPath(getPathToFailure()) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
                                try
                                {
                                    System.IO.File.WriteAllText(localFile, bodyText);
                                    xmlName = "failed";
                                }
                                catch (Exception e3)
                                {

                                }

                            }
                        }
                    }
                    else
                    {
                         localFile = currentContext.Server.MapPath(getPathToFailure()) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";

                         try
                         {
                            System.IO.File.WriteAllText(localFile, bodyText);
                         }
                         catch (Exception E)
                         {
                            localFile = currentContext.Server.MapPath(getPathToFailure()) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_failed.xml";
                            try
                            {
                                System.IO.File.WriteAllText(localFile, bodyText);
                                infomsgs.Clear();
                                errs.Clear();
                                errs.Add(E.Message);
                                infomsgs.Add("Failed while trying to write to : " + localFile);

                                return returnHttpResponse("", "", false, infomsgs, errs);
                            }
                            catch (Exception E2)
                            {
                                infomsgs.Clear();
                                infomsgs.Add("Error in error routine");
                                infomsgs.Add("Outer error in first message");
                                infomsgs.Add("Inner error in second message");
                                errs.Clear();
                                errs.Add(E.Message);
                                errs.Add(E2.Message);

                                return returnHttpResponse("", "", false, infomsgs, errs);
                            }
                        }
                    }
                    errs.Clear();
                    infomsgs.Clear();
                    infomsgs.Add("Saved to " + localFile);

                    return returnHttpResponse("", "", false, infomsgs, errs);
                }

                // OK so the XML parsed OK into XDocument and we should have extracted AdjustmentIndicator if it exists in the XML

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
                        schemaXSDFile = currentContext.Server.MapPath(pathToXSD + xmlName + "_Delete.xsd");
                    else
                        schemaXSDFile = currentContext.Server.MapPath(pathToXSD + xmlName + "_Add.xsd");

                    if (!File.Exists(schemaXSDFile))
                    {
                            // Default to the none appended version 
                        schemaXSDFile = currentContext.Server.MapPath(pathToXSD + xmlName + ".xsd");
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
                            if ( xmlName == "")
                            { 
                                xmlName = calledby;
                            }

                            localFile = currentContext.Server.MapPath(getPathToFailure()) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
                            try
                            {
                                System.IO.File.WriteAllText(localFile, bodyText);
                            }
                            catch (Exception E)
                            {
                                infomsgs.Clear();
                                errs.Clear();
                                localFile = currentContext.Server.MapPath(getPathToFailure()) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_failure.xml";
                                try
                                {
                                    System.IO.File.WriteAllText(localFile, bodyText);
                                }
                                catch (Exception e5)
                                {
                                    errs.Add("Error saving to " + localFile);
                                    errs.Add(e5.Message);
                                }
                                infomsgs.Add("Saved to " + localFile);

                                return returnHttpResponse("", "", false, infomsgs, errs);

                            }
                                
                            errs.Clear();
                            infomsgs.Clear();

                            errs.Add(e.Message);

                            return returnHttpResponse(Path.GetFileNameWithoutExtension(schemaXSDFile), xmlName, false, infomsgs, errs);

                        }
                        /*
                         * If it failed (msgs.Count > 0 ) then write out the mesages back to the browser / client and return BadRequest
                         * If it worked return OK response
                         */
                        if (errs.Count == 0)
                        {
                            // Save the file in the Success folder on the server
                            if (xmlName == "")
                            {
                                xmlName = calledby;
                            }

                            localFile = currentContext.Server.MapPath(getPathToSuccess()) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";

                            errs.Clear();
                            infomsgs.Clear();
                            try
                            {
                                System.IO.File.WriteAllText(localFile, bodyText);
                                infomsgs.Add("Save to " + localFile);
                            }
                            catch (Exception E)
                            {
                                errs.Add(E.Message);
                                infomsgs.Add("Error trying to save to " + localFile);
                                infomsgs.Add(localFile);

                            }
                            return returnHttpResponse("", "", false, infomsgs, errs);
                        }
                        else
                        {
                            // Save the file in the Failure folder on the server

                            if (xmlName == "")
                            {
                                xmlName = calledby;
                            }
                            localFile = currentContext.Server.MapPath(getPathToFailure()) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
                            errs.Clear();
                            infomsgs.Clear();
                            try
                            {
                                System.IO.File.WriteAllText(localFile, bodyText);
                                infomsgs.Add("Saved to " + localFile);
                            }
                            catch (Exception E)
                            {
                                errs.Add(E.Message);
                                infomsgs.Add("Error while trying to save to " + localFile);
                            }
                            return returnHttpResponse(Path.GetFileNameWithoutExtension(schemaXSDFile), xmlName, false, infomsgs, errs);
                        }

                    }
                }

                /*
                 *  To be here we have parsed the XML but cannot find a matching XSD to validate against ,so we use a generic
                 *  one purely to trap missing/mismatching end tags etc called "generic.xsd"
                 * 
                 */

                String schemaXSDGFile = currentContext.Server.MapPath(pathToXSD + "generic.xsd");
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
                        infomsgs.Clear();

                        theDocument.Validate(schemaSet, (s2, e) => errs.Add(e.Message));

                        if (errs.Count == 0)
                        {
                            /*
                             * No errors found , so return the OK response and some arbitrary text to say it worked 
                             * save the XML in the success folder
                             */
                            infomsgs.Add("XSD Success");
                            bool TaskSuccess = false;
                            localFile = currentContext.Server.MapPath(getPathToSuccess()) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
                            try
                            {
                                System.IO.File.WriteAllText(localFile, bodyText);
                                TaskSuccess = true;
                            }
                            catch (Exception E)
                            {
                                errs.Add(E.Message);
                                infomsgs.Add("Error while saving to " + localFile);
                            }

                            return returnHttpResponse ("", "", TaskSuccess, infomsgs, errs);

                        }
                    }
                    catch (Exception E)
                    {
                        localFile = currentContext.Server.MapPath(getPathToFailure()) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";
                        try
                        {
                            currentContext.Request.SaveAs(localFile, false);
                        }
                        catch (Exception E2)
                        {
                            errs.Clear();
                            errs.Add(E2.Message);
                            errs.Add(E.Message);
                            infomsgs.Clear();
                            infomsgs.Add("Error saving to " + localFile);
                            infomsgs.Add("First error message relates to outer error");

                        }
                        return returnHttpResponse("", "", false, infomsgs, errs);
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
            localFile = currentContext.Server.MapPath(getPathToSuccess()) + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + xmlName + ".xml";

            try
            {
                currentContext.Request.SaveAs(localFile, false);
            }
            catch (Exception E)
            {
                errs.Clear();
                errs.Add(E.Message);
                infomsgs.Clear();
                infomsgs.Add("Error saving to " + localFile);

                return returnHttpResponse("", "", false, infomsgs, errs);
            }
            theResponse = new HttpResponseMessage(statusCodeOK);

            infomsgs.Clear();
            errs.Clear();

            infomsgs.Add("No XSD Match to Validate XML");
            return returnHttpResponse("NONE", xmlName, true, infomsgs, errs);
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
