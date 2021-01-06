using System;
using Microsoft.Extensions.DependencyInjection;
using MyNoSqlGrpc.Engine.Api;
using MyNoSqlGrpc.Engine.Db;
using MyNoSqlGrpc.Engine.ServerSessions;
using MyNoSqlGrpc.Engine.ServerSyncEvents;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Engine
{
    public interface IMyNoSqlGrpcEngineSettings
    {
        public TimeSpan SessionExpiration { get; }
        public int MaxPayloadSize { get; }
    }
    
    public static class BindEngineServices
    {

        public static void RegisterEngineServices(this IServiceCollection serviceCollection, IMyNoSqlGrpcEngineSettings settings)
        {
            serviceCollection.AddSingleton<DbTablesList>();

            serviceCollection.AddSingleton(settings);
            
            serviceCollection.AddSingleton<MyNoSqlReaderSessionsList>();
            serviceCollection.AddSingleton(new SyncEventsQueue());
            serviceCollection.AddSingleton<SyncEventsPusher>();
            
            serviceCollection.AddSingleton<IMyNoSqlGrpcServerWriter, MyNoSqlGrpcServerWriter>();
            serviceCollection.AddSingleton<IMyNoSqlGrpcServerReader, MyNoSqlGrpcServerReader>();
        }
        
    }
}