using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Migrataur
{
    public sealed class ScriptEngine : IDisposable
    {
        private readonly SqlConnection _dbConnection;
        private readonly ScriptProvider _scriptProvider;
        private readonly object @lock = new object();

        /// <summary>
        /// Create an instance of a ScriptEngine which receives a database connection string and a configured ScriptProvider from which it gets the migration scripts.
        /// </summary>
        /// <param name="dbConnectionString">Your database connection string.</param>
        /// <param name="scriptProvider">The configured ScriptProvider object.</param>
        public ScriptEngine(string dbConnectionString, ScriptProvider scriptProvider)
        {
            _dbConnection = new SqlConnection(dbConnectionString);
            _dbConnection.Open();

            _scriptProvider = scriptProvider;
        }

        /// <summary>
        /// Checks whether there are any pending migration scripts to be applied to the database.
        /// </summary>
        /// <returns>Boolean representing whether there are migration scripts waiting to be applied.</returns>
        public bool NeedsUpdating()
        {
            lock (@lock)
            {
                CreateHistoryTableIfNotExists();

                using (SqlCommand dbCommand = new SqlCommand())
                {
                    dbCommand.Connection = _dbConnection;
                    dbCommand.CommandType = CommandType.Text;
                    dbCommand.CommandTimeout = 60;
                    dbCommand.CommandText = "select ScriptID from MigrationHistory";

                    using (SqlDataReader dbReader = dbCommand.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            _scriptProvider.RemoveScript(dbReader["ScriptID"].ToString());
                        }
                    }                    
                }
            }

            return _scriptProvider.GetScripts().Count > 0;
        }

        /// <summary>
        /// Applies any pending migration scripts to the database in a transaction, if any migration script fails then ALL scripts will be rolled back.
        /// </summary>
        public void Update()
        {
            lock (@lock)
            {
                using (SqlTransaction dbTransaction = _dbConnection.BeginTransaction())
                {
                    try
                    {
                        foreach (var script in _scriptProvider.GetScripts().OrderBy(s => s.Name))
                        {
                            string scriptBody = script.Content.Replace("\\r\\n", "");

                            while (!string.IsNullOrEmpty(scriptBody))
                            {
                                string scriptStep = scriptBody.Substring(0, scriptBody.IndexOf(';') + 1).Trim();

                                scriptBody = scriptBody.Replace(scriptStep, "").Trim();

                                ApplyMigrationScript(scriptStep, dbTransaction);
                            }

                            RegisterMigrationScript(script, dbTransaction);
                        }

                        dbTransaction.Commit();
                    }
                    catch (SqlException sqlEx)
                    {
                        dbTransaction.Rollback();

                        throw sqlEx;
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_dbConnection != null)
            {
                if (_dbConnection.State == ConnectionState.Open)
                {
                    _dbConnection.Close();
                }

                _dbConnection.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        internal void CreateHistoryTableIfNotExists()
        {
            using (SqlTransaction dbTransaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    using (SqlCommand dbCommand = new SqlCommand())
                    {
                        dbCommand.Connection = _dbConnection;
                        dbCommand.Transaction = dbTransaction;
                        dbCommand.CommandType = CommandType.Text;
                        dbCommand.CommandTimeout = 60;
                        dbCommand.CommandText = "if not exists (select * from sysobjects where name = 'MigrationHistory' and xtype = 'U') create table MigrationHistory ( ScriptID varchar(200) primary key, DateApplied datetime not null );";
                        dbCommand.ExecuteNonQuery();
                    }

                    dbTransaction.Commit();
                }
                catch (SqlException sqlEx)
                {
                    dbTransaction.Rollback();

                    throw sqlEx;
                }
            }
        }

        internal void ApplyMigrationScript(string script, SqlTransaction dbTransaction)
        {
            using (SqlCommand dbCommand = new SqlCommand())
            {
                dbCommand.Connection = _dbConnection;
                dbCommand.Transaction = dbTransaction;
                dbCommand.CommandType = CommandType.Text;
                dbCommand.CommandTimeout = 60;
                dbCommand.CommandText = script;
                dbCommand.ExecuteNonQuery();
            }
        }

        internal void RegisterMigrationScript(Script script, SqlTransaction dbTransaction)
        {
            using (SqlCommand dbCommand = new SqlCommand())
            {
                dbCommand.Connection = _dbConnection;
                dbCommand.Transaction = dbTransaction;
                dbCommand.CommandType = CommandType.Text;
                dbCommand.CommandTimeout = 30;
                dbCommand.CommandText = "insert into MigrationHistory ( ScriptID, DateApplied ) values ( @scriptid, getdate() );";
                dbCommand.Parameters.Add("scriptid", SqlDbType.VarChar).Value = script.Name;
                dbCommand.ExecuteNonQuery();
            }
        }
    }
}