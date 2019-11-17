using Newtonsoft.Json;
using Pahkat.Sdk.Native;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pahkat.Sdk
{
    internal class PackageKeyJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var str = (string)reader.Value;
            return PackageKey.New(str);
        }
            
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }

    [JsonConverter(typeof(PackageKeyJsonConverter))]
    public class PackageKey
    {
        public readonly string BaseUrl;
        public readonly string Id;
        public readonly string Channel;

        public override string ToString()
        {
            return $"{BaseUrl}packages/{Id}#{Channel}";
        }

        public static PackageKey New(Uri url)
        {
            var pathChunks = new Stack<string>(url.AbsolutePath.Split('/'));
            var id = pathChunks.Pop();

            // Pop packages off
            var key = pathChunks.Pop();
            if (key != "packages")
            {
                throw new ArgumentException($"Provided URI does not contain packages path: {url}");
            }

            var channel = url.Fragment.Substring(1);

            var builder = new UriBuilder(url);
            builder.Path = string.Join("/", pathChunks.Reverse()) + "/";
            builder.Fragment = string.Empty;
            builder.Query = string.Empty;

            return new PackageKey(builder.Uri.ToString(), id, channel);
        }

        public static PackageKey FromPtr(IntPtr ptr)
        {
            var urlString = MarshalUtf8.PtrToStringUtf8(ptr);
            return New(urlString);
        }
        
        internal static PackageKey New(string url)
        {
            return New(new Uri(url));
        }

        private PackageKey(string uri, string id, string channel)
        {
            BaseUrl = uri;
            Id = id;
            Channel = channel;
        }

        protected bool Equals(PackageKey other)
        {
            return string.Equals(BaseUrl, other.BaseUrl) && 
                string.Equals(Id, other.Id) && 
                string.Equals(Channel, other.Channel);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PackageKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (BaseUrl != null ? BaseUrl.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Channel != null ? Channel.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
