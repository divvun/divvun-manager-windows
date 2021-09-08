using System.Collections.Generic;
using System.Globalization;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Models;

namespace Divvun.Installer.Extensions {

public static class ReadOnlyDictExt {
    public static TValue? Get<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dict, TKey key)
        where
        TValue : class {
        return dict.TryGetValue(key, out var value) ? value : null;
    }
}

public static class NativeNameExt {
    public static string NativeName(this ResolvedAction action) {
        var tag = CultureInfo.CurrentCulture.IetfLanguageTag;
        return action.Name.Get(tag) ?? action.Name.Get("en") ?? action.Action.PackageKey.Id;
    }

    public static string NativeName(this IDescriptor descriptor) {
        var tag = CultureInfo.CurrentCulture.IetfLanguageTag;
        var map = descriptor.Name;
        var name = map.Get(tag) ?? map.Get("en") ?? descriptor.Id;
        return name;
    }

    public static string NativeName(this LoadedRepository.IndexValue value) {
        var tag = CultureInfo.CurrentCulture.IetfLanguageTag;
        var map = value.Name;
        return map.Get(tag) ?? map.Get("en") ?? value.Url.ToString();
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