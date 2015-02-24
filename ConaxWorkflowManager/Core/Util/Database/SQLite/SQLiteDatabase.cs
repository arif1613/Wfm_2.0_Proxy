using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Data;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database.SQLite
{
    public class SQLiteDatabase
    {

        String dbConnection;
        private System.Object lockThis = new System.Object();

        public SQLiteDatabase()
            : this(Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == "ConaxWorkflowManager").GetConfigParam("DBSource")) { }
        
        public SQLiteDatabase(String inputFile)
        {
            dbConnection = String.Format("Data Source={0}", inputFile);
        }

        public SQLiteDatabase(Dictionary<String, String> connectionOpts)
        {
            String str = "";

            foreach (KeyValuePair<String, String> row in connectionOpts)
            {
                str += String.Format("{0}={1}; ", row.Key, row.Value);
            }

            str = str.Trim().Substring(0, str.Length - 1);

            dbConnection = str;
        }

        //public DataTable GetDataTable(string sql)
        //{
        //    DataTable dt = new DataTable();
        //    try
        //    {
        //        lock (lockThis)
        //        {
        //            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
        //            cnn.Open();
        //            SQLiteCommand mycommand = new SQLiteCommand(cnn);
        //            mycommand.CommandText = sql;
        //            SQLiteDataReader reader = mycommand.ExecuteReader();
        //            dt.Load(reader);
        //            reader.Close();
        //            reader.Dispose();
        //            mycommand.Dispose();
        //            cnn.Close();
        //            cnn.Dispose();
        //            GC.Collect();
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception(e.Message);
        //    }
        //    return dt;
        //}

        //public int ExecuteNonQuery(string sql)
        //{
        //    int rowsUpdated = 0;
        //    lock (lockThis)
        //    {
        //        SQLiteConnection cnn = new SQLiteConnection(dbConnection);
        //        cnn.Open();
        //        SQLiteCommand mycommand = new SQLiteCommand(cnn);
        //        mycommand.CommandText = sql;
        //        rowsUpdated = mycommand.ExecuteNonQuery();
        //        mycommand.Dispose();
        //        cnn.Close();
        //        cnn.Dispose();
        //        GC.Collect();
        //    }
        //    return rowsUpdated;
        //}

        //public void ClearAndDefragDB()
        //{
        //    lock (lockThis)
        //    {
        //        SQLiteConnection cnn = new SQLiteConnection(dbConnection);
        //        cnn.Open();
        //        using (SQLiteCommand command = cnn.CreateCommand())
        //        {
        //            command.CommandText = "vacuum;";
        //            command.ExecuteNonQuery();
        //        } 
        //        cnn.Close();
        //        cnn.Dispose();
        //        GC.Collect();
        //    }
           
        //}

        //public string ExecuteScalar(string sql)
        //{
        //    SQLiteConnection cnn = new SQLiteConnection(dbConnection);
        //    cnn.Open();
        //    SQLiteCommand mycommand = new SQLiteCommand(cnn);
        //    mycommand.CommandText = sql;
        //    object value = mycommand.ExecuteScalar();
        //    mycommand.Dispose();
        //    cnn.Close();
        //    cnn.Dispose();
        //    GC.Collect();
        //    if (value != null)
        //    {
        //        return value.ToString();    
        //    }
        //    return "";
        //}

        public Int32 Update(String tableName, Dictionary<String, String> data, String where)
        {
            String vals = "";
            Int32 returnCode = 0;
            if (data.Count >= 1)
            {
                foreach (KeyValuePair<String, String> val in data)
                {
                    vals += String.Format(" {0} = '{1}',", val.Key.ToString(), (val.Value != null) ? val.Value.Replace("'", "''") : val.Value);
                }
                vals = vals.Substring(0, vals.Length - 1);
            }
            //try
            //{
            //    returnCode = this.ExecuteNonQuery(String.Format("update {0} set {1} where {2};", tableName, vals, where));
            //}
            //catch
            //{
            //    throw;
            //}
            return returnCode;
        }


        public bool Delete(String tableName, String where)
        {
            Boolean returnCode = true;
            //try
            //{
            //    this.ExecuteNonQuery(String.Format("delete from {0} where {1};", tableName, where));
            //}
            //catch (Exception fail)
            //{                
            //    returnCode = false;
            //}
            return returnCode;
        }

        public bool Insert(String tableName, Dictionary<String, String> data)
        {
            String columns = "";
            String values = "";
            Boolean returnCode = true;
            foreach (KeyValuePair<String, String> val in data)
            {
                columns += String.Format(" {0},", val.Key.ToString());
                values += String.Format(" '{0}',", (val.Value != null) ? val.Value.Replace("'", "''") : val.Value);
            }
            columns = columns.Substring(0, columns.Length - 1);
            values = values.Substring(0, values.Length - 1);
            //try
            //{
            //    this.ExecuteNonQuery(String.Format("insert into {0}({1}) values({2});", tableName, columns, values));
            //}
            //catch(Exception ex)
            //{
            //    throw;
            //}
            return returnCode;
        }

        public bool ClearDB()
        {
            DataTable tables;
            try
            {
                //tables = this.GetDataTable("select NAME from SQLITE_MASTER where type='table' order by NAME;");
                //foreach (DataRow table in tables.Rows)
                //{
                //    this.ClearTable(table["NAME"].ToString());
                //}
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ClearTable(String table)
        {
            try
            {
               // this.ExecuteNonQuery(String.Format("delete from {0};", table));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
