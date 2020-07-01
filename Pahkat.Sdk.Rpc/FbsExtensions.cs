using System;
using System.Collections;
using System.Collections.Generic;
using Iterable;

namespace Pahkat.Sdk.Rpc.Fbs
{
    public class RefMap<K, V> : IReadOnlyDictionary<K, V> where K: IEquatable<K>
    {
        public IEnumerator<KeyValuePair<K, V>> GetEnumerator() {
            for (var i = 0; i < Count; ++i) {
                var pair = new KeyValuePair<K, V>(_keyGetter.Invoke(i), _valueGetter.Invoke(i));
                yield return pair;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public int Count { get; }
        private readonly Func<int, V> _valueGetter;
        private readonly Func<int, K> _keyGetter;

        private int IndexOf(K key) {
            for (var i = 0; i < Count; ++i) {
                if (key.Equals(_keyGetter.Invoke(i))) {
                    return i;
                }
            }

            return -1;
        }
        
        public bool ContainsKey(K key) {
            return IndexOf(key) != -1;
        }

        public bool TryGetValue(K key, out V value) {
            var index = IndexOf(key);
            if (index == -1) {
                value = default;
                return false;
            }

            value = _valueGetter.Invoke(index);
            return true;
        }

        public V this[K key] {
            get {
                var index = IndexOf(key);
                if (index == -1) {
                    return default;
                }

                return _valueGetter.Invoke(index);
            }
        }

        public IEnumerable<K> Keys {
            get {
                for (var i = 0; i < Count; ++i) {
                    yield return _keyGetter.Invoke(i);
                }
            }
        }
        
        public IEnumerable<V> Values {
            get {
                for (var i = 0; i < Count; ++i) {
                    yield return _valueGetter.Invoke(i);
                }
            }
        }
        
        internal RefMap(int count, Func<int, V> valueGetter, Func<int, K> keyGetter) {
            Count = count;
            _valueGetter = valueGetter;
            _keyGetter = keyGetter;
        }
    }

    public class RefList<V> : IReadOnlyList<V>
    {
        public IEnumerator<V> GetEnumerator() {
            for (var i = 0; i < Count; ++i) {
                yield return _valueGetter.Invoke(i);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public int Count { get; }
        private readonly Func<int, V> _valueGetter;

        public V this[int index] {
            get {
                if (index >= Count || index < 0) {
                    throw new IndexOutOfRangeException();
                }
                
                return _valueGetter.Invoke(index);
            }
        }

        internal RefList(int count, Func<int, V> valueGetter) {
            Count = count;
            _valueGetter = valueGetter;
        }
    }
    
    public static class FbsExtensions
    {
        public static RefMap<string, Descriptor?> Packages(this Packages packages) {
            return new RefMap<string, Descriptor?>(
                packages.PackagesValuesLength,
                packages.PackagesValues,
                packages.PackagesKeys);
        }

        public static RefMap<string, string> Name(this Descriptor descriptor) {
            return new RefMap<string, string>(
                descriptor.NameValuesLength, 
                descriptor.NameValues, 
                descriptor.NameKeys);
        }
        
        public static RefMap<string, string> Description(this Descriptor descriptor) {
            return new RefMap<string, string>(
                descriptor.DescriptionValuesLength, 
                descriptor.DescriptionValues, 
                descriptor.DescriptionKeys);
        }

        public static RefList<string> Tags(this Descriptor descriptor) {
            return new RefList<string>(descriptor.TagsLength, descriptor.Tags);
        }

        public static RefList<Release?> Release(this Descriptor descriptor) {
            return new RefList<Release?>(descriptor.ReleaseLength, descriptor.Release);
        }

        public static RefList<string> Authors(this Release release) {
            return new RefList<string>(release.AuthorsLength, release.Authors);
        }

        public static RefList<Target?> Target(this Release release) {
            return new RefList<Target?>(release.TargetLength, release.Target);
        }

        public static Target? WindowsTarget(this Release release) {
            return release.Target().First(x => x.HasValue && x.Value.Platform == "windows");
        }

        public static RefMap<string, string> Dependencies(this Target target) {
            return new RefMap<string, string>(
                target.DependenciesValuesLength,
                target.DependenciesValues,
                target.DependenciesKeys);
        }

        public static WindowsExecutable? WindowsExecutable(this Target target) {
            if (target.PayloadType == Payload.WindowsExecutable) {
                return target.Payload<WindowsExecutable>();
            }

            return null;
        }
    }
}