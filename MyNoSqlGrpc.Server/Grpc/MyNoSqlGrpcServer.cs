using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Server.Grpc
{
    public class MyNoSqlGrpcServer : IMyNoSqlGrpcServer
    {

        public ValueTask CreateTableIfNotExistsAsync(CreateTableGrpcRequest reqContract)
        {
            ServiceLocation.DbTablesList.CreateIfNotExists(reqContract.TableName.ToLower());
            return new ValueTask();
        }

        public ValueTask<GrpcResponse> InsertAsync(RowWithTableNameGrpcRequest request)
        {
            var dbTable = ServiceLocation.DbTablesList.TryGetTable(request.TableName);

            var result = new GrpcResponse
            {
                Status = GrpcResultStatus.Ok
            };

            if (dbTable == null)
                result.Status = GrpcResultStatus.TableNotFound;
            else
                dbTable.LockWithWriteAccess(writeAccess =>
                {
                    var dbPartition = writeAccess.GetOrCreatePartition(request.DbRow.PartitionKey);
                    var insertResult = dbPartition.Insert(request.DbRow);

                    if (!insertResult)
                        result.Status = GrpcResultStatus.RecordAlreadyExists;
                });

            return new ValueTask<GrpcResponse>(result);
        }

        public ValueTask<GrpcResponse> InsertOrReplaceAsync(RowWithTableNameGrpcRequest request)
        {
            var dbTable = ServiceLocation.DbTablesList.TryGetTable(request.TableName);

            var result = new GrpcResponse
            {
                Status = GrpcResultStatus.Ok
            };

            if (dbTable == null)
                result.Status = GrpcResultStatus.TableNotFound;
            else
                dbTable.LockWithWriteAccess(writeAccess =>
                {
                    var dbPartition = writeAccess.GetOrCreatePartition(request.DbRow.PartitionKey);
                    dbPartition.InsertOrReplace(request.DbRow);
                });

            return new ValueTask<GrpcResponse>(result);
        }

        public ValueTask<GrpcResponse> BulkInsertOrReplaceAsync(RowsWithTableNameGrpcRequest dbRows)
        {
            var result = new GrpcResponse();

            var dbTable =  ServiceLocation.DbTablesList.TryGetTable(dbRows.TableName);

            if (dbTable == null)
            {
                result.Status = GrpcResultStatus.TableNotFound;
                return new ValueTask<GrpcResponse>(result);
            }

            var groupByPartitionKey = dbRows.DbRows.GroupBy(itm => itm.PartitionKey);
            
            dbTable.LockWithWriteAccess(writeAccess =>
            {
                foreach (var group in groupByPartitionKey)
                {
                    var partition = writeAccess.GetOrCreatePartition(group.Key);

                    foreach (var dbRow in group)
                        partition.InsertOrReplace(dbRow);

                }
                
            });

            result.Status = GrpcResultStatus.Ok;
            return new ValueTask<GrpcResponse>(result);
        }

        public ValueTask<GrpcResponseDbRow> ModifyAsync(RowWithTableNameGrpcRequest request)
        {
            var result = new GrpcResponseDbRow();
            var table = ServiceLocation.DbTablesList.TryGetTable(request.TableName);

            if (table == null)
            {
                result.Status = GrpcResultStatus.TableNotFound;
                return new ValueTask<GrpcResponseDbRow>(result);
            }
            
            table.LockWithWriteAccess(writeAccess =>
            {
                var partition = writeAccess.TryGetPartition(request.DbRow.PartitionKey);
                if (partition == null)
                {
                    result.Status = GrpcResultStatus.NotFound;
                    return;
                }

                var dbRow = partition.TryGet(request.DbRow.RowKey);
                if (dbRow == null)
                {
                    result.Status = GrpcResultStatus.NotFound;
                    return;     
                }


                if (dbRow.TimeStamp != request.DbRow.TimeStamp)
                {
                    result.Status = GrpcResultStatus.RecordChanged;
                    return;
                }

                partition.InsertOrReplace(request.DbRow);

            });

            return new ValueTask<GrpcResponseDbRow>(result);

        }

        public async IAsyncEnumerable<DbRowGrpcModel> GetAsync(GetDbRowsGrpcRequest request)
        {
            var table = ServiceLocation.DbTablesList.TryGetTable(request.TableName);

            if (table == null) 
                yield break;
            
            if (request.RowKey == null)
            {
                foreach (var dbRow in table.Get(request.PartitionKey))
                    yield return dbRow;
            }
            else
            {
                var dbRow = table.Get(request.PartitionKey, request.RowKey);

                if (dbRow != null)
                    yield return dbRow;
            }
        }

        public ValueTask<GrpcResponse> GcPartitionAsync(GcPartitionGrpcRequest request)
        {
            var result = new GrpcResponse();
            var table = ServiceLocation.DbTablesList.TryGetTable(request.TableName);

            if (table == null)
            {
                result.Status = GrpcResultStatus.TableNotFound;
                return new ValueTask<GrpcResponse>(result);
            }


            result.Status = GrpcResultStatus.Ok;
            var rowsAmount = table.GetAmount(request.PartitionKey);

            if (rowsAmount > request.MaxRowsAmount)
                table.Gc(request.PartitionKey, request.MaxRowsAmount);

            return new ValueTask<GrpcResponse>(result);
        }

        public ValueTask<GrpcResponse> GcTableAsync(GcTableGrpcRequest request)
        {
            var result = new GrpcResponse();
            var table = ServiceLocation.DbTablesList.TryGetTable(request.TableName);

            if (table == null)
            {
                result.Status = GrpcResultStatus.TableNotFound;
                return new ValueTask<GrpcResponse>(result);
            }

            table.Gc(request.MaxPartitionsAmount);
            return new ValueTask<GrpcResponse>();
        }
        
    }
}