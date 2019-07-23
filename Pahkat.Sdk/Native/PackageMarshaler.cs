using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pahkat.Sdk.Native
{
    class PackageMarshaler : JsonMarshaler<Package>
    {
        private static PackageMarshaler instance = new PackageMarshaler();
        static ICustomMarshaler GetInstance(string cookie) => instance;
    }
}
