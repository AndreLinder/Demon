using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demon
{
    internal class DBUtils
    {
        public static MySqlConnection GetDBConnection()
        {
            string host = "192.168.50.219";
            int port = 3306;
            string database = "server_chats";
            string username = "andrelinder";
            string password = "gusar1628652470";

            return DBMySQLUtils.GetDBConnection(host, port, database, username, password);
        }
    }
}
