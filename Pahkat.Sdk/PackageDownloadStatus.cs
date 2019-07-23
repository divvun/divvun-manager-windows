using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Pahkat.Sdk
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PackageDownloadStatus
    {
        [EnumMember(Value = "notStarted")]
        NotStarted,
        [EnumMember(Value = "starting")]
        Starting,
        [EnumMember(Value = "progress")]
        Progress,
        [EnumMember(Value = "completed")]
        Completed,
        [EnumMember(Value = "error")]
        Error
    }
}

