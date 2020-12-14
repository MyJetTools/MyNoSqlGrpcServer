using System;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.DependencyInjection;
using MyNoSqlGrpc.Engine.Db;
using MyNoSqlGrpc.Engine.ServerSessions;
using MyNoSqlGrpc.Engine.ServerSyncEvents;

namespace MyNoSqlGrpc.Server
{
    public static class ServiceLocator
    {
        public static DbTablesList DbTablesList { get; private set; }

        public static MyNoSqlReaderSessionsList MyNoSqlReaderSessionsList { get; private set; }
        public static SyncEventsQueue SyncEventsQueue { get; private set; }
        public static SyncEventsPusher SyncEventsPusher { get; private set; }

        private static readonly TaskTimer SecondTaskTimer = new (TimeSpan.FromSeconds(1));
        public static void Init(IServiceCollection sc)
        {
            var sp = sc.BuildServiceProvider();
            SyncEventsPusher = sp.GetRequiredService<SyncEventsPusher>();
            MyNoSqlReaderSessionsList = sp.GetRequiredService<MyNoSqlReaderSessionsList>();
            SyncEventsQueue = sp.GetRequiredService<SyncEventsQueue>();

            DbTablesList = sp.GetRequiredService<DbTablesList>();
        }

        public static void Start()
        {
            SecondTaskTimer.Register("Ping Sessions", () =>
            {
                MyNoSqlReaderSessionsList.PingSessions();
                return new ValueTask();
            });
            
            SecondTaskTimer.Start();
        }

    }
}