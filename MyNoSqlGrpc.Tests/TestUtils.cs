using System;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using MyNoSqlGrpc.Engine;
using MyNoSqlGrpc.Writer;

namespace MyNoSqlGrpc.Tests
{

    public class TestSettings : IMyNoSqlGrpcEngineSettings
    {
        public TimeSpan SessionExpiration { get; } = TimeSpan.FromMinutes(1);
        public int MaxPayloadSize { get; set; } = 3000000;
    }
    
    public static class TestUtils
    {

        public static ServiceProvider CreateTestServiceCollection()
        {
            var sc = new ServiceCollection();
            var settings = new TestSettings();
            sc.RegisterEngineServices(settings);
            return sc.BuildServiceProvider();
        }

        public static MyNoSqlGrpcWriter<T> PlugJsonSerializer<T>(this MyNoSqlGrpcWriter<T> src)
        {
            src.PlugSerializerDeserializer(itm => Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(itm)),
                b =>
                {
                    var json = Encoding.UTF8.GetString(b.Span);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
                }
            );
            
            return src;
        }
        
    }
}