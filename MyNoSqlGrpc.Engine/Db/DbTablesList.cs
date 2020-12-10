using System.Collections.Generic;

namespace MyNoSqlGrpc.Engine.Db
{
    public class DbTablesList
    {
        private Dictionary<string, DbTable> _tables = new ();


        private readonly object _lockObject = new();


        public (bool created, DbTable table) CreateIfNotExists(string tableName)
        {

            lock (_lockObject)
            {
                if (_tables.TryGetValue(tableName, out var tbl))
                    return (false, tbl);

                var result = new DbTable(tableName);

                var newTables = new Dictionary<string, DbTable>(_tables) {{tableName, result}};

                _tables = newTables;
                return (true, result);
            }
        }

        public DbTable TryGetTable(string tableName)
        {
            var tables = _tables;

            return tables.TryGetValue(tableName, out var result) 
                ? result 
                : default;
        }
    }
}