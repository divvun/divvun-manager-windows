using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Iterable;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FlatBuffers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pahkat.Sdk.Rpc.Fbs;
using Pahkat.Sdk.Rpc.Models;
using Serilog;

namespace Pahkat.Sdk.Rpc
{
    public class LoadedRepository : ILoadedRepository
    {
        public class IndexValue
        {
            public class AgentValue
            {
                public string Name;
                public string Version;
                public Uri? Url;
            }
            
            public Uri Url;
            public string[] Channels;
            public string? DefaultChannel;
            public IReadOnlyDictionary<string, string> Name;
            public IReadOnlyDictionary<string, string> Description;
            public AgentValue Agent;
            public Uri? LandingUrl;

            string[] LinkedRepositories;
            string[] AcceptedRepositories;
        }

        public class MetaValue
        {
            public string? Channel;
        }
        
        public byte[] PackagesFbs { get; set; }

        public IndexValue Index { get; }
        public MetaValue Meta { get; }
        public IPackages Packages => Fbs.Packages.GetRootAsPackages(new ByteBuffer(PackagesFbs));

        public PackageKey PackageKey(IDescriptor descriptor) {
            return Sdk.PackageKey.Create(Index.Url, descriptor.Id);
        }
    }
    
    public static class MarshalUtf8
    {
        public static string PtrToStringUtf8(IntPtr utf8Ptr, long len) {
            var buffer = new byte[len];
            Marshal.Copy(utf8Ptr, buffer, 0, (int)len);
            return Encoding.UTF8.GetString(buffer);
        }

        public static pahkat_rpc.Slice StringToHGlobalUtf8(string str) {
            var buffer = Encoding.UTF8.GetBytes(str);
            Array.Resize(ref buffer, buffer.Length);
            var ptr = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, ptr, buffer.Length);
            return new pahkat_rpc.Slice(ptr, buffer.Length);
        }
    }
    
    public class PahkatClientException : Exception
    {
        private static string? _lastError;

        internal static pahkat_rpc.ErrCallback Callback = (ptr, len) => {
            _lastError = MarshalUtf8.PtrToStringUtf8(ptr, len.ToInt64());
        };
        
        internal static void AssertNoError() {
            if (_lastError != null) {
                var err = _lastError;
                _lastError = null;
                throw new PahkatClientException(err);
            }
        }

        private PahkatClientException(string message) : base(message) { }
    }

    public interface IPahkatClient
    {
        CancellationTokenSource ProcessTransaction(PackageAction[] actions, Action<TransactionResponseValue> callback);
        PackageStatus Status(PackageKey packageKey);
        Dictionary<Uri, ILoadedRepository> RepoIndexes();
        Dictionary<Uri, RepoRecord> GetRepoRecords();
        Dictionary<Uri, RepoRecord> SetRepo(Uri url, RepoRecord record);
        Dictionary<Uri, RepoRecord> RemoveRepo(Uri url);
        IObservable<Notification> Notifications();
        Dictionary<Uri, LocalizationStrings> Strings(string languageTag);
        string ResolvePackageQuery(PackageQuery query);
    }

    public class PahkatClient : IPahkatClient, IDisposable
    {
        public static PahkatClient Create() {
            var ptr = pahkat_rpc.pahkat_rpc_new(PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
            return new PahkatClient(ptr);
        }

        private readonly IntPtr handle;

        private PahkatClient(IntPtr handle) {
            this.handle = handle;
        }

        public CancellationTokenSource ProcessTransaction(PackageAction[] actions, Action<TransactionResponseValue> callback) {
            var stringActions = JsonConvert.SerializeObject(actions, Json.Settings.Value);
            Log.Debug(stringActions);
            var slice = pahkat_rpc.Slice.From(stringActions);

            pahkat_rpc.TransactionResponseCallback cCallback = (s) => {
                var str = MarshalUtf8.PtrToStringUtf8(s.Ptr, s.Length.ToInt64());

                Log.Debug("Raw tx response: " + str);

                try {
                    var value = JsonConvert.DeserializeObject<TransactionResponseValue>(str, Json.Settings.Value);
                    Log.Debug("Tx respo: " + value);
                    if (value != null) {
                        callback(value);
                    }
                    else {
                        Log.Debug("Warning: null transaction response");
                    }
                }
                catch (Exception e) {
                    callback(new TransactionResponseValue.TransactionError() {
                        Error = e.Message,
                    });
                }
            };

            var gch = GCHandle.Alloc(cCallback);
            
            pahkat_rpc.pahkat_rpc_process_transaction(handle, slice, (pahkat_rpc.TransactionResponseCallback)gch.Target, PahkatClientException.Callback);
            pahkat_rpc.Slice.Free(slice);
            
            PahkatClientException.AssertNoError();

            var source = new CancellationTokenSource();
            source.Token.Register(() => {
                gch.Free();
                // cancelCallback();
            });
            return source;
        }

        public PackageStatus Status(PackageKey packageKey) {
            var slice = pahkat_rpc.Slice.From(packageKey.ToString());
            var status = pahkat_rpc.pahkat_rpc_status(handle, slice, 0, PahkatClientException.Callback);
            pahkat_rpc.Slice.Free(slice);
            PahkatClientException.AssertNoError();
            return PackageStatusExt.FromInt(status);
        }

        public struct RepoIndexesResponse
        {
            public LoadedRepository[] Repositories { get; set; }
        }

        public Dictionary<Uri, ILoadedRepository> RepoIndexes() {
            var indexes = pahkat_rpc.pahkat_rpc_repo_indexes(handle, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
            var str = indexes.AsString();
            // Log.Debug(str);

            var list = JsonConvert.DeserializeObject<RepoIndexesResponse>(str, Json.Settings.Value);
            pahkat_rpc.pahkat_rpc_slice_free(indexes);

            var map = new Dictionary<Uri, ILoadedRepository>();
            
            foreach (var loadedRepository in list.Repositories) {
                map.Add(loadedRepository.Index.Url, loadedRepository);
            }
            
            return map;
        }

        public struct RepoRecordResponse
        {
            public Dictionary<Uri, RepoRecord> Records;
            public Dictionary<Uri, string> Errors;
        }

        public Dictionary<Uri, RepoRecord> GetRepoRecords() {
            pahkat_rpc.Slice recordSlice = pahkat_rpc.pahkat_rpc_get_repo_records(handle, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();

            var recordString = recordSlice.AsString();
            var records = JsonConvert.DeserializeObject<RepoRecordResponse>(recordString);
            pahkat_rpc.pahkat_rpc_slice_free(recordSlice);

            return records.Records;
        }

        public Dictionary<Uri, RepoRecord> SetRepo(Uri url, RepoRecord record) {
            pahkat_rpc.Slice recordSlice;

            {
                var cUrl = pahkat_rpc.Slice.From(url.ToString());
                var cRecord = pahkat_rpc.Slice.From(JsonConvert.SerializeObject(record, Json.Settings.Value));
                recordSlice = pahkat_rpc.pahkat_rpc_set_repo(handle, cUrl, cRecord, PahkatClientException.Callback);
                pahkat_rpc.Slice.Free(cUrl);
                pahkat_rpc.Slice.Free(cRecord);
                PahkatClientException.AssertNoError();
            }

            var recordString = recordSlice.AsString();
            var records = JsonConvert.DeserializeObject<RepoRecordResponse>(recordString);
            pahkat_rpc.pahkat_rpc_slice_free(recordSlice);

            return records.Records;
        }

        public Dictionary<Uri, RepoRecord> RemoveRepo(Uri url) {
            pahkat_rpc.Slice recordSlice;

            {
                var cUrl = pahkat_rpc.Slice.From(url.ToString());
                recordSlice = pahkat_rpc.pahkat_rpc_remove_repo(handle, cUrl, PahkatClientException.Callback);
                pahkat_rpc.Slice.Free(cUrl);
                PahkatClientException.AssertNoError();
            }

            var recordString = recordSlice.AsString();
            var records = JsonConvert.DeserializeObject<RepoRecordResponse>(recordString);
            pahkat_rpc.pahkat_rpc_slice_free(recordSlice);

            return records.Records;
        }

        public struct StringsResponse
        {
            public Dictionary<Uri, LocalizationStrings> Repos;
        }

        public Dictionary<Uri, LocalizationStrings> Strings(string languageTag) {
            var cTag = pahkat_rpc.Slice.From(languageTag);
            var slice = pahkat_rpc.pahkat_rpc_strings(handle, cTag, PahkatClientException.Callback);
            pahkat_rpc.Slice.Free(cTag);
            PahkatClientException.AssertNoError();
            
            var recordString = slice.AsString();
            pahkat_rpc.pahkat_rpc_slice_free(slice);
            var response = JsonConvert.DeserializeObject<StringsResponse>(recordString, Json.Settings.Value);

            return response.Repos;
        }

        public IObservable<Notification> Notifications() {
            return Observable.Create<Notification>(emitter => {
                pahkat_rpc.NotificationCallback callback = id => {
                    emitter.OnNext((Notification) id);
                };
                var gch = GCHandle.Alloc(callback);
                
                pahkat_rpc.pahkat_rpc_notifications(handle,
                    (pahkat_rpc.NotificationCallback)gch.Target,
                    PahkatClientException.Callback);

                return Disposable.Create(() => {
                    // FIXME: we can't actually free this unless we have a way to end this call.
                    // gch.Free();
                });
            });
        }

        public string ResolvePackageQuery(PackageQuery query) {
            var cQuery = pahkat_rpc.Slice.From(JsonConvert.SerializeObject(query, Json.Settings.Value));
            var slice = pahkat_rpc.pahkat_rpc_resolve_package_query(handle, cQuery, PahkatClientException.Callback);
            pahkat_rpc.Slice.Free(cQuery);
            PahkatClientException.AssertNoError();

            var queryResponse = slice.AsString();
            pahkat_rpc.pahkat_rpc_slice_free(slice);

            return queryResponse;
        }
        
        private void ReleaseUnmanagedResources() {
            pahkat_rpc.pahkat_rpc_free(handle);
        }

        public void Dispose() {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~PahkatClient() {
            ReleaseUnmanagedResources();
        }
    }
    
#pragma warning disable IDE1006 // Naming Styles
    public partial class pahkat_rpc
    {
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Slice
        {
            public readonly IntPtr Ptr;
            public readonly IntPtr Length;

            public static Slice From(string str) {
                return MarshalUtf8.StringToHGlobalUtf8(str);
            }

            public static void Free(Slice slice) {
                Marshal.FreeHGlobal(slice.Ptr);
            }
            
            public string AsString() {
                return MarshalUtf8.PtrToStringUtf8(Ptr, Length.ToInt64());
            }

            public Slice(IntPtr ptr, int length) {
                Ptr = ptr;
                Length = new IntPtr(length);
            }
            
            public Slice(IntPtr ptr, long length) {
                Ptr = ptr;
                Length = new IntPtr(length);
            }

            public static Slice Null => new Slice(IntPtr.Zero, 0);
        }
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate void ErrCallback(IntPtr bytes, IntPtr len);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate void TransactionResponseCallback(Slice slice);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate void NotificationCallback(Int32 notificationId);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate void CancelCallback();
        
        [DllImport(nameof(pahkat_rpc), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pahkat_rpc_slice_free(Slice slice);
        
        [DllImport(nameof(pahkat_rpc), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pahkat_rpc_free(IntPtr ptr);
        
        [DllImport(nameof(pahkat_rpc), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr pahkat_rpc_new([In] ErrCallback exception);
        
        [DllImport(nameof(pahkat_rpc), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern Slice pahkat_rpc_repo_indexes(IntPtr handle, [In] ErrCallback exception);

        [DllImport(nameof(pahkat_rpc), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pahkat_rpc_process_transaction(IntPtr handle, Slice actions, TransactionResponseCallback callback, [In] ErrCallback exception);

        [DllImport(nameof(pahkat_rpc), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pahkat_rpc_status(IntPtr handle, Slice packageKey, byte target, [In] ErrCallback exception);
        
        [DllImport(nameof(pahkat_rpc), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern Slice pahkat_rpc_strings(IntPtr handle, Slice languageTag, [In] ErrCallback exception);

        [DllImport(nameof(pahkat_rpc), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern Slice pahkat_rpc_get_repo_records(IntPtr handle, [In] ErrCallback exception);
        
        [DllImport(nameof(pahkat_rpc), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern Slice pahkat_rpc_set_repo(IntPtr handle, Slice repoUrl, Slice record, [In] ErrCallback exception);
        
        [DllImport(nameof(pahkat_rpc), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern Slice pahkat_rpc_remove_repo(IntPtr handle, Slice repoUrl, [In] ErrCallback exception);
        
        [DllImport(nameof(pahkat_rpc), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pahkat_rpc_notifications(IntPtr handle, NotificationCallback callback, [In] ErrCallback exception);
        
        [DllImport(nameof(pahkat_rpc), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern Slice pahkat_rpc_resolve_package_query(IntPtr handle, Slice packageQuery, [In] ErrCallback exception);
    }
#pragma warning restore IDE1006 // Naming Styles
}