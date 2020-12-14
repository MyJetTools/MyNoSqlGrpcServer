using System;
using System.Collections.Generic;
using System.Linq;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Reader.Cache
{
    
    
    public class ReaderPartition<T>
    {

        private readonly SortedDictionary<string, ReaderRow<T>> _items = new ();

        private IReadOnlyList<ReaderRow<T>> _list = Array.Empty<ReaderRow<T>>();

        public int Count => _items.Count;

        public ReaderRow<T> TryGet(string rowKey)
        {
            return _items.TryGetValue(rowKey, out var result) ? result : default;
        }

        public IReadOnlyList<ReaderRow<T>> Get()
        {
            if (_list != null)
                return _list;

            _list = _items.Values.ToList();
            return _list;
        }


        public void Init(IEnumerable<ReaderRow<T>> entities)
        {
            _list = null;
            foreach (var entity in entities)
                _items.Add(entity.RowKey, entity);
        }

        public RowOperationResult InsertOrReplace(ReaderRow<T> entity)
        {
            _list = null;
            
            if (_items.ContainsKey(entity.RowKey))
            {
                _items[entity.RowKey] = entity;
                return RowOperationResult.Update;
            }
            
            _items.Add(entity.RowKey, entity);
            return RowOperationResult.Insert;
            
        }

        public ReaderRow<T> DeleteRow(string rowKey)
        {
            return _items.Remove(rowKey, out var result) ? result : null;
        }
    }
}