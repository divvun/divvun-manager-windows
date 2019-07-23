using Pahkat.Sdk.Native;
using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using static Pahkat.Sdk.PahkatClientException;

namespace Pahkat.Sdk
{
    public class PackageStore : Arced
    {
        internal PackageStore(IntPtr handle) : base(handle) { }

        public static PackageStore Default() => pahkat_client.pahkat_windows_package_store_default();

        public static PackageStore New(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var store = pahkat_client.pahkat_windows_package_store_new(fullPath, out var exception);
            Try(exception);
            return store;
        }

        public static PackageStore Load(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var store = pahkat_client.pahkat_windows_package_store_load(fullPath, out var exception);
            Try(exception);
            return store;
        }

        public static PackageStore NewForSelfUpdate(string path)
        {
            // We copy the file to a tmp file first so we can modify it without worry.
            var tmpFile = Path.GetTempFileName();
            File.Copy(path, tmpFile, true);
            return Load(tmpFile);
        }

        public StoreConfig Config()
        {
            var result = pahkat_client.pahkat_windows_package_store_config(this, out var exception);
            Try(exception);
            return result;
        }

        public (PackageStatus, PackageTarget) Status(AbsolutePackageKey key)
        {
            var result = pahkat_client.pahkat_windows_package_store_status(this, key, out var isSystem);
            var status = PackageStatusExt.FromInt(result);
            return (status, isSystem ? PackageTarget.System : PackageTarget.User);
        }

        public void RefreshRepos()
        {
            pahkat_client.pahkat_windows_package_store_refresh_repos(this, out var exception);
            Try(exception);
        }

        public void ClearCache()
        {
            pahkat_client.pahkat_windows_package_store_clear_cache(this, out var exception);
            Try(exception);
        }

        public void ForceRefreshRepos()
        {
            pahkat_client.pahkat_windows_package_store_force_refresh_repos(this, out var exception);
            Try(exception);
        }

        public bool RemoveRepo(string url, string channel)
        {
            var result = pahkat_client.pahkat_windows_package_store_remove_repo(this, url, channel, out var exception);
            Try(exception);
            return result;
        }

        public bool AddRepo(string url, string channel)
        {
            var result = pahkat_client.pahkat_windows_package_store_add_repo(this, url, channel, out var exception);
            Try(exception);
            return result;
        }

        public bool UpdateRepo(uint index, string url, string channel)
        {
            var result = pahkat_client.pahkat_windows_package_store_update_repo(this, index, url, channel, out var exception);
            Try(exception);
            return result;
        }

        public RepositoryIndex[] RepoIndexes()
        {
            var result = pahkat_client.pahkat_windows_package_store_repo_indexes(this, out var exception);
            Try(exception);
            return result;
        }

        public Package ResolvePackage(AbsolutePackageKey key)
        {
            var result = pahkat_client.pahkat_windows_package_store_resolve_package(this, key, out var exception);
            Try(exception);
            return result;
        }

        public IObservable<DownloadProgress> Download(AbsolutePackageKey key, PackageTarget target)
        {
            return Observable.Create<DownloadProgress>((observer) =>
            {
                // Callback for FFI
                void Callback(IntPtr rawPackageId, ulong cur, ulong max)
                {
                    if (cur < max)
                    {
                        observer.OnNext(DownloadProgress.Progress(key, cur, max));
                    }
                    else
                    {
                        observer.OnNext(DownloadProgress.Progress(key, cur, max));
                        observer.OnNext(DownloadProgress.Completed(key));
                    }
                }

                var task = new Task(() =>
                {
                    observer.OnNext(DownloadProgress.NotStarted(key));
                    observer.OnNext(DownloadProgress.Starting(key));

                    unsafe
                    {
                        var ret = pahkat_client.pahkat_windows_package_store_download(this, key, Callback, out var exception);
                        try
                        {
                            Try(exception);
                            observer.OnCompleted();
                        }
                        catch (PahkatClientException e)
                        {
                            observer.OnNext(DownloadProgress.Error(key, e.Message));
                            observer.OnError(e);
                        }
                    }
                });

                task.Start();

                return Disposable.Empty;
            });
        }
    }
}
