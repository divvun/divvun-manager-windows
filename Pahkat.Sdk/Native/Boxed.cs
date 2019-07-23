using System;

namespace Pahkat.Sdk.Native
{
    public abstract class Boxed
    {
        internal IntPtr handle { get; private set; }

        internal Boxed(IntPtr handle)
        {
            this.handle = handle;
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}
