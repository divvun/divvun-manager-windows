using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Pahkat.Sdk.Native
{
#pragma warning disable IDE1006 // Naming Styles
    /// <summary>
    /// StoreConfig FFI functions
    /// </summary>
    unsafe internal partial class pahkat_client // store_config
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate void ErrCallback(IntPtr cString);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pahkat_store_config_set_ui_value(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StoreConfigMarshaler))]
            [In] StoreConfig handle,

            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8CStrMarshaler))]
            [In] string key,

            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8CStrMarshaler))]
            [In] string value,

            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8CStrMarshaler))]
        internal static extern string pahkat_store_config_ui_value(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StoreConfigMarshaler))]
            [In] StoreConfig handle,

            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8CStrMarshaler))]
            [In] string key,

            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8CStrMarshaler))]
        internal static extern string pahkat_store_config_skipped_package(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StoreConfigMarshaler))]
            [In] StoreConfig handle,

            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageKeyMarshaler))]
            [In] PackageKey key,

            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pahkat_store_config_add_skipped_package(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StoreConfigMarshaler))]
            [In] StoreConfig handle,

            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageKeyMarshaler))]
            [In] PackageKey key,

            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8CStrMarshaler))]
            [In] string value,

            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pahkat_store_config_set_repos(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StoreConfigMarshaler))]
            [In] StoreConfig handle,

            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(RepoRecordListMarshaler))]
            [In] RepoRecord[] recordList,

            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(RepoRecordListMarshaler))]
        internal static extern RepoRecord[] pahkat_store_config_repos(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StoreConfigMarshaler))]
            [In] StoreConfig handle,

            [In] ErrCallback exception);
    }

    /// <summary>
    /// Transaction FFI functions
    /// </summary>
    unsafe internal partial class pahkat_client // transactions
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate void TransactionProcessCallback(uint txId, IntPtr packageId, uint action);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TransactionMarshaler))]
        internal static extern Transaction pahkat_windows_transaction_new(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageStoreMarshaler))]
            [In] PackageStore store,

            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ActionListMarshaler))]
            TransactionAction[] actions,

            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pahkat_windows_transaction_process(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TransactionMarshaler))]
            [In] Transaction transaction,
            uint tag,
            [In] TransactionProcessCallback callback,
            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ActionListMarshaler))]
        internal static extern TransactionAction[] pahkat_windows_transaction_actions(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TransactionMarshaler))]
            [In] Transaction transaction,
            [In] ErrCallback exception);

        //[DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TransactionActionMarshaler))]
        //internal static extern TransactionAction pahkat_windows_action_new_install(
        //    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageKeyMarshaler))]
        //    [In] PackageKey key,
        //    bool targetIsSystem,
        //    [In] ErrCallback exception);

        //[DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TransactionActionMarshaler))]
        //internal static extern TransactionAction pahkat_windows_action_new_uninstall(
        //    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageKeyMarshaler))]
        //    [In] PackageKey key,
        //    bool targetIsSystem,
        //    [In] ErrCallback exception);

        //[DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8CStrMarshaler))]
        //internal static extern string pahkat_windows_action_to_json(
        //    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TransactionActionMarshaler))]
        //    [In] TransactionAction action,
        //    [In] ErrCallback exception);

        //[DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TransactionActionMarshaler))]
        //internal static extern TransactionAction pahkat_windows_action_from_json(
        //    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8CStrMarshaler))]
        //    [In] string actionString,
        //    [In] ErrCallback exception);
    }

    /// <summary>
    /// General FFI functions
    /// </summary>
    unsafe internal partial class pahkat_client // etc
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate void DownloadProgressCallback(IntPtr packageId, ulong cur, ulong max);

        //[DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //internal static extern bool pahkat_arc_release(
        //    IntPtr arcPtrHandle,
        //    [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pahkat_enable_logging(byte level);

    }

    /// <summary>
    /// PackageStore FFI functions
    /// </summary>
    unsafe internal partial class pahkat_client // package_store
    {
        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageStoreMarshaler))]
        internal static extern PackageStore pahkat_windows_package_store_default();

        // CharSet.Unicode here isn't a bug as we're dealing with Windows paths, and the Rust side of the code
        // expects a wide string.
        [DllImport(nameof(pahkat_client), CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageStoreMarshaler))]
        internal static extern PackageStore pahkat_windows_package_store_new(
            [In] string path,
            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageStoreMarshaler))]
        internal static extern PackageStore pahkat_windows_package_store_load(
            [In] string path,
            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        internal static extern string pahkat_windows_package_store_download(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageStoreMarshaler))]
            [In] PackageStore handle,

            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageKeyMarshaler))]
            [In] PackageKey key,

            [In] DownloadProgressCallback callback,

            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StoreConfigMarshaler))]
        internal static extern StoreConfig pahkat_windows_package_store_config(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageStoreMarshaler))]
            [In] PackageStore handle,
            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern sbyte pahkat_windows_package_store_status(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageStoreMarshaler))]
            [In] PackageStore handle,

            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageKeyMarshaler))]
            [In] PackageKey key,

            [Out] out bool isSystem);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pahkat_windows_package_store_refresh_repos(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageStoreMarshaler))]
            [In] PackageStore store,
            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pahkat_windows_package_store_clear_cache(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageStoreMarshaler))]
            [In] PackageStore store,
            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pahkat_windows_package_store_force_refresh_repos(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageStoreMarshaler))]
            [In] PackageStore store,
            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(RepositoryIndexListMarshaler))]
        internal static extern RepositoryIndex[] pahkat_windows_package_store_repo_indexes(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageStoreMarshaler))]
            [In] PackageStore store,
            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool pahkat_windows_package_store_add_repo(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageStoreMarshaler))]
            [In] PackageStore store,
            string url,
            string channel,
            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool pahkat_windows_package_store_remove_repo(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageStoreMarshaler))]
            [In] PackageStore store,
            string url,
            string channel,
            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool pahkat_windows_package_store_update_repo(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageStoreMarshaler))]
            [In] PackageStore store,
            uint index,
            string url,
            string channel,
            [In] ErrCallback exception);

        [DllImport(nameof(pahkat_client), CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageMarshaler))]
        internal static extern Package pahkat_windows_package_store_resolve_package(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageStoreMarshaler))]
            [In] PackageStore store,

            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PackageKeyMarshaler))]
            [In] PackageKey key,

            [In] ErrCallback exception);
    }
#pragma warning restore IDE1006 // Naming Styles
}
