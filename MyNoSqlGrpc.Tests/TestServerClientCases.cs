using System.Threading.Tasks;
using AsyncAwaitUtils;
using MyNoSqlGrpc.Server.Grpc;
using MyNoSqlGrpc.Writer;
using NUnit.Framework;

namespace MyNoSqlGrpc.Tests
{

    public class TestEntity
    {
        public int Id { get; set; }
        public string TestField { get; set; }
    }
    
    public class TestServerClientCases
    {

        [Test]

        public async Task TestOneInsertOneGet()
        {

            var server = new MyNoSqlGrpcServerWriterService();
            
            var writer = new MyNoSqlGrpcWriter<TestEntity>(server, "test").PlugJsonSerializer();

            await writer.CreateTableIfNotExistsAsync();

            var srcEntity = new TestEntity
            {
                Id = 5,
                TestField = "test"
            };

            await writer.Insert("pk", "rk", srcEntity).ExecuteAsync();

            var result = await writer.GetAsync("pk", "rk");
            
            Assert.AreEqual(srcEntity.Id, result.Id);
            Assert.AreEqual(srcEntity.TestField, result.TestField);
        }
        
        [Test]

        public async Task TestOneInsertGetAsList()
        {

            var server = new MyNoSqlGrpcServerWriterService();
            
            var writer = new MyNoSqlGrpcWriter<TestEntity>(server, "test").PlugJsonSerializer();

            await writer.CreateTableIfNotExistsAsync();

            var srcEntity = new TestEntity
            {
                Id = 5,
                TestField = "test"
            };

            await writer.Insert("pk", "rk", srcEntity).ExecuteAsync();

            var result = await writer.Get().WithPartitionKey("pk").ExecuteAsync().ToListAsync();
            
            Assert.AreEqual(srcEntity.Id, result[0].Id);
            Assert.AreEqual(srcEntity.TestField, result[0].TestField);
        }
    }
    
    
    
}