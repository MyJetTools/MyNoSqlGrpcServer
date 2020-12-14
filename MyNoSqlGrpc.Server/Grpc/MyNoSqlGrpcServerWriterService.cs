using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncAwaitUtils;
using MyNoSqlGrpc.Server.Services;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Server.Grpc
{
    public class MyNoSqlGrpcServerWriterService : IMyNoSqlGrpcServerWriter
    {

        public ValueTask CreateTableIfNotExistsAsync(CreateTableGrpcRequest reqContract)
        {
            ServiceLocator.DbTablesList.CreateIfNotExists(reqContract.TableName.ToLower());
            return new ValueTask();
        }

        public ValueTask<GrpcResponse> InsertAsync(RowWithTableNameGrpcRequest request)
        {
            var dbTable = ServiceLocator.DbTablesList.TryGetTable(request.TableName);

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

                    if (insertResult)
                        ServiceLocator.SyncEventsQueue.EnqueueDbRowChange(dbTable, request.DbRow);
                    else
                        result.Status = GrpcResultStatus.RecordAlreadyExists;
                });

            ServiceLocator.SyncEventsPusher.PushEventsToReaders();
            
            return new ValueTask<GrpcResponse>(result);
        }

        public ValueTask<GrpcResponse> InsertOrReplaceAsync(RowWithTableNameGrpcRequest request)
        {
            var dbTable = ServiceLocator.DbTablesList.TryGetTable(request.TableName);

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
                    ServiceLocator.SyncEventsQueue.EnqueueDbRowChange(dbTable, request.DbRow);
                });

            ServiceLocator.SyncEventsPusher.PushEventsToReaders();
            return new ValueTask<GrpcResponse>(result);
        }

        public ValueTask<GrpcResponse> BulkInsertOrReplaceAsync(RowsWithTableNameGrpcRequest dbRows)
        {
            var result = new GrpcResponse();

            var dbTable =  ServiceLocator.DbTablesList.TryGetTable(dbRows.TableName);

            if (dbTable == null)
            {
                result.Status = GrpcResultStatus.TableNotFound;
                return new ValueTask<GrpcResponse>(result);
            }

            var groupByPartitionKey = dbRows.DbRows
                .GroupBy(itm => itm.PartitionKey)
                .ToDictionary(
                    itm => itm.Key, 
                    itm => itm.ToList());
            
            dbTable.LockWithWriteAccess(writeAccess =>
            {
                foreach (var (partitionKey, dbRowsByPartition) in groupByPartitionKey)
                {
                    var partition = writeAccess.GetOrCreatePartition(partitionKey);
                    
                    partition.InsertOrReplace(dbRowsByPartition);
                    
                    ServiceLocator.SyncEventsQueue.EnqueueDbRowsChange(dbTable, partitionKey, dbRowsByPartition);
                }
                
            });

            ServiceLocator.SyncEventsPusher.PushEventsToReaders();
            
            result.Status = GrpcResultStatus.Ok;
            return new ValueTask<GrpcResponse>(result);
        }

        public ValueTask<GrpcResponseDbRow> UpdateAsync(RowWithTableNameGrpcRequest request)
        {
            var result = new GrpcResponseDbRow();
            var table = ServiceLocator.DbTablesList.TryGetTable(request.TableName);

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
                ServiceLocator.SyncEventsQueue.EnqueueDbRowChange(table, request.DbRow);

            });

            if (result.Status == GrpcResultStatus.Ok)
                ServiceLocator.SyncEventsPusher.PushEventsToReaders();

            return new ValueTask<GrpcResponseDbRow>(result);

        }

        public async ValueTask DeleteAsync(IAsyncEnumerable<DeleteEntityGrpcContract> request)
        {
            var groupsByTable = await request.GroupToDictionaryAsync(itm => itm.TableName);

            foreach (var groupByTable in groupsByTable)
            {
                var table = ServiceLocator.DbTablesList.TryGetTable(groupByTable.Key);
                
                if (table == null)
                    continue;


                foreach (var partitionGroup in groupByTable.Value.GroupBy(itm => itm.PartitionKey))
                {
                    table.LockWithWriteAccess(writeAccess =>
                    {
                        var partition = writeAccess.TryGetPartition(partitionGroup.Key);
                        
                        if (partition == null)
                            return;

                        foreach (var entityToDelete in partitionGroup)
                        {
                            var deletedRow = partition.TryDelete(entityToDelete.RowKey);
                            if (deletedRow != null)
                                ServiceLocator.SyncEventsQueue.EnqueueDbRowDelete(table, deletedRow);
                        }

                    });  
                }
                
                ServiceLocator.SyncEventsPusher.PushEventsToReaders();
            }
            
        }

        public IAsyncEnumerable<DbRowGrpcModel> GetAsync(GetDbRowsGrpcRequest request)
        {
            var table = ServiceLocator.DbTablesList.TryGetTable(request.TableName);

            if (table == null)
                return AsyncEnumerableResult<DbRowGrpcModel>.Empty();

            
            if (request.RowKey == null)
            {
                var result = table.Get(request.PartitionKey);
                return new AsyncEnumerableResult<DbRowGrpcModel>(result);
            }
                

            var dbRow = table.Get(request.PartitionKey, request.RowKey);

            return dbRow != null 
                ? new AsyncEnumerableResult<DbRowGrpcModel>(new[] {dbRow}) 
                : AsyncEnumerableResult<DbRowGrpcModel>.Empty();
        }

        public ValueTask<GrpcResponse> GcPartitionAsync(GcPartitionGrpcRequest request)
        {
            var result = new GrpcResponse();
            var table = ServiceLocator.DbTablesList.TryGetTable(request.TableName);

            if (table == null)
            {
                result.Status = GrpcResultStatus.TableNotFound;
                return new ValueTask<GrpcResponse>(result);
            }


            result.Status = GrpcResultStatus.Ok;
            var rowsAmount = table.GetAmount(request.PartitionKey);

            if (rowsAmount <= request.MaxRowsAmount)
                return new ValueTask<GrpcResponse>(result);
            
            
            table.LockWithWriteAccess(writeAccess =>
            {

                var partition = writeAccess.TryGetPartition(request.PartitionKey);
                if (partition == null)
                    return;

                foreach (var dbRow in partition.Gc(request.MaxRowsAmount))
                    ServiceLocator.SyncEventsQueue.EnqueueDbRowDelete(table, dbRow);
            });
            
            
            ServiceLocator.SyncEventsPusher.PushEventsToReaders();

            return new ValueTask<GrpcResponse>(result);
        }

        public ValueTask<GrpcResponse> GcTableAsync(GcTableGrpcRequest request)
        {
            var result = new GrpcResponse();
            var table = ServiceLocator.DbTablesList.TryGetTable(request.TableName);

            if (table == null)
            {
                result.Status = GrpcResultStatus.TableNotFound;
                return new ValueTask<GrpcResponse>(result);
            }


            if (!table.WeHavePartitionsToGc(request.MaxPartitionsAmount))
                return new ValueTask<GrpcResponse>(result);
            
            table.LockWithWriteAccess(writeAccess =>
            {
                if (writeAccess.PartitionsCount() > request.MaxPartitionsAmount)
                    return;
                
                var itemsByLastAccess = writeAccess.GetPartitions().OrderBy(itm => itm.LastAccessTime).ToList();

                var i = 0;
    
                while (writeAccess.PartitionsCount() > request.MaxPartitionsAmount)
                {
                    writeAccess.RemovePartition(itemsByLastAccess[i].PartitionKey);
                    ServiceLocator.SyncEventsQueue.EnqueueSyncPartition(table, itemsByLastAccess[i]);
                    i++;
                }


            });
            
            ServiceLocator.SyncEventsPusher.PushEventsToReaders();

            return new ValueTask<GrpcResponse>();
        }
        
    }
}