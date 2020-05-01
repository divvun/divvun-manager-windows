using System;
using Pahkat.Extensions;
using Pahkat.Sdk;

namespace Pahkat.Models
{
    public class PackageActionInfo : IEquatable<PackageActionInfo>
    {
        public readonly PackageKey PackageKey;
        public readonly PackageAction Action;

        public PackageActionInfo(PackageKey packageKey, PackageAction action) {
            PackageKey = packageKey;
            Action = action;
        }

        public PackageActionInfo(PackageKey packageKey) {
            PackageKey = packageKey;
            Action = packageKey.DefaultPackageAction();
        }

        public bool Equals(PackageActionInfo other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(PackageKey, other.PackageKey) && Action == other.Action;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PackageActionInfo) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return ((PackageKey != null ? PackageKey.GetHashCode() : 0) * 397) ^ (int) Action;
            }
        }
    }
}