using System;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;

namespace MyNoSqlGrpc.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Any, 5000,
                            o => o.Protocols = HttpProtocols.Http2);
                        options.Limits.KeepAliveTimeout = 
                            TimeSpan.FromSeconds(5);
                        options.Limits.RequestHeadersTimeout = 
                            TimeSpan.FromSeconds(15);
                    });
                    
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Any, 8000,
                            o => o.Protocols = HttpProtocols.Http1);
                    });
                    
                    webBuilder.UseStartup<Startup>();
                });
    }
}