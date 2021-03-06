using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyNoSqlGrpc.Engine;
using ProtoBuf.Grpc.Server;

namespace MyNoSqlGrpc.Server
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

            var settings = MySettingsReader.SettingsReader.GetSettings<SettingsModel>(".mynosqlgrpcservser");

            services.AddCodeFirstGrpc();
            services.AddApplicationInsightsTelemetry();
            services.AddControllers();
            services.RegisterEngineServices(settings);
            
            services.AddSwaggerDocument(o => { o.Title = "MyNoSqlGrpcServer"; });
            
            BackgroundJobsServiceLocator.Init(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
                        
            app.UseStaticFiles();

            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGrpcService<Engine.Api.MyNoSqlGrpcServerWriter>();
                endpoints.MapGrpcService<Engine.Api.MyNoSqlGrpcServerReader>();
            });
            
            BackgroundJobsServiceLocator.Start();
        }
    }
}