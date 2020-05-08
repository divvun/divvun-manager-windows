using System.Linq;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Fbs;

namespace Divvun.Installer.Extensions
{
    public static class NativeNameExt
    {
        public static string? NativeName(this ResolvedAction action) {
            // TODO: localise
            return action.Name["en"];
        }

        public static string? NativeName(this Descriptor descriptor) {
            var map = descriptor.Name();

            return map.First().Value;
        }

        public static string? NativeName(this LoadedRepository.IndexValue value) {
            return value.Name["en"];
        }
    }
}