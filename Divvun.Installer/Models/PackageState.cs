using System;
using System.Collections.Generic;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;

namespace Divvun.Installer.Models
{
    public class PackageState : IEquatable<PackageState>
    {
        public PackageState() {
            SelectedPackages = new Dictionary<PackageKey, PackageAction>();
        }
        
        public Dictionary<PackageKey, PackageAction> SelectedPackages { get; internal set; }

        public bool Equals(PackageState? other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return SelectedPackages.Equals(other.SelectedPackages);
        }

        public override bool Equals(object? obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PackageState) obj);
        }

        public override int GetHashCode() {
            return SelectedPackages.GetHashCode();
        }
    }
}