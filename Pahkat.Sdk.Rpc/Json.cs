using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Pahkat.Sdk.Rpc
{
    public static class Json
    {
        public static Lazy<JsonSerializerSettings> Settings = new Lazy<JsonSerializerSettings>(() => {
            
            var settings = new JsonSerializerSettings {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                },
                Formatting = Formatting.Indented
            };
            
            settings.Converters.Add(TransactionResponseValue.JsonConvertor());

            return settings;
        });
    }
}