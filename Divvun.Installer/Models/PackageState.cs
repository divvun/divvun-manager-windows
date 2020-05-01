using System;
using System.Collections.Generic;
using Divvun.Installer.Sdk;

namespace Divvun.Installer.Models
{
    public struct PackageState : IEquatable<PackageState>
    {
        public bool Equals(PackageState other) {
            return Equals(SelectedPackages, other.SelectedPackages);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is PackageState && Equals((PackageState) obj);
        }

        public override int GetHashCode() {
            return (SelectedPackages != null ? SelectedPackages.GetHashCode() : 0);
        }

        public Dictionary<PackageKey, PackageActionInfo> SelectedPackages { get; private set; }

        public static PackageState Default() {
            return new PackageState {
                SelectedPackages = new Dictionary<PackageKey, PackageActionInfo>()
            };
        }

        public static PackageState SelfUpdate(PackageKey packageKey) {
            return new PackageState {
                SelectedPackages = new Dictionary<PackageKey, PackageActionInfo> {
                    {packageKey, new PackageActionInfo(packageKey, PackageAction.Install)}
                }
            };
        }
    }
}