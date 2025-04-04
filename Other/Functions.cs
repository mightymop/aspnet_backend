
using log4net;
using System.Net;

namespace fahrtenbuch_service.Other
{
    public class Functions
    {
        private static ILog log = LogManager.GetLogger(typeof(Functions));

        public static bool IsWebPageAvailable(string url)
        {
            return IsWebPageAvailableInternal(url, allowRetry: true);
        }

        private static bool IsWebPageAvailableInternal(string url, bool allowRetry)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = 10000;
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

                // Retry once with HTTP if HTTPS failed and retry is allowed
                if (allowRetry && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    string httpUrl = "http://" + url.Substring("https://".Length);
                    log.Info("Retrying with HTTP: " + httpUrl);
                    return IsWebPageAvailableInternal(httpUrl, allowRetry: false);
                }

                return false;
            }
        }
    }
}
