using Newtonsoft.Json.Linq;
using System.Security.AccessControl;

namespace fahrtenbuch_service.Services
{
    public class ConfigService
   {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private ConfigurationManager _cmgr;

        private string _storage_sql_Host_Write = null;
        private string _storage_sql_Database_Write = null;
        private string _storage_sql_User_Write = null;
        private string _storage_sql_Pass_Write = null;
        private string _storage_sql_Domain_Write = null;
        private bool _storage_sql_Windows_Auth_Write = false;
        private string _storage_sql_DBDateFormat_Write = null;

        private bool _debug;

        private string[] _cors_origins;

        private bool _auth_enabled = false;
        private bool _auth_validate_audience = false;
        private bool _auth_validate_sign = false;
        private string _auth_meta_url = null;
        private string _auth_client_id = null;
        private string _auth_audience = null;

        private JToken _authMetadata;


        public ConfigService(ConfigurationManager cmgr)
        {
            _cmgr = cmgr;

            _storage_sql_Host_Write = _cmgr.GetSection("storage").GetSection("database").GetSection("host").Value;
            _storage_sql_Database_Write = _cmgr.GetSection("storage").GetSection("database").GetSection("database").Value;
            _storage_sql_User_Write = _cmgr.GetSection("storage").GetSection("database").GetSection("user").Value;
            _storage_sql_Pass_Write = _cmgr.GetSection("storage").GetSection("database").GetSection("pass").Value;
            _storage_sql_Domain_Write = _cmgr.GetSection("storage").GetSection("database").GetSection("domain").Value;
            _storage_sql_Windows_Auth_Write = _cmgr.GetSection("storage").GetSection("database").GetSection("windows_auth").Get<bool>();
            _storage_sql_DBDateFormat_Write = _cmgr.GetSection("storage").GetSection("database").GetSection("dbDateFormat").Value;

            _cors_origins = _cmgr.GetSection("cors").GetSection("origins").Get<string[]>();
            _debug = _cmgr.GetSection("debug").Get<bool>();

            _auth_enabled = _cmgr.GetSection("auth").GetSection("enabled").Get<bool>();
            _auth_validate_audience = _cmgr.GetSection("auth").GetSection("validate_audience").Get<bool>();
            _auth_validate_sign = _cmgr.GetSection("auth").GetSection("validate_sign").Get<bool>();
            _auth_client_id = _cmgr.GetSection("auth").GetSection("clientid").Value; ;
            _auth_audience = _cmgr.GetSection("auth").GetSection("audience").Value; ;
            _auth_meta_url = _cmgr.GetSection("auth").GetSection("metadata").Value; ;
            _authMetadata = loadAuthMeta(_auth_meta_url);

        }

        public string getTokenUrl()
        {
            if (_authMetadata == null)
            {
                _authMetadata = loadAuthMeta(_auth_meta_url);
            }

            if (_authMetadata == null)
                return null;

            return _authMetadata["token_endpoint"].ToString();
        }

        public string getAuthorizeUrl()
        {
            if (_authMetadata==null)
            {
                _authMetadata = loadAuthMeta(_auth_meta_url);
            }

            if (_authMetadata == null)
                return null;

            return _authMetadata["authorization_endpoint"].ToString();
        }

        private JToken loadAuthMeta(string url)
        {        
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            HttpClient client = new HttpClient(handler);

            try
            {
                string json = client.GetStringAsync(url).GetAwaiter().GetResult();
                return JToken.Parse(json);
            }
            catch (Exception ex)
            {
                log.Error($"Fehler beim Laden des JSON: {ex.Message}");
                return null;
            }
        }

        public string getAuthMetadata ()
        {
            return _auth_meta_url;
        }

        public string getAuthClientID()
        {
            return _auth_client_id;
        }

        public string getAuthAudience()
        {
            return _auth_audience;
        }

        public bool isAuthValidateAudienceEnabled()
        {
            return _auth_validate_audience;
        }

        public bool isAuthValidateSignEnabled()
        {
            return _auth_validate_sign;
        }

        public bool isAuthEnabled()
        {
            return _auth_enabled;
        }

        public string[] getCorsOrigins()
        {
            return _cors_origins;
        }

        public bool isDebug()
        {
            return _debug;
        }
      
        public string getSQLConnectionstringWRITE(bool alternate)
        {
            if (isSql_Windows_Auth_Write())
            {
                if (!alternate)
                    return "Initial Catalog=" + _storage_sql_Database_Write + ";Data Source=" + _storage_sql_Host_Write + ";Integrated Security=SSPI;MultipleActiveResultSets=true;TrustServerCertificate=True";
                else
                    return "Database=" + _storage_sql_Database_Write + ";Server=" + _storage_sql_Host_Write + ";Trusted_Connection=Yes;MultipleActiveResultSets=true;TrustServerCertificate=True";

            }
            else
                return "Server=" + _storage_sql_Host_Write + ";Database=" + _storage_sql_Database_Write + ";User ID=" + _storage_sql_User_Write + ";Password=" + _storage_sql_Pass_Write + ";MultipleActiveResultSets=true;TrustServerCertificate=True";
        }

        public bool isSql_Windows_Auth_Write()
        {
            return _storage_sql_Windows_Auth_Write;
        }

        public string getSQLDateTimeFormat()
        {
            return _storage_sql_DBDateFormat_Write;
        }

        public string getSQLDomainWrite()
        {
            return _storage_sql_Domain_Write;
        }


        public string getSQLUserWrite()
        {
            return _storage_sql_User_Write;
        }


        public string getSQLPassWrite()
        {
            return _storage_sql_Pass_Write;
        }


        public string getSQLDatabaseWrite()
        {
            return _storage_sql_Database_Write;
        }

        public string get(string val)
        {
            string[] parts = val.Split(":");
            IConfigurationSection s = _cmgr.GetSection(parts[0]);
            for (int n=1;n<parts.Length;n++)
            {
               s = s.GetSection(parts[n]);
            }
            return s.Value;
        }

        public ConfigurationManager getConfigurationManager()
        {
            return _cmgr;
        }

    }
}
