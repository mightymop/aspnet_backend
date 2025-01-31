using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using log4net;
using Microsoft.Win32.SafeHandles;
using SimpleImpersonation;

namespace fahrtenbuch_service.Services
{

    public class DatabaseService
    {

        private ConfigService _config = null;

        private ILog log = LogManager.GetLogger(typeof(DatabaseService));

        public DatabaseService(ConfigService config)
        {
            this._config = config;
        }

        private SqlConnection openConnection()
        {
            SqlConnection connection;

            try
            {
                string cstring = _config.getSQLConnectionstringWRITE(false);

                connection = new SqlConnection(cstring);
                connection.Open();
            }
            catch (Exception e)
            {
                log.Error("FEHLER BEIM SQL VERBINDUNGSAUFBAU: " + e.Message, e);

                string cstring = _config.getSQLConnectionstringWRITE(true);

                log.Error("SQL-CONNECTION-STRING: " + cstring);
                connection = new SqlConnection(cstring);
                connection.Open();
            }

            return connection;
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out IntPtr phToken);

        private SafeAccessTokenHandle getUserHandle(bool read)
        {
            var credentials = new UserCredentials(_config.getSQLDomainWrite(),
                                                  _config.getSQLUserWrite(),
                                                  _config.getSQLPassWrite());
            return credentials.LogonUser(LogonType.Interactive);  // or another LogonType
        }

        private bool isEmpty(string data)
        {
            return data == null || data.Trim().Length == 0 || data.Trim().ToLower().Equals("null");
        }

        private int executeInsertUpdateQuery(SqlCommand cmd, int current, int max)
        {
            int result;

            if (current > max)
            {
                throw new Exception("Execution Timeout Expired.");
            }

            try
            {
                cmd.CommandTimeout = 30 * current;
                result = cmd.ExecuteNonQuery();
            }
            catch (SqlException e) when (e.Message.StartsWith("Execution Timeout Expired."))
            {
                result = executeInsertUpdateQuery(cmd, current + 1, max);
            }

            return result;
        }

        /*******************************tab_data*********************************************************/

        public object readData(IDataReader reader)
        {
            int colIndexID = reader.GetOrdinal("id");
            int colIndexData = reader.GetOrdinal("data");

            string id = !reader.IsDBNull(colIndexID) ? reader["id"].ToString() : "";
            string data = !reader.IsDBNull(colIndexData) ? reader["data"].ToString() : "";
          
            return new
            {
                id = id,
                data = data
            };
        }

        public object getData(string id, out string error)
        {

            try
            {
                var fkt = () =>
                {
                    using (SqlConnection connection = openConnection())
                    {
                        string sql = "SELECT tab_data.id, tab_data.data FROM " + this._config.getSQLDatabaseWrite() + ".dbo.tab_data WHERE id=@id";

                        log.Info(sql);

                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            log.Debug("ADD PARAM ID: " + id);
                            cmd.Parameters.Add("@id", SqlDbType.VarChar, id.Length).Value = id;

                            object result = null;
                            using (IDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    result = readData(reader);
                                }
                            }
                            connection.Close();
                            return result;
                        }
                    }
                };

                error = null;
                if (_config.isSql_Windows_Auth_Write())
                {
                    return WindowsIdentity.RunImpersonated(getUserHandle(false), fkt);
                }
                else
                {
                    return fkt();
                }
            }
            catch (Exception e)
            {
                string method = new StackTrace(new StackFrame(1)).GetFrame(0)!.GetMethod()!.Name;
                string classname = this.GetType().BaseType!.Name;
                log.Error(classname + "." + method + ": " + e.Message, e);
                error = e.Message;
                return null;
            }
        }

        public object[] getAllData(out string error)
        {

            try
            {
                var fkt = () =>
                {
                    using (SqlConnection connection = openConnection())
                    {
                        string sql = "SELECT tab_data.id, tab_data.data FROM " + this._config.getSQLDatabaseWrite() + ".dbo.tab_data";

                        log.Info(sql);

                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                           
                            ArrayList result = new ArrayList();
                            using (IDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    result.Add(readData(reader));
                                }
                            }
                            connection.Close();
                            return result.ToArray();
                        }
                    }
                };

                error = null;
                if (_config.isSql_Windows_Auth_Write())
                {
                    return WindowsIdentity.RunImpersonated(getUserHandle(false), fkt);
                }
                else
                {
                    return fkt();
                }
            }
            catch (Exception e)
            {
                string method = new StackTrace(new StackFrame(1)).GetFrame(0)!.GetMethod()!.Name;
                string classname = this.GetType().BaseType!.Name;
                log.Error(classname + "." + method + ": " + e.Message, e);
                error = e.Message;
                return null;
            }
        }

        public string insertOrUpdateData(string id, string data, out string error)
        {

            try
            {
                var fkt = () =>
                {
                    using (SqlConnection connection = openConnection())
                    {
                        if (id==null)
                        {
                            id = Guid.NewGuid().ToString();
                        }


                        string sql = "BEGIN TRAN" +
                                   " SET DATEFORMAT ymd;" +
                                   " if exists(SELECT * FROM " + this._config.getSQLDatabaseWrite() + ".dbo.tab_data WHERE id=@uuid)" +
                                   " BEGIN" +
                                         " UPDATE " + this._config.getSQLDatabaseWrite() + ".dbo.tab_data " +
                                           " SET " +
                                           " data=@data " +                                     
                                           " WHERE id=@uuid" +
                                   " END" +
                                   " else" +
                                   " BEGIN" +
                                     " INSERT INTO " + this._config.getSQLDatabaseWrite() + ".dbo.tab_data (id,data)" +
                                       " VALUES (@uuid,@data)" +
                                   " END " +
                              " COMMIT TRAN";

                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.Add("@uuid", SqlDbType.VarChar, id.Length).Value = id;
                                                    

                            if (!isEmpty(data))
                            {
                                cmd.Parameters.Add("@data", SqlDbType.VarChar, data.Length).Value = data;
                            }
                            else
                            {
                                cmd.Parameters.Add("@data", SqlDbType.VarChar, 0).Value = DBNull.Value;
                            }
                           

                            bool result = (executeInsertUpdateQuery(cmd, 1, 3) == 1 ? true : false);
                            connection.Close();
                            if (result)
                            {
                                return id;
                            }
                            else
                            {
                                return null;
                            }
                        }

                    }
                };

                error = null;
                string result;
                if (_config.isSql_Windows_Auth_Write())
                {
                    result = WindowsIdentity.RunImpersonated(getUserHandle(false), fkt);
                    
                }
                else
                {
                    result = fkt();
                }

                if (result == null)
                {
                    error = "Fehler beim Insert!";
                }
                return result;
            }
            catch (Exception e)
            {
                string method = new StackTrace(new StackFrame(1)).GetFrame(0)!.GetMethod()!.Name;
                string classname = this.GetType().BaseType!.Name;
                log.Error(classname + "." + method + ": " + e.Message + " DATA: " + data, e);
                error = e.Message;
                return null;
            }
        }

        public bool deleteData(string id, out string error)
        {
            try
            {
                var fkt = () =>
                {
                    using (SqlConnection connection = openConnection())
                    {
                        string sql = " BEGIN TRAN" +
                                   " DELETE FROM " + this._config.getSQLDatabaseWrite() + ".dbo.tab_data WHERE id=@uuid;" +
                                  " COMMIT TRAN";

                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.Add("@uuid", SqlDbType.VarChar, id.Length).Value = id;

                            bool result = (executeInsertUpdateQuery(cmd, 1, 3) >= 1 ? true : false);
                            connection.Close();
                            return result;
                        }
                    }
                };

                error = null;
                if (_config.isSql_Windows_Auth_Write())
                {
                    return WindowsIdentity.RunImpersonated(getUserHandle(false), fkt);
                }
                else
                {
                    return fkt();
                }
            }
            catch (Exception e)
            {
                string method = new StackTrace(new StackFrame(1)).GetFrame(0)!.GetMethod()!.Name;
                string classname = this.GetType().BaseType!.Name;
                log.Error(classname + "." + method + ": " + e.Message, e);
                error = e.Message;
                return false;
            }
        }


      
    }
}
