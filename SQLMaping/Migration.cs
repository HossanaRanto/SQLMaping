using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLMaping
{
    public static class Migration
    {
        public static Type[] Classes { get; set; }
        public static MigrationTemplate MigrationTemplate { get; set; }
        
        public static void InitMigration(this DbConnection dbConnection)
        {

        }
    }
}
