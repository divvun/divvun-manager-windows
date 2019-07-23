using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Pahkat.Sdk
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PackageAction
    {
        [EnumMember(Value = "install")]
        Install,
        [EnumMember(Value = "uninstall")]
        Uninstall
    }
}

