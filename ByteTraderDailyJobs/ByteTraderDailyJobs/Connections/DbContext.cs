using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace ByteTraderDailyJobs.Connections
{
    public abstract class DbContext
    {
        internal IDbConnection Connection
        {
            get
            {
                return new SqlConnection("ConnectionString");
            }
        }
        internal SqlConnection SqlConnect
        {
            get
            {
                return new SqlConnection("");
            }
        }
    }
}
