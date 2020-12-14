using System;
using Microsoft.Extensions.DependencyInjection;
using MyNoSqlGrpc.Engine.Db;
using MyNoSqlGrpc.Engine.ServerSessions;
using MyNoSqlGrpc.Engine.ServerSyncEvents;

namespace MyNoSqlGrpc.Engine
{
    public interface IMyNoSqlGrpcEngineSettings
    {
        public string SessionExpiration { get; set; }
    }
    
    public static class BindEngineServices
    {

        public static void RegisterEngineServices(this IServiceCollection serviceCollection, IMyNoSqlGrpcEngineSettings settings)
        {
            serviceCollection.AddSingleton<DbTablesList>();

            var sessionExpirationTimeOut = TimeSpan.Parse(settings.SessionExpiration);
            serviceCollection.AddSingleton(new MyNoSqlReaderSessionsList(sessionExpirationTimeOut));
            serviceCollection.AddSingleton(new SyncEventsQueue());
            serviceCollection.AddSingleton<SyncEventsPusher>();
        }
        
    }
}