using System.Collections.Generic;

namespace Pahkat.Sdk
{
    public static class Extensions
    {
        public static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
            where TValue : new()
        {
            if (!dict.ContainsKey(key))
            {
                dict[key] = new TValue();
            }

            return dict[key];
        }

        public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue fallback)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }

            return fallback;
        }
    }
}
