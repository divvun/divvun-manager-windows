using Newtonsoft.Json;
using Pahkat.Sdk.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using static Pahkat.Sdk.PahkatClientException;

namespace Pahkat.Sdk
{
    public class StoreConfig : Boxed
    {
        internal StoreConfig(IntPtr handle) : base(handle) { }

        public void SetUiValue(string key, string rawValue)
        {
            pahkat_client.pahkat_store_config_set_ui_value(this, key, rawValue, out var exception);
            Try(exception);
        }

        public void SetUiValue<T>(string key, T rawValue)
        {
            var value = JsonConvert.SerializeObject(rawValue);
            SetUiValue(key, value);
        }
        
        /// <summary>
        /// Values tend to be JSON-encoded strings meaning they are double quoted; this ignores the parsing.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetUiValueRaw(string key)
        {
            var value = pahkat_client.pahkat_store_config_ui_value(this, key, out var exception);
            Try(exception);
            return value;
        }

        public string GetUiValue(string key)
        {
            return GetUiValue<string>(key);
        }

        public T GetUiValue<T>(string key)
        {
            var value = pahkat_client.pahkat_store_config_ui_value(this, key, out var exception);
            Try(exception);
            if (value == null)
            {
                return default;
            }
            return JsonConvert.DeserializeObject<T>(value);
        }

        public void SetRepos(List<RepoRecord> records)
        {
            pahkat_client.pahkat_store_config_set_repos(this, records.ToArray(), out var exception);
            Try(exception);
        }

        public List<RepoRecord> Repos()
        {
            var result = pahkat_client.pahkat_store_config_repos(this, out var exception);
            Try(exception);
            return result.ToList();
        }

        public void AddSkippedVersion(AbsolutePackageKey key, string version)
        {
            pahkat_client.pahkat_store_config_add_skipped_version(this, key, version, out var exception);
            Try(exception);
        }

        public string SkippedVersion(AbsolutePackageKey key)
        {
            var result = pahkat_client.pahkat_store_config_skipped_version(this, key, out var exception);
            Try(exception);
            return result;
        }
    }
}
