
using log4net;
using System.Net;

namespace fahrtenbuch_service.Other
{
    public class Functions
    {
        private static ILog log = LogManager.GetLogger(typeof(Functions));
       
        public static bool IsWebPageAvailable(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = 5000;
                request.AllowAutoRedirect = false;
                request.AllowAutoRedirect = true;
                request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (Exception e)
            {
                log.Error("IsWebPageAvailable() url: " + url, e);
                return false;
            }
        }
    }
}
