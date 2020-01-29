using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pahkat.Sdk.Native
{
    class PackageTargetMarshaler : TypedMarshaler<PackageTarget>
    {
        private ICustomMarshaler marshaler = Utf8CStrMarshaler.GetInstance(typeof(PackageTarget).FullName);
        static ICustomMarshaler GetInstance(string cookie) => new PackageTargetMarshaler();

        public override int GetNativeDataSize() => IntPtr.Size;

        public override void CleanUpManagedData(PackageTarget obj)
        {
            marshaler.CleanUpManagedData(obj);
        }

        public override void CleanUpNativeData(IntPtr ptr)
        {
            marshaler.CleanUpNativeData(ptr);
        }

        public override IntPtr MarshalManagedToNative(PackageTarget obj)
        {
            string target;
            
            switch (obj)
            {
                case PackageTarget.System:
                    target = "system";
                    break;
                case PackageTarget.User:
                default:
                    target = "user";
                    break;
            };

            return marshaler.MarshalManagedToNative(target);
        }

        public override PackageTarget MarshalNativeToTypedManaged(IntPtr ptr)
        {
            var jsonString = (string)marshaler.MarshalNativeToManaged(ptr);
            return JsonConvert.DeserializeObject<PackageTarget>(jsonString);
        }
    }
}
