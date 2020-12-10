using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;

namespace MyNoSqlGrpcServer.GrpcContracts
{
    
    [ServiceContract(Name = "MyNoSqlGrpcServer")]
    public interface IMyNoSqlGrpcServer
    {
        
        [OperationContract(Action = "CreateTableIfNotExists")]
        ValueTask CreateTableIfNotExistsAsync(CreateTableGrpcRequest reqContract);

        [OperationContract(Action = "Insert")]
        ValueTask<GrpcResponse> InsertAsync(RowWithTableNameGrpcRequest request);
        
        [OperationContract(Action = "InsertOrReplace")]
        ValueTask<GrpcResponse> InsertOrReplaceAsync(RowWithTableNameGrpcRequest dbRow);

        [OperationContract(Action = "BulkInsertOrReplace")]
        ValueTask<GrpcResponse> BulkInsertOrReplaceAsync(RowsWithTableNameGrpcRequest dbRows);
        
        [OperationContract(Action = "Modify")]
        ValueTask<GrpcResponseDbRow> ModifyAsync(RowWithTableNameGrpcRequest request);

        [OperationContract(Action = "Get")]
        IAsyncEnumerable<DbRowGrpcModel> GetAsync(GetDbRowsGrpcRequest request);

        [OperationContract(Action = "GcPartition")]
        ValueTask<GrpcResponse> GcPartitionAsync(GcPartitionGrpcRequest request);
        
        [OperationContract(Action = "GcTable")]
        ValueTask<GrpcResponse> GcTableAsync(GcTableGrpcRequest request);
    }
}