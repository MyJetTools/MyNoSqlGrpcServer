using System.Runtime.Serialization;

namespace MyNoSqlGrpcServer.GrpcContracts
{
    [DataContract]
    public class CreateTableGrpcRequest
    {
        [DataMember(Order = 1)]
        public string TableName { get; set; }
    }

    public enum GrpcResultStatus
    {
        Ok, RecordChanged, NotFound, TableNotFound, RecordAlreadyExists
    }

    [DataContract]
    public class GrpcResponseDbRow
    {
        
        [DataMember(Order = 1)]
        public GrpcResultStatus Status { get; set; }
        
        [DataMember(Order = 2)]
        public DbRowGrpcModel DbRow { get; set; }
    }
    
    [DataContract]
    public class GrpcResponse
    {
        [DataMember(Order = 1)]
        public GrpcResultStatus Status { get; set; }
    }
    
    [DataContract]
    public class GetDbRowsGrpcRequest
    {
        [DataMember(Order = 1)]
        public string TableName { get; set; }
        
        [DataMember(Order = 2)]
        public string PartitionKey { get; set; }

        [DataMember(Order = 3)]
        public string RowKey { get; set; }
        
        [DataMember(Order = 4)]
        public int Skip { get; set; }
        
        [DataMember(Order = 5)]
        public int Limit { get; set; }

    }
    


    [DataContract]
    public class GcPartitionGrpcRequest
    {
        [DataMember(Order = 1)]
        public string TableName { get; set; }
        
        [DataMember(Order = 2)]
        public string PartitionKey { get; set; }
        
        [DataMember(Order = 3)]
        public int MaxRowsAmount { get; set; }
    }
    
    [DataContract]
    public class GcTableGrpcRequest
    {
        [DataMember(Order = 1)]
        public string TableName { get; set; }
        
        [DataMember(Order = 2)]
        public int MaxPartitionsAmount { get; set; }
    }
    
    [DataContract]
    public class RowWithTableNameGrpcRequest
    {
        [DataMember(Order = 1)]
        public string TableName { get; set; }
        
        [DataMember(Order = 2)]
        public DbRowGrpcModel DbRow { get; set; }

    }
    
    [DataContract]
    public class RowsWithTableNameGrpcRequest
    {
        [DataMember(Order = 1)]
        public string TableName { get; set; }
        
        [DataMember(Order = 2)]
        public DbRowGrpcModel[] DbRows { get; set; }

    }
    
    
}