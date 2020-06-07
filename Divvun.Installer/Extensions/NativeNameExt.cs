using System.Globalization;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Fbs;

namespace Divvun.Installer.Extensions
{
    public static class NativeNameExt
    {
        public static string NativeName(this ResolvedAction action) {
            var tag = CultureInfo.CurrentCulture.IetfLanguageTag;
            return (action.Name.ContainsKey(tag) ? action.Name[tag] : action.Name["en"]) ?? action.Action.PackageKey.Id;
        }

        public static string NativeName(this Descriptor descriptor) {
            var tag = CultureInfo.CurrentCulture.IetfLanguageTag;
            var map = descriptor.Name();
            var name = (map.ContainsKey(tag) ? map[tag] : map["en"]) ?? descriptor.Id;
            return name;
        }

        public static string NativeName(this LoadedRepository.IndexValue value) {
            var tag = CultureInfo.CurrentCulture.IetfLanguageTag;
            var map = value.Name;
            return (map.ContainsKey(tag) ? map[tag] : map["en"]) ?? value.Url.ToString();
        }

        public static string NativeName(this InstallAction installAction) {
            switch (installAction) {
                case InstallAction.Install:
                    return Strings.Install;
                case InstallAction.Uninstall:
                    return Strings.Uninstall;
                default:
                    return "??";
            }
        }
    }
}