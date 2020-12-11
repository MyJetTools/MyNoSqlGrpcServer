using System;
using System.Collections.Generic;
using System.Linq;

namespace MyNoSqlGrpc.Engine.Db
{
    public class DbTablesList
    {
        private Dictionary<string, DbTable> _tables = new ();

        private IReadOnlyList<DbTable> _tablesAsList = Array.Empty<DbTable>();


        private readonly object _lockObject = new();


        public DbTable CreateIfNotExists(string tableName)
        {

            lock (_lockObject)
            {
                if (_tables.TryGetValue(tableName, out var tbl))
                    return tbl;

                var newTable = new DbTable(tableName);

                var newTables = new Dictionary<string, DbTable>(_tables) {{tableName, newTable}};

                _tables = newTables;

                _tablesAsList = _tables.Values.ToList();
                return newTable;
            }
        }

        public DbTable TryGetTable(string tableName)
        {
            var tables = _tables;

            return tables.TryGetValue(tableName, out var result) 
                ? result 
                : default;
        }


        public IReadOnlyList<DbTable> GetTables()
        {
            return _tablesAsList;
        }
    }
}