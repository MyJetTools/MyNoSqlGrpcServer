using System.Text;
using MyNoSqlGrpc.Writer;

namespace MyNoSqlGrpc.Tests
{
    public static class TestUtils
    {

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