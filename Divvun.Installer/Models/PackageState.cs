using System;
using System.Collections.Generic;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;

namespace Divvun.Installer.Models
{
    public class PackageState : IEquatable<PackageState>
    {
        public Dictionary<PackageKey, PackageAction> SelectedPackages { get; private set; }
            = new Dictionary<PackageKey, PackageAction>();

        public bool Equals(PackageState other) {
            return Equals(SelectedPackages, other.SelectedPackages);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is PackageState && Equals((PackageState) obj);
        }
        public static PackageState Default() {
            return new PackageState {
                SelectedPackages = new Dictionary<PackageKey, PackageAction>()
            };
        }

        public static PackageState SelfUpdate(PackageKey packageKey) {
            return new PackageState {
                SelectedPackages = new Dictionary<PackageKey, PackageAction> {
                    {packageKey, new PackageAction(packageKey, InstallAction.Install)}
                }
            };
        }
    }
}