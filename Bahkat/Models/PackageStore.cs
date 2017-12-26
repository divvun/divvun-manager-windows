using System;
using System.Collections.Generic;
using System.Linq;
using Bahkat.Models.PackageEvent;
using Bahkat.Service;
using Bahkat.Util;

namespace Bahkat.Models
{
    public enum PackageAction
    {
        Install,
        Uninstall
    }
    
    public class PackageActionInfo : IEquatable<PackageActionInfo>
    {
        public Package Package;
        public PackageAction Action;

        public bool Equals(PackageActionInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Package, other.Package) && Action == other.Action;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PackageActionInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Package != null ? Package.GetHashCode() : 0) * 397) ^ (int) Action;
            }
        }
    }

    public struct PackageState : IEquatable<PackageState>
    {
        public bool Equals(PackageState other)
        {
            return Equals(SelectedPackages, other.SelectedPackages);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is PackageState && Equals((PackageState) obj);
        }

        public override int GetHashCode()
        {
            return (SelectedPackages != null ? SelectedPackages.GetHashCode() : 0);
        }

        public Dictionary<Package, PackageActionInfo> SelectedPackages { get; private set; }

        public static PackageState Default()
        {
            var state = new PackageState
            {
                SelectedPackages = new Dictionary<Package, PackageActionInfo>()
            };
            return state;
        }
    }

    public interface IPackageEvent { }

    public static class PackageStoreAction
    {
        public static IPackageEvent AddSelectedPackage(Package package, PackageAction action)
        {
            return new AddSelectedPackage
            {
                Package = package,
                Action = action
            };
        }

        public static IPackageEvent TogglePackage(Package package, PackageAction action, bool value)
        {
            return new TogglePackage
            {
                Package = package,
                Action = action,
                Value = value
            };
        }
        
        public static IPackageEvent TogglePackageWithDefaultAction(Package package, bool value)
        {
            return new TogglePackageWithDefaultAction
            {
                Package = package,
                Value = value
            };
        }
        
        public static IPackageEvent ToggleGroupWithDefaultAction(Package[] packages, bool value)
        {
            return new ToggleGroupWithDefaultAction
            {
                Packages = packages,
                Value = value
            };
        }

        public static IPackageEvent ToggleGroup(PackageActionInfo[] packageActions, bool value)
        {
            return new ToggleGroup
            {
                PackageActions = packageActions,
                Value = value
            };
        }

        public static IPackageEvent ResetSelection => new ResetSelection();

        public static IPackageEvent RemoveSelectedPackage(Package package)
        {
            return new RemoveSelectedPackage
            {
                Package = package
            };
        }
    }
    
    namespace PackageEvent
    {
        internal struct AddSelectedPackage : IPackageEvent
        {
            public Package Package;
            public PackageAction Action;
        }
        
        internal struct RemoveSelectedPackage : IPackageEvent
        {
            public Package Package;
        }

        internal struct TogglePackage : IPackageEvent
        {
            public Package Package;
            public PackageAction Action;
            public bool Value;
        }
        
        internal struct TogglePackageWithDefaultAction : IPackageEvent
        {
            public Package Package;
            public bool Value;
        }
        
        internal struct ToggleGroupWithDefaultAction : IPackageEvent
        {
            public Package[] Packages;
            public bool Value;
        }

        internal struct ToggleGroup : IPackageEvent
        {
            public PackageActionInfo[] PackageActions;
            public bool Value;
        }

        internal struct ResetSelection : IPackageEvent {}
    }
    
    public class PackageStore: IStore<PackageState, IPackageEvent>
    {
        private RxStore<PackageState, IPackageEvent> _store;
        private IPackageService _pkgServ;
        
        public IObservable<PackageState> State => _store.State;

        public void Dispatch(IPackageEvent e)
        {
            _store.Dispatch(e);
        }

        private PackageState Reduce(PackageState state, IPackageEvent e)
        {   
            switch (e)
            {
                case null:
                    return state;
                case ResetSelection v:
                    state.SelectedPackages.Clear();
                    break;
                case AddSelectedPackage v:
                    if (!_pkgServ.IsValidAction(v.Package, v.Action))
                    {
                        break;
                    }
                    
                    state.SelectedPackages[v.Package] = new PackageActionInfo
                    {
                        Package = v.Package,
                        Action = v.Action
                    };
                    break;
                case RemoveSelectedPackage v:
                    state.SelectedPackages.Remove(v.Package);
                    break;
                case ToggleGroupWithDefaultAction v:
                    // Convert into an ordinary ToggleGroup
                    return Reduce(state, PackageStoreAction.ToggleGroup(v.Packages.Select(pkg => new PackageActionInfo
                    {
                        Package = pkg,
                        Action = _pkgServ.DefaultPackageAction(pkg)
                    }).ToArray(), v.Value));
                case ToggleGroup v:
                    if (v.Value)
                    {
                        foreach (var item in v.PackageActions.Where(_pkgServ.IsValidAction))
                        {
                            state.SelectedPackages[item.Package] = item;
                        }
                    }
                    else
                    {
                        foreach (var item in v.PackageActions)
                        {
                            state.SelectedPackages.Remove(item.Package);
                        }
                    }
                    break;
                case TogglePackageWithDefaultAction v:
                    // Convert into an ordinary TogglePackage
                    return Reduce(state, PackageStoreAction.TogglePackage(v.Package,
                        _pkgServ.DefaultPackageAction(v.Package),
                        v.Value));
                case TogglePackage v:
                    if (v.Value)
                    {
                        if (!_pkgServ.IsValidAction(v.Package, v.Action))
                        {
                            break;
                        }
                        
                        state.SelectedPackages[v.Package] = new PackageActionInfo
                        {
                            Package = v.Package,
                            Action = v.Action
                        };
                    }
                    else
                    {
                        state.SelectedPackages.Remove(v.Package);
                    }
                    break;
            }
            
            Console.WriteLine(string.Join(", ", state.SelectedPackages
                .Select(x => x.Value)
                .Select(x => $"{x.Package.Id}:{x.Action}")));
            
            return state;
        }

        public PackageStore(IPackageService pkgServ)
        {
            _pkgServ = pkgServ;
            _store = new RxStore<PackageState, IPackageEvent>(PackageState.Default(), Reduce);
        }
    }
}