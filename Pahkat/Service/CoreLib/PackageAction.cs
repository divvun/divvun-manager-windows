using System;
using System.Runtime.InteropServices;

namespace Pahkat.Service.CoreLib
{
    internal class PackageAction: IDisposable
    {
        IntPtr handle;
        bool disposed;

        public PackageAction(string packageId, PackageActionType type, InstallTarget target)
        {
            handle = pahkat_create_action((byte)type, (byte)target, packageId);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (handle != IntPtr.Zero)
                    {
                        pahkat_free_action(handle);
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

        [DllImport("pahkat_client.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr pahkat_create_action(byte action, byte target, [MarshalAs(UnmanagedType.LPWStr)] string package_key);

        [DllImport("pahkat_client.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static extern void pahkat_free_action(IntPtr action);
    }
}
