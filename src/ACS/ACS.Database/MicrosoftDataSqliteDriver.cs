using NHibernate.Driver;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace ACS.Database
{
    /// <summary>
    /// NHibernate driver for Microsoft.Data.Sqlite (cross-platform SQLite support)
    /// </summary>
    public class MicrosoftDataSqliteDriver : ReflectionBasedDriver
    {
        public MicrosoftDataSqliteDriver()
            : base(
                "Microsoft.Data.Sqlite",
                "Microsoft.Data.Sqlite.SqliteConnection",
                "Microsoft.Data.Sqlite.SqliteCommand")
        {
        }

        public override DbConnection CreateConnection()
        {
            return new SqliteConnection();
        }

        public override DbCommand CreateCommand()
        {
            return new SqliteCommand();
        }

        public override bool UseNamedPrefixInSql => true;

        public override bool UseNamedPrefixInParameter => true;

        public override string NamedPrefix => "@";

        public override bool SupportsMultipleOpenReaders => false;
    }
}
