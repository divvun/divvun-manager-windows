using System;

namespace Pahkat.Sdk.Native
{
    public abstract class Arced
    {
        internal IntPtr handle { get; private set; }

        internal Arced(IntPtr handle)
        {
            this.handle = handle;
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}
