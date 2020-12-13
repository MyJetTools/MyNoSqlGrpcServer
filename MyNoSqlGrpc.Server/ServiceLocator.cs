using MyNoSqlGrpc.Engine.Db;
using MyNoSqlGrpc.Server.Services;

namespace MyNoSqlGrpc.Server
{
    public static class ServiceLocator
    {

        public static readonly DbTablesList DbTablesList = new ();

        public static readonly MyNoSqlReaderSessionsList MyNoSqlReaderSessionsList = new ();
        

    }
}