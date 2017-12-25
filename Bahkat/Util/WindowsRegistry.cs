using System;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Bahkat.Util
{
    public interface IWindowsRegistry
    {
        IWindowsRegKey LocalMachine { get; }

        IWindowsRegKey OpenBaseKey(RegistryHive key, RegistryView view);
        T Get<T>(string keyName, string valueName) where T : class;
        T Get<T>(string keyName, string valueName, T defaultValue) where T : class;
        T Get<T>(string keyName, string valueName, Func<object, T> handler);
        void Set<T>(string keyName, string valueName, T value, RegistryValueKind kind) where T : class;
    }
    
    public interface IWindowsRegKey
    {
        T Get<T>(string valueName) where T : class;
        T Get<T>(string valueName, T defaultValue) where T : class;
        T Get<T>(string valueName, Func<object, T> handler);
        void Set<T>(string valueName, T value, RegistryValueKind kind) where T : class;
        
        IWindowsRegKey CreateSubKey(string subkey);
        IWindowsRegKey OpenSubKey(string subkey, bool isWritable = false);
    }

    public class WindowsRegKey : IWindowsRegKey
    {
        private readonly RegistryKey _rk;
        
        public WindowsRegKey(RegistryKey regKey)
        {
            _rk = regKey ?? throw new ArgumentNullException(nameof(regKey));
        }

        public T Get<T>(string valueName) where T : class
        {
            return _rk.GetValue(valueName) as T;
        }

        public T Get<T>(string valueName, T defaultValue) where T : class
        {
            //Console.WriteLine("{0}, {1}, {2}", valueName, defaultValue, _rk == null);
            return _rk.GetValue(valueName, defaultValue) as T;
        }

        public T Get<T>(string valueName, Func<object, T> handler)
        {
            return handler(_rk.GetValue(valueName, null));
        }

        public void Set<T>(string valueName, T value, RegistryValueKind kind) where T : class
        {
            _rk.SetValue(valueName, value, kind);
        }

        public IWindowsRegKey CreateSubKey(string subkey)
        {
            return new WindowsRegKey(_rk.CreateSubKey(subkey));
        }

        public IWindowsRegKey OpenSubKey(string subkey, bool isWriteable = false)
        {
            var srk = _rk.OpenSubKey(subkey, isWriteable);
            return srk == null ? null : new WindowsRegKey(srk);
        }
    }
    
    public class WindowsRegistry : IWindowsRegistry
    {
        public IWindowsRegKey LocalMachine => new WindowsRegKey(Registry.LocalMachine);

        public IWindowsRegKey OpenBaseKey(RegistryHive key, RegistryView view)
        {
            var bk = RegistryKey.OpenBaseKey(key, view);
            return bk != null ? new WindowsRegKey(bk) : null;
        }

        public T Get<T>(string keyName, string valueName) where T : class
        {
            return Registry.GetValue(keyName, valueName, null) as T;
        }

        public T Get<T>(string keyName, string valueName, T defaultValue) where T : class
        {
            var foo = Registry.GetValue(keyName, valueName, defaultValue) as T;

            return foo ?? defaultValue;
        }

        public T Get<T>(string keyName, string valueName, Func<object, T> handler)
        {
            return handler(Registry.GetValue(keyName, valueName, null));
        }

        public void Set<T>(string keyName, string valueName, T value, RegistryValueKind kind) where T : class
        {
            Registry.SetValue(keyName, valueName, value, kind);
        }
    }

    public class MockRegKey : IWindowsRegKey
    {
        private readonly MockRegistry _registry;
        private readonly string _name;

        public MockRegKey(MockRegistry registry, string name)
        {
            _registry = registry;
            _name = name;
        }

        public T Get<T>(string valueName) where T : class
        {
            return _registry.Get<T>(_name, valueName);
        }

        public T Get<T>(string valueName, T defaultValue) where T : class
        {
            return _registry.Get(_name, valueName, defaultValue);
        }

        public T Get<T>(string valueName, Func<object, T> handler)
        {
            return _registry.Get(_name, valueName, handler);
        }

        public void Set<T>(string valueName, T value, RegistryValueKind kind) where T : class
        {
            _registry.Set(_name, valueName, value, kind);
        }

        public IWindowsRegKey CreateSubKey(string subkey)
        {
            var key = _name + "\\" + subkey;
            return new MockRegKey(_registry, key);
        }

        public IWindowsRegKey OpenSubKey(string subkey, bool isWritable = false)
        {
            var key = _name + "\\" + subkey;
            
            if (!_registry.Store.ContainsKey(key))
            {
                return null;
            }
            
            return new MockRegKey(_registry, key);
        }
    }

    public class MockRegistry : IWindowsRegistry
    {
        private string ConvertHiveToString(RegistryHive hive)
        {
            switch (hive)
            {
                case RegistryHive.LocalMachine:
                    return "HKEY_LOCAL_MACHINE";
            }

            throw new Exception("No key found for " + hive);
        }
        
        internal readonly Dictionary<string, Dictionary<string, object>> Store =
            new Dictionary<string, Dictionary<string, object>>();

        public IWindowsRegKey LocalMachine { get; }
        
        public MockRegistry()
        {
            LocalMachine = new MockRegKey(this, Registry.LocalMachine.Name);
        }

        public IWindowsRegKey OpenBaseKey(RegistryHive key, RegistryView view)
        {
            // TODO implement view!!
            return new MockRegKey(this, ConvertHiveToString(key));
        }

        public T Get<T>(string keyName, string valueName) where T : class
        {
            if (!Store.ContainsKey(keyName))
            {
                return null;
            }
            
            if (Store[keyName].ContainsKey(valueName))
            {
                return Store[keyName][valueName] as T;
            }

            return null;
        }

        public T Get<T>(string keyName, string valueName, T defaultValue) where T : class
        {
            return Get<T>(keyName, valueName) ?? defaultValue;
        }

        public T Get<T>(string keyName, string valueName, Func<object, T> handler)
        {
            return handler(!Store.ContainsKey(keyName) ? null : Store[keyName]);
        }

        public void Set<T>(string keyName, string valueName, T value, RegistryValueKind kind) where T : class
        {
            if (!Store.ContainsKey(keyName))
            {
                Store[keyName] = new Dictionary<string, object>();
            }

            Console.WriteLine("REG: {0}:{1} -> {2}", keyName, valueName, value);

            Store[keyName][valueName] = value;
        }
    }
}