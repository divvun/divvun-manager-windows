using System;
using System.Linq;
using Flurl;
using Newtonsoft.Json;

namespace Pahkat.Sdk
{
    public class PackageKeyConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
            if (value == null) {
                writer.WriteNull();
                return;
            }

            var key = (PackageKey) value;
            writer.WriteValue(key.ToString());
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
            string? stringUrl = (string?)reader.Value;
            
            if (stringUrl != null) {
                return PackageKey.From(stringUrl);
            }

            return null;
        }

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(PackageKey);
        }
    }
    
    public struct PackageKeyParams
    {
        public string? Channel { get; internal set; }
        public string? Arch { get; internal set; }
        public string? Platform { get; internal set; }
        public string? Version { get; internal set; }
    }
    
    [JsonConverter(typeof(PackageKeyConverter))]
    public struct PackageKey : IEquatable<PackageKey>
    {
        public Uri RepositoryUrl { get; private set; }
        public string Id { get; private set; }
        public PackageKeyParams? Params { get; private set; }

        public static PackageKey Create(Uri repoUri, string id, PackageKeyParams? pkgParams = null) {
            return new PackageKey {
                Id = id,
                RepositoryUrl = repoUri,
                Params = pkgParams
            };
        } 
        
        public static PackageKey From(string url) => PackageKey.From(new Uri(url));

        public static PackageKey From(Uri uri) {
            Url url = new Url(uri);
            
            var packageSegmentIndex = url.PathSegments.IndexOf("packages");

            if (packageSegmentIndex == -1) {
                throw new Exception("Invalid PackageKey URI, no packages segment");
            }
            
            if (packageSegmentIndex != url.PathSegments.Count - 2) {
                throw new Exception("Invalid PackageKey URI, invalid identifier segment");
            }

            PackageKeyParams? pkParams = null;

            if (url.QueryParams.Count > 0) {
                var p = new PackageKeyParams();
                
                foreach (var pair in url.QueryParams) {
                    switch (pair.Name)
                    {
                    case "channel":
                        p.Channel = pair.Value.ToString();
                        break;
                    case "arch":
                        p.Arch = pair.Value.ToString();
                        break;
                    case "platform":
                        p.Platform = pair.Value.ToString();
                        break;
                    case "version":
                        p.Version = pair.Value.ToString();
                        break;
                    }
                }

                pkParams = p;
            }

            var id = url.PathSegments.Last();
            var repoUrl = url
                .RemovePathSegment()
                .RemovePathSegment()
                .RemoveFragment()
                .RemoveQuery()
                .AppendPathSegment("")
                .ToUri();

            return new PackageKey() {
                RepositoryUrl = repoUrl,
                Id = id,
                Params = pkParams
            };
        }

        public Uri toUri() {
            var url = RepositoryUrl
                .AppendPathSegment("packages")
                .AppendPathSegment(Id);
            
            if (Params.HasValue) {
                url.SetQueryParam("channel", Params.Value.Channel);
                url.SetQueryParam("platform", Params.Value.Platform);
                url.SetQueryParam("arch", Params.Value.Arch);
                url.SetQueryParam("version", Params.Value.Version);
            }

            return url.ToUri();
        }

        public override string ToString() => toUri().ToString();

        public bool Equals(PackageKey other) {
            return Equals(RepositoryUrl, other.RepositoryUrl) && Id == other.Id && Nullable.Equals(Params, other.Params);
        }

        public override bool Equals(object obj) {
            return obj is PackageKey other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = (RepositoryUrl != null ? RepositoryUrl.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Params.GetHashCode();
                return hashCode;
            }
        }
    }
}