using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
//using System.Security.Policy;
using System.Threading.Tasks;
using JetBrains.Annotations;
//using Moq.Language.Flow;
//using Pahkat.Extensions;
//using Pahkat.UI.Settings;
//using Quartz.Xml.JobSchedulingData20;

namespace Pahkat.Sdk
{
    public enum InstallerTarget : byte
    {
        System,
        User
    }

    public static class InstallerTargetExtensions
    {
        public static byte ToByte(this InstallerTarget target)
        {
            switch (target)
            {
                case InstallerTarget.System:
                    return 0;
                case InstallerTarget.User:
                    return 1;
                default:
                    return 255;
            }
        }
    }

    public struct TransactionAction
    {
        [JsonProperty("action", Required = Required.Always)]
        public readonly PackageActionType Action;
        [JsonProperty("id", Required = Required.Always)]
        public readonly AbsolutePackageKey Id;
        [JsonProperty("target", Required = Required.Always)]
        public readonly InstallerTarget Target;

        public TransactionAction(PackageActionType action, AbsolutePackageKey packageKey, InstallerTarget target)
        {
            Action = action;
            Id = packageKey;
            Target = target;
        }

        public IntPtr ToCType()
        {
            var cKey = MarshalUtf8.StringToHGlobalUtf8(Id.ToString());
            var o = Native.pahkat_create_action(Action.ToByte(), Target.ToByte(), cKey);
            Marshal.FreeHGlobal(cKey);
            return o;
        }

        public Dictionary<string, string> ToJson()
        {
            var x = new Dictionary<string, string>();
            x["action"] = Action == PackageActionType.Install ? "install" : "uninstall";
            x["id"] = Id.ToString();
            x["target"] = Target == InstallerTarget.System ? "system" : "user";
            return x;
        }

        public static TransactionAction FromJson(Dictionary<string, string> x)
        {
            return new TransactionAction(
                x["action"] == "install" ? PackageActionType.Install : PackageActionType.Uninstall,
                new AbsolutePackageKey(new Uri(x["id"])),
                x["target"] == "system" ? InstallerTarget.System : InstallerTarget.User
            );
        }
    }

    public enum PackageEventType
    {
        NotStarted,
        Uninstalling,
        Installing,
        Completed,
        Error
    }

    public struct PackageEvent
    {
        public readonly AbsolutePackageKey PackageKey;
        public readonly PackageEventType Event;

        private PackageEvent(AbsolutePackageKey key, PackageEventType evt)
        {
            PackageKey = key;
            Event = evt;
        }

        public static PackageEvent FromCode(AbsolutePackageKey key, uint code)
        {
            PackageEventType evt = PackageEventType.Error;
            switch (code)
            {
                case 0:
                    evt = PackageEventType.NotStarted;
                    break;
                case 1:
                    evt = PackageEventType.Uninstalling;
                    break;
                case 2:
                    evt = PackageEventType.Installing;
                    break;
                case 3:
                    evt = PackageEventType.Completed;
                    break;
                case 4:
                    evt = PackageEventType.Error;
                    break;
            }

            return new PackageEvent(key, evt);
        }

        public static PackageEvent Completed(AbsolutePackageKey key) => FromCode(key, 3);
        public static PackageEvent Error(AbsolutePackageKey key) => FromCode(key, 4);
    }

    public interface IPahkatTransaction
    {
        IObservable<PackageEvent> Process();
        TransactionAction[] Actions { get; }
    }

    public class PahkatConfig
    {
        private IntPtr _handle;

        internal PahkatConfig(IntPtr handle)
        {
            _handle = handle;
        }

        public void SetUiSetting([NotNull] string key, [CanBeNull] string value)
        {
            var cKey = MarshalUtf8.StringToHGlobalUtf8(key);
            var cValue = value != null ? MarshalUtf8.StringToHGlobalUtf8(value) : IntPtr.Zero;
            Native.pahkat_config_ui_set(_handle, cKey, cValue);
            Marshal.FreeHGlobal(cKey);

            if (cValue != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(cValue);
            }
        }

        public void SetUiSetting<T>([NotNull] string key, [CanBeNull] T obj)
        {
            var value = JsonConvert.SerializeObject(obj);
            SetUiSetting(key, value);
        }

        [CanBeNull]
        public string GetUiSetting([NotNull] string key)
        {
            var cKey = MarshalUtf8.StringToHGlobalUtf8(key);
            var cValue = Native.pahkat_config_ui_get(_handle, cKey);
            Marshal.FreeHGlobal(cKey);

            if (cValue == IntPtr.Zero)
            {
                return null;
            }

            var o = MarshalUtf8.PtrToStringUtf8(cValue);
            Native.pahkat_str_free(cValue);
            return o;
        }

        [CanBeNull]
        public T GetUiSetting<T>([NotNull] string key)
        {
            var value = GetUiSetting(key);
            return value != null ? JsonConvert.DeserializeObject<T>(value) : default(T);
        }

        public RepoConfig[] GetRepos()
        {
            var cStr = Native.pahkat_config_repos(_handle);
            var str = MarshalUtf8.PtrToStringUtf8(cStr);
            Native.pahkat_str_free(cStr);
            return JsonConvert.DeserializeObject<RepoConfig[]>(str);
        }

        public void SetRepos(RepoConfig[] repos)
        {
            var repoStr = JsonConvert.SerializeObject(repos);
            var cRepoStr = MarshalUtf8.StringToHGlobalUtf8(repoStr);
            Native.pahkat_config_set_repos(_handle, cRepoStr);
            Marshal.FreeHGlobal(cRepoStr);
        }
    }

    public class PahkatTransaction : IPahkatTransaction
    {
        private readonly PahkatClient _client;
        private readonly IntPtr _handle;
        private readonly IntPtr[] _actions;

        public TransactionAction[] Actions { get; }

        public PahkatTransaction(PahkatClient client, TransactionAction[] actions)
        {
            _client = client;
            _actions = actions.Select((a) => a.ToCType()).ToArray();

            IntPtr cStr;
            unsafe
            {
                _handle = Native.pahkat_create_package_transaction(client.handle,
                    (uint)actions.Length,
                    _actions,
                    out var errors);

                cStr = Native.pahkat_package_transaction_actions(client.handle, _handle, out var txErrors);
            }

            var str = MarshalUtf8.PtrToStringUtf8(cStr);
            Native.pahkat_str_free(cStr);

            var weakActions = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(str);
            // HACK: parsing the key as a string causes newtonsoft JSON to throw up.
            Actions = weakActions.Select((x) => TransactionAction.FromJson(x)).ToArray();

            //            Actions = JsonConvert.DeserializeObject<TransactionAction[]>(str);
        }

        public IObservable<PackageEvent> Process()
        {
            return Observable.Create<PackageEvent>((observer) =>
            {
                //                var task = new Task(() =>
                //                {
                unsafe
                {
                    var result = Native.pahkat_run_package_transaction(_client.handle, _handle, 0,
                        ((txId, rawPackageId, eventCode) =>
                        {
                            var packageKey = AbsolutePackageKey.FromPtr(rawPackageId);
                            var evt = PackageEvent.FromCode(packageKey, eventCode);
                            observer.OnNext(evt);
                        }), out var errors);

                    if (result == 0)
                    {
                        observer.OnCompleted();
                    }
                    else
                    {
                        observer.OnError(new Exception($"Return code {result}"));
                    }
                }
                //                });
                //
                //                task.Start();

                return Disposable.Empty;
            });
        }
    }

    public class AbsolutePackageKey
    {
        public readonly string Url;
        public readonly string Id;
        public readonly string Channel;

        public override string ToString()
        {
            return $"{Url}packages/{Id}#{Channel}";
        }

        public AbsolutePackageKey(Uri url)
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

            Url = builder.Uri.ToString();
            Id = id;
            Channel = channel;
        }

        public static AbsolutePackageKey FromPtr(IntPtr ptr)
        {
            var packageId = MarshalUtf8.PtrToStringUtf8(ptr);
            if (packageId == null)
            {
                return null;
            }

            var packageIdUrl = new Uri(packageId);
            return new AbsolutePackageKey(packageIdUrl);
        }

        protected bool Equals(AbsolutePackageKey other)
        {
            return string.Equals(Url, other.Url) && string.Equals(Id, other.Id) && string.Equals(Channel, other.Channel);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AbsolutePackageKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Url != null ? Url.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Channel != null ? Channel.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct pahkat_repo_t
    {
        IntPtr Url;
        IntPtr Channel;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct pahkat_error_t
    {
        internal uint Code;
        internal IntPtr Message;

        public override string ToString()
        {
            return $"{MarshalUtf8.PtrToStringUtf8(Message)} (error code {Code})";
        }
    }

    public struct DownloadProgress
    {
        public AbsolutePackageKey PackageId;
        public PackageDownloadStatus Status;
        public UInt64 Downloaded;
        public UInt64 Total;
        [CanBeNull] public string ErrorMessage;

        public static DownloadProgress Progress(AbsolutePackageKey packageId, UInt64 cur, UInt64 max)
        {
            return new DownloadProgress
            {
                PackageId = packageId,
                Status = PackageDownloadStatus.Progress,
                Downloaded = cur,
                Total = max
            };
        }

        public static DownloadProgress Completed(AbsolutePackageKey packageId)
        {
            return new DownloadProgress
            {
                PackageId = packageId,
                Status = PackageDownloadStatus.Completed
            };
        }

        public static DownloadProgress NotStarted(AbsolutePackageKey packageId)
        {
            return new DownloadProgress
            {
                PackageId = packageId,
                Status = PackageDownloadStatus.NotStarted
            };
        }

        public static DownloadProgress Error(AbsolutePackageKey packageId, string error)
        {
            return new DownloadProgress
            {
                PackageId = packageId,
                Status = PackageDownloadStatus.Error,
                ErrorMessage = error
            };
        }

        public static DownloadProgress Starting(AbsolutePackageKey packageId)
        {
            return new DownloadProgress
            {
                PackageId = packageId,
                Status = PackageDownloadStatus.Starting
            };
        }
    }

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

    public class PackageStatusResponse
    {
        [JsonProperty("status", Required = Required.Always)]
        public readonly PackageStatus Status;

        [JsonProperty("target", Required = Required.Always)]
        public readonly InstallerTarget Target;

        public PackageStatusResponse(PackageStatus status, InstallerTarget target)
        {
            Status = status;
            Target = target;
        }
    }

    public class PahkatClient : IDisposable
    {
        internal IntPtr handle;
        private bool disposed = false;

        public readonly PahkatConfig Config;

        public PahkatClient(string configPath = null, bool saveChanges = true)
        {
            IntPtr ptr = configPath != null ? MarshalUtf8.StringToHGlobalUtf8(configPath) : IntPtr.Zero;
            handle = Native.pahkat_client_new(ptr, saveChanges ? (byte)1 : (byte)0);

            if (handle == IntPtr.Zero)
            {
                throw new Exception("pahkat_client_new returned a NULL pointer");
            }

            Config = new PahkatConfig(handle);

            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public string ConfigPath
        {
            get
            {
                var ptr = Native.pahkat_config_path(handle);
                var str = MarshalUtf8.PtrToStringUtf8(ptr);
                Native.pahkat_str_free(ptr);
                return str;
            }
        }

        public void RefreshRepos()
        {
            Native.pahkat_refresh_repos(handle);
        }

        public void ForceRefreshRepos()
        {
            Native.pahkat_force_refresh_repos(handle);
        }

        public RepositoryIndex[] Repos()
        {
            var stringPtr = Native.pahkat_repos_json(handle);
            var rawString = MarshalUtf8.PtrToStringUtf8(stringPtr);

            var repos = JsonConvert.DeserializeObject<RepositoryIndex[]>(rawString);

            foreach (var repo in repos)
            {
                var statuses = new Dictionary<AbsolutePackageKey, PackageStatusResponse>();
                foreach (var package in repo.Packages.Values)
                {
                    var packageKey = repo.AbsoluteKeyFor(package);

                    UInt32 error = 0;
                    var cPackageKey = MarshalUtf8.StringToHGlobalUtf8(packageKey.ToString());
                    var cStatus = Native.pahkat_status(handle, cPackageKey, out error);
                    Marshal.FreeHGlobal(cPackageKey);

                    // TODO: check errors

                    if (cStatus == IntPtr.Zero)
                    {
                        continue;
                    }

                    var status = MarshalUtf8.PtrToStringUtf8(cStatus);
                    Native.pahkat_str_free(cStatus);

                    var response = JsonConvert.DeserializeObject<PackageStatusResponse>(status);
                    statuses[packageKey] = response;
                }

                repo.Statuses = statuses;
            }

            Native.pahkat_str_free(stringPtr);
            return repos;
        }

        public void Install(AbsolutePackageKey packageKey, InstallerTarget target)
        {
            var keyPtr = MarshalUtf8.StringToHGlobalUtf8(packageKey.ToString());
            Native.pahkat_package_install(handle, keyPtr, 0);
            Marshal.FreeHGlobal(keyPtr);
        }

        public string PackagePath(AbsolutePackageKey packageKey)
        {
            var keyPtr = MarshalUtf8.StringToHGlobalUtf8(packageKey.ToString());
            var ptr = Native.pahkat_package_path(handle, keyPtr);
            Marshal.FreeHGlobal(keyPtr);
            var o = MarshalUtf8.PtrToStringUtf8(ptr);
            Native.pahkat_str_free(ptr);
            return o;
        }

        public IObservable<DownloadProgress> Download(AbsolutePackageKey packageKey, InstallerTarget target)
        {
            return Observable.Create<DownloadProgress>((observer) =>
            {
                void Callback(IntPtr rawPackageId, ulong cur, ulong max)
                {
                    var localPackageKey = AbsolutePackageKey.FromPtr(rawPackageId);
                    if (localPackageKey == null)
                    {
                        return;
                    }

                    if (cur < max)
                    {
                        observer.OnNext(DownloadProgress.Progress(localPackageKey, cur, max));
                    }
                    else
                    {
                        observer.OnNext(DownloadProgress.Progress(localPackageKey, cur, max));
                        observer.OnNext(DownloadProgress.Completed(localPackageKey));
                        //                        observer.OnCompleted();
                    }
                }

                var task = new Task(() =>
                {
                    observer.OnNext(DownloadProgress.NotStarted(packageKey));
                    observer.OnNext(DownloadProgress.Starting(packageKey));

                    unsafe
                    {
                        var cKey = MarshalUtf8.StringToHGlobalUtf8(packageKey.ToString());
                        var ret = Native.pahkat_download_package(handle, cKey, target.ToByte(), Callback, out var error);

                        if (error != null)
                        {
                            var str = error->ToString();
                            observer.OnNext(DownloadProgress.Error(packageKey, str));
                            observer.OnError(new Exception(str));
                            Native.pahkat_error_free(error);
                        }
                        else
                        {
                            observer.OnCompleted();
                        }

                        //                        if (ret > 0)
                        //                        {
                        //                            observer.OnNext(DownloadProgress.Error(packageKey, $"Error code {ret}"));
                        //                        }

                    }
                });

                task.Start();

                return Disposable.Empty;
            });
        }

        //        private IObservable<IPahkatTransaction> AdminTransaction(TransactionAction[] actions)
        //        {
        //            
        //        }
        //
        public IPahkatTransaction Transaction(TransactionAction[] actions)
        {
            if (actions.Any((action) => action.Target == InstallerTarget.System))
            {
                //                return AdminTransaction(actions);
                return new PahkatTransaction(this, actions);
            }
            else
            {
                return new PahkatTransaction(this, actions);
            }
        }

        //public string GetUiConfig(string key)
        //{
        //    if (string.IsNullOrWhiteSpace(key))
        //    {
        //        throw new ArgumentException($"{nameof(key)} cannot be null or whitespace");
        //    }
        //    return pahkat_config_ui_get(handle, key);
        //}

        //public void SetUiConfig(string key, string value)
        //{
        //    if (string.IsNullOrWhiteSpace(key))
        //    {
        //        throw new ArgumentException($"{nameof(key)} cannot be null or whitespace");
        //    }
        //    pahkat_config_ui_set(handle, key, value);
        //}

        //public string GetReposConfig()
        //{
        //    return pahkat_config_repos(handle);
        //}

        //public void SetReposConfig(string config)
        //{
        //    pahkat_config_set_repos(handle, config);
        //}

        //public void RefreshRepos()
        //{
        //    pahkat_refresh_repos(handle);
        //}

        //public string GetReposJson()
        //{
        //    return pahkat_repos_json(handle);
        //}

        //public string GetPackageStatus(string packageId)
        //{
        //    uint error;
        //    var status = pahkat_status(handle, packageId, out error);
        //    if (error != 0)
        //    {
        //        throw new Exception($"Failed to get package status with error code: {error}");
        //    }
        //    return status;
        //}

        //public void DownloadPackage(string packageId)
        //{
        //    unsafe
        //    {
        //        pahkat_error_t* error = null;
        //        if (pahkat_download_package(handle, packageId, (byte)InstallTarget.System, ProcessDownloadCallback, &error) != 0)
        //        {
        //            var code = (*error).GetCode();
        //            var message = (*error).GetMessage();
        //            pahkat_error_free(&error);
        //            throw new Exception($"Failed to download package: {packageId}, code: {code}, message: {message}");
        //        }
        //    }
        //}

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (handle != IntPtr.Zero)
                    {
                        Native.pahkat_client_free(handle);
                        handle = IntPtr.Zero;
                    }
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        //        void ProcessDownloadCallback(string packageId, ulong cur, ulong max)
        //        {
        //            Console.WriteLine($"Received download callback for {packageId}, cur: {cur}, max: {max}");
        //        }

    }

    internal static class Native
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate void DownloadProgressCallback(IntPtr packageId, ulong cur, ulong max);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate void PackageTransactionRunCallback(uint txId, IntPtr packageId, uint action);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pahkat_client_new(IntPtr configPath, byte saveChanges);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pahkat_config_path(IntPtr handle);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pahkat_config_ui_get(IntPtr handle, IntPtr key);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pahkat_config_ui_set(IntPtr handle, IntPtr key, IntPtr value);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pahkat_config_repos(IntPtr handle);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pahkat_config_set_repos(IntPtr handle, IntPtr repos);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pahkat_refresh_repos(IntPtr handle);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pahkat_force_refresh_repos(IntPtr handle);

        [DllImport("pahkat_client.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void pahkat_client_free(IntPtr handle);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pahkat_repos_json(IntPtr handle);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pahkat_status(IntPtr handle, IntPtr packageId, out uint error);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pahkat_str_free(IntPtr str);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void pahkat_error_free(pahkat_error_t* error);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe uint pahkat_download_package(IntPtr handle, IntPtr packageKey, byte target, DownloadProgressCallback callback, out pahkat_error_t* error);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pahkat_package_install(IntPtr handle, IntPtr packageKey, byte target);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pahkat_package_path(IntPtr handle, IntPtr packageKey);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe IntPtr pahkat_create_package_transaction(IntPtr handle, uint actionCount, IntPtr[] actions, out pahkat_error_t* error);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pahkat_create_action(byte action, byte target, IntPtr packageKey);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe IntPtr pahkat_package_transaction_actions(IntPtr handle, IntPtr transaction, out pahkat_error_t* error);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe uint pahkat_run_package_transaction(IntPtr handle, IntPtr transaction, uint txId, PackageTransactionRunCallback callback, out pahkat_error_t* error);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe byte pahkat_semver_is_valid(IntPtr versionString);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int pahkat_semver_compare(IntPtr lhsVersionString, IntPtr rhsVersionString);
    }
}
