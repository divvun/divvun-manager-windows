using System;
using System.Runtime.InteropServices;

namespace Pahkat.Service.CoreLib
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct pahkat_repo_t
    {
        IntPtr url;
        IntPtr channel;

        public string GetUrl()
        {
            return Marshal.PtrToStringUni(url);
        }

        public string GetChannel()
        {
            return Marshal.PtrToStringUni(channel);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct pahkat_error_t
    {
        uint code;
        IntPtr message;

        public uint GetCode()
        {
            return code;
        }

        public string GetMessage()
        {
            return Marshal.PtrToStringUni(message);
        }
    }

    internal class Client: IDisposable
    {
        private IntPtr handle;
        private bool disposed = false;

        public Client(string configPath)
        {
            handle = pahkat_client_new(configPath);
            if (handle == IntPtr.Zero)
            {
                throw new Exception("pahkat_client_new returned a NULL pointer");
            }
        }

        public string GetConfigPath()
        {
            return pahkat_config_path(handle);
        }

        public string GetUiConfig(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException($"{nameof(key)} cannot be null or whitespace");
            }
            return pahkat_config_ui_get(handle, key);
        }

        public void SetUiConfig(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException($"{nameof(key)} cannot be null or whitespace");
            }
            pahkat_config_ui_set(handle, key, value);
        }

        public string GetReposConfig()
        {
            return pahkat_config_repos(handle);
        }

        public void SetReposConfig(string config)
        {
            pahkat_config_set_repos(handle, config);
        }

        public void RefreshRepos()
        {
            pahkat_refresh_repos(handle);
        }

        public string GetReposJson()
        {
            return pahkat_repos_json(handle);
        }

        public string GetPackageStatus(string packageId)
        {
            uint error;
            var status = pahkat_status(handle, packageId, out error);
            if (error != 0)
            {
                throw new Exception($"Failed to get package status with error code: {error}");
            }
            return status;
        }

        public void DownloadPackage(string packageId)
        {
            unsafe
            {
                pahkat_error_t* error = null;
                if (pahkat_download_package(handle, packageId, (byte)InstallTarget.System, ProcessDownloadCallback, &error) != 0)
                {
                    var code = (*error).GetCode();
                    var message = (*error).GetMessage();
                    pahkat_error_free(&error);
                    throw new Exception($"Failed to download package: {packageId}, code: {code}, message: {message}");
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (handle != IntPtr.Zero)
                    {
                        pahkat_client_free(handle);
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

        void ProcessDownloadCallback(string packageId, ulong cur, ulong max)
        {
            Console.WriteLine($"Received download callback for {packageId}, cur: {cur}, max: {max}");
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        delegate void DownloadProgressCallback([MarshalAs(UnmanagedType.LPWStr)] string package_id, ulong cur, ulong max);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        delegate void PackageTransactionRunCallback([MarshalAs(UnmanagedType.LPWStr)] string package_id, uint action);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr pahkat_client_new([MarshalAs(UnmanagedType.LPWStr)] string config_path);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        static extern string pahkat_config_path(IntPtr handle);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        static extern string pahkat_config_ui_get(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] string key);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static extern void pahkat_config_ui_set(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] string key, [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        static extern string pahkat_config_repos(IntPtr handle);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static extern void pahkat_config_set_repos(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] string repos);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static extern void pahkat_refresh_repos(IntPtr handle);

        [DllImport("pahkat_client.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void pahkat_client_free(IntPtr handle);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        static extern string pahkat_repos_json(IntPtr handle);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        static extern string pahkat_status(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] string package_id, out uint error);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static extern void pahkat_str_free([MarshalAs(UnmanagedType.LPWStr)] string str);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        unsafe static extern void pahkat_error_free(pahkat_error_t** error);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        unsafe static extern uint pahkat_download_package(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] string package_key, byte target, DownloadProgressCallback callback, pahkat_error_t** error);

        // todo:
        [DllImport("pahkat_client.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        unsafe static extern IntPtr pahkat_create_package_transaction(IntPtr handle, uint action_count, IntPtr actions, pahkat_error_t** error);

        // todo:
        [DllImport("pahkat_client.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        unsafe static extern uint pahkat_run_package_transaction(IntPtr handle, IntPtr transaction, uint tx_id, PackageTransactionRunCallback callback, pahkat_error_t** error);

        // todo:
        [DllImport("pahkat_client.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        unsafe static extern string pahkat_package_transaction_actions(IntPtr handle, IntPtr transaction, pahkat_error_t** error);
    }
}
