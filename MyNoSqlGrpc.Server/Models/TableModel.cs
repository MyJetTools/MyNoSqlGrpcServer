using MyNoSqlGrpc.Engine.Db;

namespace MyNoSqlGrpc.Server.Models
{
    public class TableModel
    {
        public string Name { get; set; }
        
        public int PartitionsCount { get; set; }


        public static TableModel Create(DbTable table)
        {
            return new TableModel
            {
                Name = table.Id,
                PartitionsCount = table.GetPartitionsCount()
            };
        }
    }
}