using System;
using Newtonsoft.Json;

namespace Pahkat.Sdk.Rpc {

public enum InstallAction : byte {
    Install = 0,
    Uninstall,
}

public enum InstallTarget : byte {
    System = 0,
}

public class PackageAction : IEquatable<PackageAction> {
    public readonly InstallAction Action;

    [JsonProperty(PropertyName = "id")] public readonly PackageKey PackageKey;

    public readonly InstallTarget Target;

    public PackageAction(
        PackageKey packageKey,
        InstallAction instAction,
        InstallTarget instTarget = InstallTarget.System
    ) {
        PackageKey = packageKey;
        Action = instAction;
        Target = instTarget;
    }

    public bool Equals(PackageAction? other) {
        if (ReferenceEquals(null, other)) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return PackageKey.Equals(other.PackageKey) && Action == other.Action && Target == other.Target;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) {
            return false;
        }

        if (ReferenceEquals(this, obj)) {
            return true;
        }

        if (obj.GetType() != GetType()) {
            return false;
        }

        return Equals((PackageAction)obj);
    }

    public override int GetHashCode() {
        unchecked {
            var hashCode = PackageKey.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)Action;
            hashCode = (hashCode * 397) ^ (int)Target;
            return hashCode;
        }
    }
}

}