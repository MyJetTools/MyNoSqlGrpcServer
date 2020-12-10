using System;
using System.Runtime.Serialization;

namespace MyNoSqlGrpcServer.GrpcContracts
{
    [DataContract]
    public class DbRowGrpcModel
    {
        [DataMember(Order = 1)]
        public string PartitionKey { get; set; }
        
        [DataMember(Order = 2)]
        public string RowKey { get; set; }
        
        [DataMember(Order = 3)]
        public DateTime TimeStamp { get; set; }
        
        [DataMember(Order = 4)]
        public DateTime? Expires { get; set; }
        
        [DataMember(Order = 5)]
        public DateTime LastAccessTime { get; set; }
        
        [DataMember(Order = 6)]
        public byte[] Content { get; set; }
    }
}