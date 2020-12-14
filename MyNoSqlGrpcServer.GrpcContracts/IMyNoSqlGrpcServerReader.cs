using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace MyNoSqlGrpcServer.GrpcContracts
{
    [ServiceContract(Name = "MyNoSqlGrpcServerReader")]
    public interface IMyNoSqlGrpcServerReader
    {
        
        [OperationContract(Action = "Greeting")]
        ValueTask<GreetingGrpcResponse> GreetingAsync(GreetingGrpcRequest request, CancellationToken token = default);
        
        [OperationContract(Action = "Subscribe")]
        IAsyncEnumerable<DbRowGrpcModel> SubscribeAsync(SubscribeGrpcRequest request, CancellationToken token = default);

        [OperationContract(Action = "GetUpdates")]
        ValueTask<UpdatesGrpcResponse> GetUpdatesAsync(GetUpdatesGrpcRequest request, CancellationToken token = default);
        
        [OperationContract(Action = "DownloadPartition")]
        IAsyncEnumerable<DbRowGrpcModel> DownloadPartitionAsync(DownloadPartitionGrpcRequest request, CancellationToken token = default);
        
        [OperationContract(Action = "SyncRows")]
        IAsyncEnumerable<DbRowGrpcModel> SyncRowsAsync(SyncRowsGrpcRequest request, CancellationToken token = default);
    }
}