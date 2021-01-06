using System;
using MyNoSqlGrpc.Engine;
using MyYamlSettingsParser;

namespace MyNoSqlGrpc.Server
{
    public class SettingsModel : IMyNoSqlGrpcEngineSettings
    {
     
        [YamlProperty]
        public int MaxPayloadSize { get; set; }
        
        
        [YamlProperty]
        public string SessionExpiration { get; set; }

        TimeSpan IMyNoSqlGrpcEngineSettings.SessionExpiration => TimeSpan.Parse(SessionExpiration);

    }
}