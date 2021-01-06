using System;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.DependencyInjection;
using MyNoSqlGrpc.Engine.ServerSessions;

namespace MyNoSqlGrpc.Server
{
    public static class BackgroundJobsServiceLocator
    {
        private static MyNoSqlReaderSessionsList MyNoSqlReaderSessionsList { get; set; }


        private static readonly TaskTimer SecondTaskTimer = new (TimeSpan.FromSeconds(1));
        public static void Init(IServiceCollection sc)
        {
            var sp = sc.BuildServiceProvider();
            MyNoSqlReaderSessionsList = sp.GetRequiredService<MyNoSqlReaderSessionsList>();
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