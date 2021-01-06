using System;
using MyNoSqlGrpc.Engine.Db;
using MyNoSqlGrpc.Engine.ServerSessions;

namespace MyNoSqlGrpc.Engine.Api
{
    public static class ApiReaderExtensions
    {
        public static MyNoSqlReaderSession GetSession(this MyNoSqlReaderSessionsList sessionsList, string connectionId)
        {
            var session = sessionsList.TryGetSession(connectionId);
            if (session == null)
               throw new Exception($"Session {connectionId} is expired");

            return session;
        }


        public static DbTable GetTable(this DbTablesList dbTablesList, string tableName)
        {
            var table = dbTablesList.TryGetTable(tableName);
            if (table == null)
                throw new Exception($"Table {tableName} is not found");

            return table;
        }
        
        
        
    }
}