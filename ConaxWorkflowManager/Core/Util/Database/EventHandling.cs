using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
//using System.Data.SQLite;
using System.Data;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database
{
    public class EventHandling
    {
       
        /*
        public ulong GetLastID()
        {
            ulong ret = 0;
            Config config = Config.GetConfig();
            String connectionString = "Data Source=" + config.DatabaseLocation +"\\" + config.DatabaseName + "; Version=3;";
            String selectCommand = "Select EventID from ContentEvent order by ID desc limit 1";
            
            SQLiteConnection connection = new SQLiteConnection(connectionString);

            SQLiteCommand command = new SQLiteCommand(selectCommand, connection);

            SQLiteDataReader dr = command.ExecuteReader();
            while (dr.Read())
            {
                String value = dr["EventID"].ToString();
                ret = ulong.Parse(value);
            }  

            return ret;
        }
         * */
    }
}
