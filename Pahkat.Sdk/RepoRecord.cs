using Newtonsoft.Json;
using System;


namespace Pahkat.Sdk
{
    public class RepoRecord
    {
        [JsonProperty("url")]
        public Uri Url { get; set; }
        [JsonProperty("channel")]
        public RepositoryMeta.Channel Channel { get; set; }

        public RepoRecord(Uri url, RepositoryMeta.Channel channel)
        {
            Url = url;
            Channel = channel;
        }

    }
}

