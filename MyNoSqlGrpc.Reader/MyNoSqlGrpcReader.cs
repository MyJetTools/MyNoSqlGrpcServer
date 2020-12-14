using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncAwaitUtils;
using MyNoSqlGrpc.Reader.Cache;
using MyNoSqlGrpc.Reader.GrpcConnection;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Reader
{
    public class MyNoSqlGrpcReader<T>
    {
        private readonly MyNoSqlGrpcReaderConnection _connection;
        private readonly string _tableName;

        private readonly ReaderTable<T> _dbTable;

        public MyNoSqlGrpcReader(MyNoSqlGrpcReaderConnection connection, string tableName)
        {
            _connection = connection;
            _tableName = tableName;
            _dbTable = new ReaderTable<T>();

            connection.SubscribeToUpdateEvent(_tableName, HandleUpdateEvent);
        }

        private Func<ReadOnlyMemory<byte>, T> _deserializer;

        public MyNoSqlGrpcReader<T> PlugDeserializer(
            Func<ReadOnlyMemory<byte>, T> deserializer)
        {
            _deserializer = deserializer;
            return this;
        }

        private readonly List<Action<RowOperationResult, T>> _onUpdateCallbacks =
            new ();

        public MyNoSqlGrpcReader<T> SubscribeOnUpdateEvent(Action<RowOperationResult, T> onUpdateCallback)
        {
            _onUpdateCallbacks.Add(onUpdateCallback);
            return this;
        }

        private void InvokeEvent(RowOperationResult operationResult, T entity)
        {
            foreach (var updateCallback in _onUpdateCallbacks)
                updateCallback(operationResult, entity);
        }

        public ValueTask<T> GetAsync(string partitionKey, string rowKey)
        {
            var partitionFound = false;
            
            var result = _dbTable.LockWithReadAccess(access =>
            {
                var partition = access.TryGetPartition(partitionKey);

                if (partition == null)
                    return default;

                partitionFound = true;
                return partition.TryGet(rowKey);

            });

            return partitionFound 
                ? new ValueTask<T>(result.PayLoad) 
                : new ValueTask<T>(InitAndGetAsync(partitionKey, rowKey));
        }
        
        public ValueTask<IEnumerable<T>> GetAsync(string partitionKey)
        {
            var partitionFound = false;
            
            var result = _dbTable.LockWithReadAccess(access =>
            {
                var partition = access.TryGetPartition(partitionKey);

                if (partition == null)
                    return Array.Empty<ReaderRow<T>>();

                partitionFound = true;
                return partition.Get();

            });

            return partitionFound 
                ? new ValueTask<IEnumerable<T>>(result.Select(itm => itm.PayLoad)) 
                : new ValueTask<IEnumerable<T>>(InitAndGetAsync(partitionKey));
        }
        
        private async Task<IEnumerable<T>> InitAndGetAsync(string partitionKey)
        {
            var result = await InitFromServerAsync(partitionKey);
            return result.Values.Select(itm => itm.PayLoad);
        }

        private async Task<T> InitAndGetAsync(string partitionKey, string rowKey)
        {
            try
            {
                var partition = await InitFromServerAsync(partitionKey);
                return partition.TryGetValue(rowKey, out var result) ? result.PayLoad : default;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
        
        private async Task<IReadOnlyDictionary<string, ReaderRow<T>>> InitFromServerAsync(string partitionKey)
        {
            await _connection.AwaitUntilConnected();
            var dbRows = await _connection.SubscribeAsync(_tableName, partitionKey).ToListAsync();
            return ResetPartition(partitionKey, dbRows);
        }
        
        private void HandleUpdateEvent(IGrpcConnectionUpdateCommand updateEventData)
        {
            switch (updateEventData)
            {
                case ClearTableUpdateResult:
                    ClearTable();
                    break;
                case ResetPartitionUpdateCommand resetPartitionUpdateCommand:
                    ResetPartition(resetPartitionUpdateCommand.PartitionKey, resetPartitionUpdateCommand.DbRows);
                    break;
                case UpdateRowsCommand updateRowsCommand:
                    UpdateDbRows(updateRowsCommand.RowsToUpdate);
                    break;
                case DeleteRowsCommand deleteRowsCommand:
                    DeleteRows(deleteRowsCommand);
                    break;
                    
            }
        }

        private void ClearTable()
        {
            _dbTable.LockWithWriteAccess(writeAccess =>
            {
                var clearedPartitions = writeAccess.Clear();

                foreach (var clearedPartition in clearedPartitions.Values)
                {
                    foreach (var entity in clearedPartition.Get())
                        InvokeEvent(RowOperationResult.Delete, entity.PayLoad);
                }
            });
        }
        
        private IReadOnlyDictionary<string, ReaderRow<T>> ResetPartition(string partitionKey, IReadOnlyList<DbRowGrpcModel> dbRows)
        {

            var entities = dbRows
                .Select(itm => new ReaderRow<T>(itm, _deserializer(itm.Content)))
                .ToDictionary(itm => itm.RowKey);
            
            _dbTable.LockWithWriteAccess(writeAccess =>
            {
                var removedPartition = writeAccess.ClearPartition(partitionKey);

                var newPartition = writeAccess.GetOrCreatePartition(partitionKey);

                newPartition.Init(entities.Values);

                foreach (var (result, entity) in removedPartition.FindTheDifference(newPartition))
                    InvokeEvent(result, entity);
            });

            return entities;
        }

        private void UpdateDbRows(IReadOnlyList<DbRowGrpcModel> rowsToUpdate)
        {

            var dbRowsByPartition = rowsToUpdate.GroupBy(itm => itm.PartitionKey)
                .ToDictionary(itm => itm.Key,
                    itm =>  itm.Select(r => new ReaderRow<T>(r, _deserializer(r.Content))).ToList());
            
            _dbTable.LockWithWriteAccess(writeAccess =>
            {

                foreach (var (partitionKey, dbRows) in dbRowsByPartition)
                {
                    var partition = writeAccess.GetOrCreatePartition(partitionKey);

                    foreach (var dbRow in dbRows)
                    {
                        var result = partition.InsertOrReplace(dbRow);
                        InvokeEvent(result, dbRow.PayLoad);
                    }
                }

            });
        }

        private void DeleteRows(DeleteRowsCommand deleteRowsCommand)
        {
            _dbTable.LockWithWriteAccess(writeAccess =>
            {

                foreach (var rowKey in deleteRowsCommand.RowKeys)
                {
                    var partition = writeAccess.GetOrCreatePartition(deleteRowsCommand.PartitionKey);

                    var dbRow = partition.DeleteRow(rowKey);
                    if (dbRow != null)
                        InvokeEvent(RowOperationResult.Delete, dbRow.PayLoad);
                }

            });
        }
        
        
    }
}