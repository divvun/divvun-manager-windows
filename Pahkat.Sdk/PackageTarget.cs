using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Pahkat.Sdk
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PackageTarget
    {
        [EnumMember(Value = "system")]
        System,
        [EnumMember(Value = "user")]
        User
    }
}

