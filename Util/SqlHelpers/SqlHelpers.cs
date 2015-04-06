using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Transactions;
using System.Xml;
using System.Xml.XPath;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Data;
using Microsoft.Practices.EnterpriseLibrary.Data.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;
using System.Xml.Linq;


namespace GrabbingParts.SqlHelpers
{
    public static class SqlHelpers
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(SqlHelpers));
        private static log4net.ILog deplog = log4net.LogManager.GetLogger("DependencyLogger");
        static public int TimeoutSeconds { get { return 60; } }
        private const int DEADLOCK = 1205;  // 1205 deadlock in SqlException.Number

        // get default database
        static private Database GetDatabase()
        {
            return GetDatabase(null);
        }

        // get specific database
        static private Database GetDatabase(string databaseName)
        {
            string db = !string.IsNullOrEmpty(databaseName) ? databaseName : DatabaseSettings.GetDatabaseSettings(new SystemConfigurationSource()).DefaultDatabase;
            return new SqlDatabase(GetDatabaseConnectionString(db));
        }  

        static public string GetDatabaseConnectionString(string databaseName)
        {
            return ConfigurationManager.ConnectionStrings[databaseName].ConnectionString;
        }

        static public DbCommand GetStoredProcCommand(Database db, string storedProcName, params object[] paramValues)
        {
            DbCommand cmd = (paramValues == null) ? db.GetStoredProcCommand(storedProcName) : db.GetStoredProcCommand(storedProcName, paramValues);
            cmd.CommandTimeout = TimeoutSeconds;
            
            return cmd;
        }

        static public XmlReader RunSPReturnXmlReader(string storedProcName, params object[] paramValues)
        {
            return RunSPReturnXmlReaderDatabase(null, storedProcName, paramValues);
        }

        static public XmlReader RunSPReturnXmlReaderDatabase(string databaseName, string storedProcName, params object[] paramValues)
        {
            XmlReader result = null;
            Database db = GetDatabase(databaseName);
            using (DbCommand cmd = GetStoredProcCommand(db, storedProcName, paramValues))
            {
                try
                {
                    using (IDataReader reader = db.ExecuteReader(cmd))
                    {
                        result = SqlHelpers.GetXmlFromDataReader(reader);
                    }
                }
                catch (SqlException sqlEx)
                {
                    if (sqlEx.Number == DEADLOCK)
                        log.Warn(string.Format(CultureInfo.InvariantCulture, "Deadlock detected in proc {0}", storedProcName), sqlEx);
                    throw;
                }
            }

            return result;
        }

        // is this an optimization of the above?
        static public XmlReader RunSPReturnXmlReader2(string storedProcName, params object[] paramValues)
        {
            SqlDatabase db = GetDatabase() as SqlDatabase;
            using (SqlCommand cmd = db.GetStoredProcCommand(storedProcName, paramValues) as SqlCommand)
            {
                cmd.CommandTimeout = TimeoutSeconds;
                using (XmlReader reader = cmd.ExecuteXmlReader())
                {
                    XPathDocument doc = new XPathDocument(reader);
                    return doc.CreateNavigator().ReadSubtree();
                }
            }
        }

        static public void RunSP(string storedProcName, params object[] paramValues)
        {
            RunSPDatabase(null, storedProcName, paramValues);
        }

        static public void RunSPDatabase(string databaseName, string storedProcName, params object[] paramValues)
        {
            Database db = GetDatabase(databaseName);
            using (DbCommand cmd = GetStoredProcCommand(db, storedProcName, paramValues))
            {
                try
                {
                    db.ExecuteNonQuery(cmd);
                }
                catch (SqlException sqlEx)
                {
                    if (sqlEx.Number == DEADLOCK)
                        log.Warn(string.Format(CultureInfo.InvariantCulture, "Deadlock detected in proc {0}", storedProcName), sqlEx);
                    throw;
                }
            }
        }

        static public byte[] RunSPReturnBinary(string storedProcName, params object[] paramValues)
        {
            Database db = GetDatabase();
            using (DbCommand cmd = GetStoredProcCommand(db, storedProcName, paramValues))
                return (byte[])db.ExecuteScalar(cmd);
        }

        static public DataSet RunSPReturnDataSet(string storedProcName, params object[] paramValues)
        {
            return RunSPReturnDataSetDatabase(null, storedProcName, paramValues);
        }

        static public DataSet RunSPReturnDataSetDatabase(string databaseName, string storedProcName, params object[] paramValues)
        {
            Database db = GetDatabase(databaseName);
            using (DbCommand cmd = GetStoredProcCommand(db, storedProcName, paramValues))
                return db.ExecuteDataSet(cmd);
        }

        static public XElement RunSPReturnXElement(string databaseName, string storedProcName, params object[] paramValues)
        {
            using (DataSet ds = RunSPReturnDataSetDatabase(databaseName, storedProcName, paramValues))
            {
                if (ds != null)
                {
                    return XElement.Parse(ds.GetXml());
                }
                return null;
            }
        }

        /// <summary>       
        /// Not use EntLib. For referencing different EntLib version case. 
        /// </summary>
        static public DataSet RunSPReturnDataSetDatabase2(string databaseName, string storedProcName, params SqlParameter[] paramValues)
        {
            DataSet ds = new DataSet();
            SqlConnection conn = null;
            try
            {
                string connectionString = GetDatabaseConnectionString(databaseName);
                using (conn = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = storedProcName;
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (paramValues != null)
                    {
                        foreach (var param in paramValues)
                        {
                            cmd.Parameters.Add(param);
                        }
                    }

                    SqlDataAdapter da = new SqlDataAdapter(cmd);

                    conn.Open();

                    da.Fill(ds);

                    conn.Close();
                }
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open)
                    conn.Close();
            }

            return ds;
        }
        
        static public IDataReader RunSPReturnDataReader(string storedProcName, params object[] paramValues)
        {
            return RunSPReturnDataReaderDatabase(null, storedProcName, paramValues);
        }

        static public IDataReader RunSPReturnDataReaderDatabase(string databaseName, string storedProcName, params object[] paramValues)
        {
            Database db = GetDatabase(databaseName);
            using (DbCommand cmd = GetStoredProcCommand(db, storedProcName, paramValues))
            {
                return db.ExecuteReader(cmd);
            }
        }

        static public object RunSPReturnValue(string storedProcName, params object[] paramValues)
        {
            return RunSPReturnValueResultVariable(storedProcName, null, paramValues);
        }

        static public object RunSPReturnValueResultVariable(string storedProcName, string resultVariable, params object[] paramValues)
        {
            object retVal = null;
            object[] newParamValues = new object[paramValues.Length + 1];
            paramValues.CopyTo(newParamValues, 0);
            newParamValues[newParamValues.Length - 1] = null;

            Database db = GetDatabase();
            using (DbCommand cmd = GetStoredProcCommand(db, storedProcName, newParamValues))
            {
                try
                {
                    db.ExecuteNonQuery(cmd);
                    retVal = cmd.Parameters[string.IsNullOrEmpty(resultVariable) ? "@retval" : resultVariable].Value;
                }
                catch (SqlException sqlEx)
                {
                    if (sqlEx.Number == DEADLOCK)
                        log.Warn(string.Format(CultureInfo.InvariantCulture, "Deadlock detected in proc {0}", storedProcName), sqlEx);
                    throw;
                }
            }

            return retVal;
        }

        static public bool RunSPReturnBool(string storedProcName, params object[] paramValues)
        {
            return (bool)RunSPReturnValue(storedProcName, paramValues);
        }

        static public int RunSPReturnInt(string storedProcName, params object[] paramValues)
        {
            return (int)RunSPReturnValue(storedProcName, paramValues);
        }

        static public Guid RunSPReturnGuid(string storedProcName, params object[] paramValues)
        {
            return (Guid)RunSPReturnValue(storedProcName, paramValues);
        }

        static public int RunSPReturnIntDatabase(string databaseName, string storedProcName, params object[] paramValues)
        {
            return RunSPReturnIntDatabaseResultVariable(databaseName, null, storedProcName, paramValues);
        }

        static public int RunSPReturnIntDatabaseResultVariable(string databaseName, string resultVariable, string storedProcName, params object[] paramValues)
        {
            int retVal;
            object[] newParamValues = new object[paramValues.Length + 1];
            paramValues.CopyTo(newParamValues, 0);
            newParamValues[newParamValues.Length - 1] = null;

            Database db = GetDatabase(databaseName);
            using (DbCommand cmd = GetStoredProcCommand(db, storedProcName, newParamValues))
            {
                db.ExecuteNonQuery(cmd);
                retVal = (int)db.GetParameterValue(cmd, string.IsNullOrEmpty(resultVariable) ? "@retval" : resultVariable);
            }
            return retVal;
        }

        // pass nulls for output parameters, we need to match the parameter count on the proc
        // returns object array of all output and in/out parameters in parameter order
        static public object[] RunSPReturnValues(string storedProcName, params object[] paramValues)
        {
            return RunSPReturnValuesDatabase(null, storedProcName, paramValues);
        }

        // pass nulls for output parameters, we need to match the parameter count on the proc
        // returns object array of all output and in/out parameters in parameter order
        static public object[] RunSPReturnValuesDatabase(string databaseName, string storedProcName, params object[] paramValues)
        {
            ArrayList objArray = new ArrayList();

            Database db = GetDatabase(databaseName);
            using (DbCommand cmd = GetStoredProcCommand(db, storedProcName, paramValues))
            {
                db.ExecuteNonQuery(cmd);
                foreach (DbParameter param in cmd.Parameters)
                {
                    if (param.Direction == ParameterDirection.InputOutput || param.Direction == ParameterDirection.Output)
                        objArray.Add(param.Value);
                }
            }

            return objArray.ToArray();
        }

        static public object[] RunSPReturnValuesUseTransaction(string storedProcName, params object[] paramValues)
        {
            object[] result;
            using (TransactionScope tx = new TransactionScope())
            {
                result = SqlHelpers.RunSPReturnValues(storedProcName, paramValues);
                tx.Complete();
            }

            return result;
        }

        static public XmlReader RunSPReturnXmlReaderUseTransaction(string storedProcName, params object[] paramValues)
        {
            XmlReader result;
            using (TransactionScope tx = new TransactionScope())
            {
                result = SqlHelpers.RunSPReturnXmlReader(storedProcName, paramValues);
                tx.Complete();
            }
            return result;
        }

        static public void RunSPUseTransactionDatabase(string databaseName, string storedProcName, params object[] paramValues)
        {
            using (TransactionScope tx = new TransactionScope())
            {
                SqlHelpers.RunSPDatabase(databaseName, storedProcName, paramValues);
                tx.Complete();
            }
        }

        static public void RunSPUseTransaction(string storedProcName, params object[] paramValues)
        {
            RunSPUseTransactionDatabase(null, storedProcName, paramValues);
        }

        static public bool RunSPReturnBoolUseTransaction(string storedProcName, params object[] paramValues)
        {
            bool retVal;
            using (TransactionScope tx = new TransactionScope())
            {
                retVal = SqlHelpers.RunSPReturnBool(storedProcName, paramValues);
                tx.Complete();
            }
            return retVal;
        }

        static public int RunSPReturnIntUseTransactionDatabase(string databaseName, string storedProcName, params object[] paramValues)
        {
            int retVal;
            using (TransactionScope tx = new TransactionScope())
            {
                retVal = SqlHelpers.RunSPReturnIntDatabase(databaseName, storedProcName, paramValues);
                tx.Complete();
            }
            return retVal;
        }

        static public int RunSPReturnIntUseTransaction(string storedProcName, params object[] paramValues)
        {
            return RunSPReturnIntUseTransactionDatabase(null, storedProcName, paramValues);
        }

        // This is a temporary helper until we go to SQL 2005,
        // where we anticipate retrieving an XmlReader directly from ADO.NET
        // and returning that same XmlReader to application.
        // In SQL 2000, readers returned from ADO.NET must be explicity closed 
        // to return the DB connection to the pool,
        // but SQL 2005 is optimized to share open connections, which
        // will eliminate the need to copy the data before returning it.
        static public XmlReader GetXmlFromDataReader(IDataReader reader)
        {
            MemoryStream ms = null;

            try
            {
                ms = new MemoryStream();
                using (XmlWriter writer = XmlHelpers.XmlHelpers.CreateXmlWriterForReading(ms))
                {
                    do
                    {
                        // Yes, we are copying data from one reader to another buffer for reading,
                        // in order to close the SQL Server 2000 DB connection maintained by first reader.
                        while (reader.Read())
                        {
                            writer.WriteRaw(reader.GetString(0));
                        }
                    } while (reader.NextResult());
                }

                return XmlHelpers.XmlHelpers.CreateXmlReaderFromMemoryStream(ms);
            }
            catch (Exception ex)
            {
                if (ms != null)
                    ms.Close();
                throw ex;
            }
            finally
            {
                reader.Close(); // required to Close underlying SQL Server 2000 DB connection
            }
        }

        //converts a input string containing pattern matching characters to literals so that can be used in a LIKE comparison
        static public string LikeOperatorStringLiteral(string v)
        {
            v = v.Replace("[", "[[").Replace("]", "]]").Replace("'", "[']").Replace("%", "[%]").Replace("_", "[_]");
            return v;
        }

        public static string OverrideDataSource(string connStr, string dataSource)
        {
            string[] pairs = connStr.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < pairs.Length; i++)
            {
                string[] keyVal = pairs[i].Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                if (keyVal.Length == 2 && keyVal[0].ToUpperInvariant() == "DATA SOURCE")
                {
                    keyVal[1] = dataSource;
                    pairs[i] = string.Join("=", keyVal);
                    break;
                }
            }

            return string.Join(";", pairs);
        }
    }

    public static class ConfigDatabaseFactory
    {
        public static string connString
        {
            set
            {
                _connString = value;
            }
        }

        public static Database CreateDatabase()
        {
            if (string.IsNullOrEmpty(_connString))
            {
                return DatabaseFactory.CreateDatabase();
            }
            else
            {
                return new SqlDatabase(_connString);
            }
        }

        private static string _connString = string.Empty;
    }
}
