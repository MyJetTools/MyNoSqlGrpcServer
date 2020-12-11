using MyNoSqlGrpc.Engine.Db;

namespace MyNoSqlGrpc.Server
{
    public static class ServiceLocator
    {

        public static readonly DbTablesList DbTablesList = new ();

    }
}