using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using GrabbingParts;

namespace GrabbingParts.HttpHelpers
{
    public static class HttpHelpers
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(HttpHelpers));

        public static string GetRequestParameters(string url)
        {
            
            return url;
        }
        public static string GetText(string uri, int timeout)
        {
            Uri uriObj = new Uri(uri);

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            webRequest.Timeout = timeout;
            webRequest.Method = "GET";
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
                CopyCookies(webRequest, HttpContext.Current.Request);

            try
            {
                using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
                {
                    using (BufferedStream streamReader = new BufferedStream(webResponse.GetResponseStream(), 1024 * 1024))
                    {
                        System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
                        StringBuilder strXml = new StringBuilder();
                        byte[] buffer = new byte[1024 * 1024];
                        int charsRead;

                        while ((charsRead = streamReader.Read(buffer, 0, buffer.Length)) > 0)
                            strXml.Append(enc.GetString(buffer, 0, charsRead));

                        return strXml.ToString();
                    }
                }
            }
            catch (WebException e)
            {
                log.Error(string.Format(CultureInfo.InvariantCulture, "Exception calling {0}.", uri.ToString()), e);
                throw;
            }
        }       

        public static void Delete(Uri uri, int timeout = 180000)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            webRequest.Timeout = timeout;
            webRequest.Method = "DELETE";
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                CopyCookies(webRequest, HttpContext.Current.Request);
            }

            try
            {
                webRequest.GetResponse();
            }
            catch (WebException ex)
            {
                log.Error(string.Format(CultureInfo.InvariantCulture, "Exception calling {0} for DELETE", uri.ToString()), ex);
                throw;
            }

        }

        public static XElement PutXmlReturnXml(string uri, XElement input, int timeout = 180000)
        {
            using (XmlReader xdr = input.CreateReader())
            {
                using (XmlReader xdrRes = PutXmlReturnXml(uri, xdr, timeout))
                {
                    return XElement.Load(xdrRes);
                }
            }
        }

        public static XmlReader PutXmlReturnXml(string uri, XmlReader reader, int timeout)
        {
            Uri uriObj = new Uri(uri);
            return PutXmlReturnXml(uriObj, reader, timeout);
        }

        public static XmlReader PutXmlReturnXml(Uri uri, XmlReader reader, int timeout)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            webRequest.Timeout = timeout;
            webRequest.Method = "PUT";
            webRequest.ContentType = "text/xml";
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                CopyCookies(webRequest, HttpContext.Current.Request);
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(reader);
            using (Stream stream = webRequest.GetRequestStream())
            {
                xmlDoc.Save(stream);
            }

            try
            {
                using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
                {
                    using (StreamReader streamReader = new StreamReader(webResponse.GetResponseStream()))
                    {
                        string strXml = streamReader.ReadToEnd();

                        try
                        {
                            // load to make sure xml is valid
                            if (!string.IsNullOrEmpty(strXml))
                                XmlHelpers.XmlHelpers.LoadXml(strXml);
                        }
                        catch (Exception)
                        {
                            log.Error("Invalid xml response:\n" + strXml + "\nInput xml:\n" + xmlDoc.OuterXml);
                            if (HttpContext.Current != null && HttpContext.Current.Request != null)
                                log.Error("Cookies:\n" + HttpContext.Current.Request.ServerVariables["HTTP_COOKIE"]);
                            throw new ApplicationException("Invalid XML");
                        }

                        // dummy up a root node if nothing to return to give valid xml
                        return XmlHelpers.XmlHelpers.AggregateXml(string.IsNullOrEmpty(strXml) ? "root" : "", strXml);
                    }
                }
            }
            catch (WebException e)
            {
                log.Error(string.Format(CultureInfo.InvariantCulture, "Exception calling {0} with xml:\n{1}", uri.ToString(), xmlDoc.OuterXml), e);
                throw;
            }
        }


        /// <summary>
        /// Igore invalid certificate for HTTPS request
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }




        public static XmlReader PostXmlReturnXml(Uri uri, XmlReader reader, int timeout)
        {

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            webRequest.Timeout = timeout;
            webRequest.Method = "POST";
            webRequest.ContentType = "text/xml";
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                CopyCookies(webRequest, HttpContext.Current.Request);
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(reader);
            using (Stream stream = webRequest.GetRequestStream())
            {
                xmlDoc.Save(stream);
            }

            try
            {
                using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
                {
                    using (StreamReader streamReader = new StreamReader(webResponse.GetResponseStream()))
                    {
                        string strXml = streamReader.ReadToEnd();

                        try
                        {
                            // load to make sure xml is valid
                            if (!string.IsNullOrEmpty(strXml))
                                XmlHelpers.XmlHelpers.LoadXml(strXml);
                        }
                        catch (Exception)
                        {
                            StringBuilder cookieStr = new StringBuilder();
                            HttpCookieCollection cookies = HttpContext.Current.Request.Cookies;
                            for (int i = 0; i < cookies.Count; i++)
                            {
                                cookieStr.AppendFormat(CultureInfo.InvariantCulture, "\n{0}={1}", cookies[i].Name, cookies[i].Value);
                            }
                            log.ErrorFormat(CultureInfo.InvariantCulture, "Invalid xml response from url: {0}. \nResponse xml: {1}. \nInput xml: {2}, \nCookies: {3}", uri.ToString(), strXml, xmlDoc.OuterXml, cookieStr.ToString());
                            throw new ApplicationException("Invalid XML");
                        }

                        // dummy up a root node if nothing to return to give valid xml
                        return XmlHelpers.XmlHelpers.AggregateXml(string.IsNullOrEmpty(strXml) ? "root" : "", strXml);
                    }
                }
            }
            catch (WebException e)
            {
                log.Error(string.Format(CultureInfo.InvariantCulture, "Exception calling {0} with xml:\n{1}", uri.ToString(), xmlDoc.OuterXml), e);
                throw;
            }
        }

       

        public static byte[] PostXmlReturnBinary(Uri uri, XmlReader reader, int timeout)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Timeout = timeout;
            webRequest.Method = "POST";
            webRequest.ContentType = "text/xml";
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
                CopyCookies(webRequest, HttpContext.Current.Request);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(reader);
            using (Stream stream = webRequest.GetRequestStream())
            {
                xmlDoc.Save(stream);
            }

            using (StreamReader sreader = new StreamReader(webRequest.GetResponse().GetResponseStream()))
            {
                using (var memstream = new MemoryStream())
                {
                    sreader.BaseStream.CopyTo(memstream);
                    return memstream.ToArray();
                }
            }
        }


        /// <summary>
        /// Retrieve data from web api using post method. 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static byte[] PostFormReturnBinary(string url, string data, int timeout)
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new StringContent("=" + HttpUtility.UrlEncode(data));
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                var response = client.PostAsync(url, content).Result;

                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsByteArrayAsync().Result;
                }
                else
                {
                    log.ErrorFormat("Retrieve data from url {0} error, post data: {1}", url, data);
                    return null;
                }
            }
        }

        public static byte[] PostFormReturnBinary(string url, Dictionary<string, string> paras, int timeout)
        {
            Uri uri = new Uri(url);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Timeout = timeout;
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
                CopyCookies(webRequest, HttpContext.Current.Request);

            using (Stream stream = webRequest.GetRequestStream())
            {
                StringBuilder postData = new StringBuilder(1000);
                foreach (var para in paras)
                {
                    if (postData.Length > 0)
                        postData.Append("&");
                    postData.Append(para.Key + "=" + HttpUtility.UrlEncode(para.Value));
                }
                byte[] buffer = Encoding.UTF8.GetBytes(postData.ToString());
                stream.Write(buffer, 0, buffer.Length);
            }

            using (StreamReader sreader = new StreamReader(webRequest.GetResponse().GetResponseStream()))
            {
                using (var memstream = new MemoryStream())
                {
                    sreader.BaseStream.CopyTo(memstream);
                    return memstream.ToArray();
                }
            }
        }

        public static byte[] GetBinary(string uri, int timeout)
        {
            Uri uriObj = new Uri(uri);
            return GetBinary(uriObj, timeout);
        }

        public static byte[] GetBinary(Uri uri, int timeout)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Timeout = timeout;
            webRequest.Method = "GET";
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
                CopyCookies(webRequest, HttpContext.Current.Request);

            using (StreamReader reader = new StreamReader(webRequest.GetResponse().GetResponseStream()))
            {
                using (var memstream = new MemoryStream())
                {
                    reader.BaseStream.CopyTo(memstream);
                    return memstream.ToArray();
                }
            }
        }

        public static byte[] GetBinaryWithCredentials(string uri, NetworkCredential cred, int timeout)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Timeout = timeout;
            webRequest.Method = "GET";
            webRequest.Credentials = cred;
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
                CopyCookies(webRequest, HttpContext.Current.Request);

            using (StreamReader reader = new StreamReader(webRequest.GetResponse().GetResponseStream()))
            {
                using (var memstream = new MemoryStream())
                {
                    reader.BaseStream.CopyTo(memstream);
                    return memstream.ToArray();
                }
            }
        }

        public static byte[] PostReturnPDFBinary(string uri, string postParams, int timeout)
        {

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            webRequest.Timeout = timeout;
            webRequest.Method = "POST";
            webRequest.ContentType = "text/PDF";
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                CopyCookies(webRequest, HttpContext.Current.Request);
            }

            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(postParams.ToString());
            webRequest.ContentLength = bytes.Length;

            using (Stream stream = webRequest.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }

            try
            {
                using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
                {
                    using (StreamReader sreader = new StreamReader(webResponse.GetResponseStream()))
                    {
                        using (var memstream = new MemoryStream())
                        {
                            sreader.BaseStream.CopyTo(memstream);
                            return memstream.ToArray();
                        }
                    }
                }
            }
            catch (WebException e)
            {
                log.Error(string.Format(CultureInfo.InvariantCulture, "Exception calling {0} with xml:\n{1}", uri.ToString(), postParams), e);
                throw;
            }
        }


        public static string PostData(string url, string postParams, int timeout = 60000)
        {
            return PostData(url, postParams, true, timeout);
        }
        public static string PostData(string url, string postParams, bool isAddToken, int timeout = 60000)
        {
            string data = string.Empty;
            try
            {
                
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                req.Headers["Cookie"] = HttpContext.Current != null ? HttpContext.Current.Request.ServerVariables["HTTP_COOKIE"] : "";
                req.ContentType = "application/x-www-form-urlencoded";
                req.Method = "POST";
                req.Timeout = timeout;

                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(postParams.ToString());
                req.ContentLength = bytes.Length;

                using (Stream s = req.GetRequestStream())
                    s.Write(bytes, 0, bytes.Length);

                WebResponse resp = req.GetResponse();
                using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    data = sr.ReadToEnd();

                resp.Close();
            }
            catch (Exception e)
            {
                log.Error(string.Format("Error retrieving data using SearchCriteriaProxy.\n{0}\n{1}\n", url, postParams.ToString()), e);
                throw;
            }
            return data;
        }


        public static object GenericPostData(string Url, NetworkCredential cred)
        {
            string postData = "";
            //int ct = 0;
            object rt = "Nothing Returned!";
            string contentType = "application/x-www-form-urlencoded";


            // Create a 'HttpWebRequest' object for the specified url.
            HttpWebRequest httpWebRequest = null;
            HttpWebResponse httpWebResponse = null;
            try
            {
                if (!String.IsNullOrEmpty(Url))
                {
                    Uri uri = new Uri(Url);

                    byte[] postDataBuffer = Encoding.ASCII.GetBytes(postData);// ("data=194596");


                    httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
                    httpWebRequest.KeepAlive = true;
                    httpWebRequest.Timeout = 1000 * 60 * 15;
                    httpWebRequest.Method = "POST";
                    httpWebRequest.ProtocolVersion = new Version(1, 0);
                    httpWebRequest.ContentType = contentType;
                    httpWebRequest.ContentLength = postDataBuffer.Length;
                    httpWebRequest.Credentials = cred; //credential/ no cookies


                    //write request data 
                    using (Stream requestStream = httpWebRequest.GetRequestStream())
                    {
                        requestStream.Write(postDataBuffer, 0, postDataBuffer.Length);
                        requestStream.Close();
                    }

                    //send request to server and wait for response.
                    httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                    using (Stream responseStream = httpWebResponse.GetResponseStream())
                    {
                        using (BinaryReader br = new BinaryReader(responseStream))
                        {
                            if (br.BaseStream.CanRead)
                            {
                                //type = httpWebResponse.ContentType;
                                int l = Convert.ToInt32(httpWebResponse.ContentLength);
                                if (l < 1000)
                                    l = 2000000;

                                rt = br.ReadBytes(l + 100);
                            }
                            br.Close();
                        }
                        responseStream.Close();
                    }
                }
                else
                    rt = "No URL!";
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (httpWebResponse != null)
                    httpWebResponse.Close();
                if (httpWebRequest != null)
                    httpWebRequest.Abort();
                httpWebResponse = null;
                httpWebRequest = null;
            }

            return rt;
        }

        public static DateTime GetFileLastModifiedDate(string uri, int timeout)
        {
            DateTime lastModifiedDate = new DateTime();
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Timeout = timeout;
            webRequest.Method = "GET";

            try
            {
                using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
                {
                    lastModifiedDate = webResponse.LastModified;
                }
            }
            catch (Exception e)
            {
                log.Error(string.Format(CultureInfo.InvariantCulture, "Error retrieving {0 }file.", uri), e);
                throw;
            }
            return lastModifiedDate;
        }

        /// <summary>
        /// Get the post param from input stream.
        /// </summary>
        /// <param name="context">HttpContext</param>
        /// <returns>post param xml</returns>
        public static XDocument GetPostStreamInput(HttpContext context)
        {
            Stream stream = context.Request.InputStream;
            string str = string.Empty;
            XDocument xdoc = XDocument.Parse("<root/>");
            if (stream.Length != 0)
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    str = context.Server.UrlDecode(sr.ReadToEnd());
                    xdoc = XDocument.Parse(str);
                }
            }
            return xdoc;
        }

        public static string GetQueryValue(string queryStr, string key)
        {
            string[] qArray = queryStr.Split('&');
            for (int i = 0; i < qArray.Length; i++)
            {
                if (qArray[i] == "")
                    continue;
                string[] kv = qArray[i].Split('=');
                if (kv[0].ToLower(CultureInfo.InvariantCulture) == key.ToLower(CultureInfo.InvariantCulture))
                {
                    return kv[1];
                }
            }
            return null;
        }

        public static string RemoveQueryValue(string queryStr, string removeKey)
        {
            string[] qArray = queryStr.Split('&');
            List<string> copyArray = new List<string>();
            for (int i = 0; i < qArray.Length; i++)
            {
                if (qArray[i] == "")
                    continue;
                string[] kv = qArray[i].Split('=');
                if (kv[0].ToLower(CultureInfo.InvariantCulture) != removeKey.ToLower(CultureInfo.InvariantCulture))
                {
                    copyArray.Add(qArray[i]);
                }
            }
            return string.Join("&", copyArray);
        }

        public static void CopyCookies(HttpWebRequest webRequest, HttpRequest httpRequest)
        {
            if (httpRequest != null && webRequest != null)
            {
                webRequest.CookieContainer = new CookieContainer();
                string[] cookieKeys = new string[] { "advtsession", "advtsecure", "advtnonsecure" };

                //if (ContextDataHelpers.ContextDataHelpers.Environment == "local"
                //    || ContextDataHelpers.ContextDataHelpers.Environment == "localiis")
                //{
                //    cookieKeys = new string[] { "advtsession", "advtsecure", "advtnonsecure", "INSTID", "USERID" };
                //}

                foreach (string key in cookieKeys)
                {
                    if (httpRequest.Cookies[key] != null)
                        webRequest.CookieContainer.Add(new Cookie(key, httpRequest.Cookies[key].Value, "/", webRequest.RequestUri.Host));
                    else //if cookie is not passed, those values should be available in post request
                    {
                        if (httpRequest.Form[key] != null)
                            webRequest.CookieContainer.Add(new Cookie(key, httpRequest.Form[key], "/", webRequest.RequestUri.Host));
                    }
                }
            }
        }

        private static void AddRemoteServerCookie(string cookieFromUrl, HttpWebRequest webRequest)
        {
            if (webRequest != null)
            {
                List<Cookie> activeCookies = getRemoteServerCookie(cookieFromUrl);
                if (activeCookies != null && activeCookies.Count > 0)
                {
                    webRequest.CookieContainer = new CookieContainer();
                    foreach (Cookie activeCookie in activeCookies)
                        webRequest.CookieContainer.Add(activeCookie);
                }
            }
        }

        public static List<Cookie> getRemoteServerCookie(string url)
        {
            try
            {
                List<Cookie> result = new List<Cookie>();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                CookieContainer cookies = new CookieContainer();
                request.Method = "get";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = 0;
                request.CookieContainer = cookies;
                Uri newUri = new Uri(url);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    foreach (Cookie activeCookie in request.CookieContainer.GetCookies(newUri))
                    {
                        result.Add(activeCookie);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                log.Error(string.Format("Error in getRemoteServerCookie.\n url: {0}", url), ex);
                throw;
            }
        }
        public static void AppendToLog(string url, int timeout = 100000)
        {
            HttpContext hc = HttpContext.Current;
            Uri uriObj = new Uri(url);
            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(uriObj);
            wr.Timeout = timeout;
            if (hc != null && hc.Request != null)
            {
                //copy cookies
                CopyCookies(wr, hc.Request);
            }
            else
            {
                log.Error("Log url don't get cookies:" + uriObj.ToString());
            }
            wr.GetResponse().Close();
            if (wr != null)
            {
                wr.Abort();
                wr = null;
            }
        }

        /// <summary>
        /// Get url with raw url, fill the host name for the url if doesn't have. 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="rawUrl"></param>
        /// <param name="isSecureConnection"></param>
        /// <returns></returns>
        public static string GetUrl(string url, string host = null, bool isSecureConnection = false)
        {
            host = !string.IsNullOrEmpty(host) ? host : HttpContext.Current.Request.Url.Host;
            if (!url.StartsWith("http"))
            {
                string protocol = "http";
                if (isSecureConnection)
                {
                    protocol = "https";
                }
                url = string.Format("{0}://{1}{2}", protocol, host, url);
            }
            return url;
        }
        public static bool isSecureConnction()
        {
            return string.IsNullOrEmpty(HttpContext.Current.Request.Headers["x-arr-ssl"]);
        }

    }
}
