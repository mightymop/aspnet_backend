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
        private string _storage_sql_Windows_Auth_Write = null;
        private string _storage_sql_DBDateFormat_Write = null;

        private bool _debug;

        public ConfigService(ConfigurationManager cmgr)
        {
            _cmgr = cmgr;

            _storage_sql_Host_Write = _cmgr.GetSection("storage").GetSection("database").GetSection("host").Value;
            _storage_sql_Database_Write = _cmgr.GetSection("storage").GetSection("database").GetSection("database").Value;
            _storage_sql_User_Write = _cmgr.GetSection("storage").GetSection("database").GetSection("user").Value;
            _storage_sql_Pass_Write = _cmgr.GetSection("storage").GetSection("database").GetSection("pass").Value;
            _storage_sql_Domain_Write = _cmgr.GetSection("storage").GetSection("database").GetSection("domain").Value;
            _storage_sql_Windows_Auth_Write = _cmgr.GetSection("storage").GetSection("database").GetSection("windows_auth").Value;
            _storage_sql_DBDateFormat_Write = _cmgr.GetSection("storage").GetSection("database").GetSection("dbDateFormat").Value;

            try
            {
                _debug = _cmgr.GetSection("debug").Value.ToLower().Equals("true") ? true : false;
            }
            catch (Exception ex)
            {
                _debug = true;
            }
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
            return _storage_sql_Windows_Auth_Write.ToLower().Equals("true") || _storage_sql_Windows_Auth_Write.ToLower().Equals("1");
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
