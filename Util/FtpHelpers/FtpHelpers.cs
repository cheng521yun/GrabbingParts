using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml.Linq;
using System.IO;
using System.Xml;
using System.Diagnostics;

namespace GrabbingParts.FtpHelpers
{
    public static class FtpHelpers
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(FtpHelpers));
        /*
        public static XmlReader GetXml(string uri, string userName, string password, int timeout)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);
            request.Timeout = timeout;
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(userName, password);

            using (FtpWebResponse webResponse = (FtpWebResponse)request.GetResponse())
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
                        log.Error("Invalid xml.\n" + strXml);
                        throw new ApplicationException("Invalid XML");
                    }

                    if (log.IsDebugEnabled)
                        log.Debug(string.Format("{0} Download Complete. \n", uri));

                    return XmlHelpers.XmlHelpers.AggregateXml(string.IsNullOrEmpty(strXml) ? "root" : "", strXml);
                }
            }

        }
         * */

        public static DateTime GetFileLastModifiedDate(string uri, string userName, string password, int timeout)
        {
            DateTime lastModifiedDate = new DateTime();
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);
            request.Timeout = timeout;
            request.Method = WebRequestMethods.Ftp.GetDateTimestamp;
            request.Credentials = new NetworkCredential(userName, password);
            try
            {
                using (FtpWebResponse webResponse = (FtpWebResponse)request.GetResponse())
                {
                    lastModifiedDate = webResponse.LastModified;
                }
            }
            catch (Exception e)
            {
                //log.Error(string.Format("Error retrieving {0 }file.", uri), e);
                throw;
            }
            return lastModifiedDate;
        }

        public static Stream GetFile(string uri, string userName, string password, int timeout)
        {
            Stream responseStream = null;
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);
                request.Timeout = timeout;
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(userName, password);

                FtpWebResponse webResponse = (FtpWebResponse)request.GetResponse();
                responseStream = webResponse.GetResponseStream();
                return responseStream;
            }
            catch (Exception e)
            {
                log.Error(string.Format("Error retrieving {0 }file.", uri), e);
            }
            return responseStream;
        }

        public static Stream GetByMethod(string uri, string userName, string password, int timeout, string method)
        {
            Stream responseStream = null;
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);
                request.Timeout = timeout;
                request.Method = method;
                request.Credentials = new NetworkCredential(userName, password);

                FtpWebResponse webResponse = (FtpWebResponse)request.GetResponse();
                responseStream = webResponse.GetResponseStream();
                return responseStream;
            }
            catch (Exception e)
            {
                log.Error(string.Format("Error retrieving {0 }file.", uri), e);
            }
            return responseStream;
        }

    }
}
