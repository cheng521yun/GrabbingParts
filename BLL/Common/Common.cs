using System;
using HtmlAgilityPack;

namespace GrabbingParts.BLL.Common
{
    public static class Common
    {
        public static HtmlDocument RetryRequest(string url)
        {
            try
            {
                int timeout = 100000;
                HtmlWeb htmlWeb = new HtmlWeb();
                return Retry.Do(() => htmlWeb.Load(url), TimeSpan.FromSeconds(timeout / 1000));
            }
            catch (Exception ex)
            {
                //Todo: add log using log4net
                return null;
            }
        }
    }
}
