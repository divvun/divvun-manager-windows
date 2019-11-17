using Newtonsoft.Json;
using Pahkat.Sdk.Native;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pahkat.Sdk
{
    public class StoreConfig : Boxed
    {
        internal StoreConfig(IntPtr handle) : base(handle) { }

        public void SetUiValue(string key, string rawValue)
        {
            pahkat_client.pahkat_store_config_set_ui_value(this, key, rawValue, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
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
            var value = pahkat_client.pahkat_store_config_ui_value(this, key, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
            return value;
        }

        public string GetUiValue(string key)
        {
            return GetUiValue<string>(key);
        }

        public T GetUiValue<T>(string key)
        {
            var value = pahkat_client.pahkat_store_config_ui_value(this, key, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
            if (value == null)
            {
                return default;
            }
            return JsonConvert.DeserializeObject<T>(value);
        }

        public void SetRepos(List<RepoRecord> records)
        {
            pahkat_client.pahkat_store_config_set_repos(this, records.ToArray(), PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
        }

        public List<RepoRecord> Repos()
        {
            var result = pahkat_client.pahkat_store_config_repos(this, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
            return result.ToList();
        }

        public void AddSkippedVersion(PackageKey key, string version)
        {
            pahkat_client.pahkat_store_config_add_skipped_package(this, key, version, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
        }

        public string SkippedVersion(PackageKey key)
        {
            var result = pahkat_client.pahkat_store_config_skipped_package(this, key, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
            return result;
        }
    }
}
