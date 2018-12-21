using System;
using System.Collections.Generic;
using System.Linq;
using Pahkat.Models.PackageEvent;
using Pahkat.Service;
using Pahkat.Service.CoreLib;
using Pahkat.Util;

namespace Pahkat.Models
{
    public enum PackageActionType: byte
    {
        Install = 0,
        Uninstall
    }
    
    public static class PackageActionTypeExtensions
    {
        public static byte ToByte(this PackageActionType action)
        {
            switch (action)
            {
                case PackageActionType.Install:
                    return 0;
                case PackageActionType.Uninstall:
                    return 1;
                default:
                    return 255;
            }
        }
    }
    
    public enum PackageStatus
    {
        NotInstalled,
        UpToDate,
        RequiresUpdate,
        VersionSkipped,
        ErrorNoInstaller,
        ErrorParsingVersion
    }

    public static class PackageInstallStatusExtensions
    {
        public static string Description(this PackageStatus status)
        {
            switch (status)
            {
                case PackageStatus.ErrorNoInstaller:
                    return Strings.ErrorNoInstaller;
                case PackageStatus.ErrorParsingVersion:
                    return Strings.ErrorInvalidVersion;
                case PackageStatus.RequiresUpdate:
                    return Strings.UpdateAvailable;
                case PackageStatus.NotInstalled:
                    return Strings.NotInstalled;
                case PackageStatus.UpToDate:
                    return Strings.Installed;
                case PackageStatus.VersionSkipped:
                    return Strings.VersionSkipped;
            }

            return null;
        }
    }

    
    public class PackageActionInfo : IEquatable<PackageActionInfo>
    {
        public readonly AbsolutePackageKey PackageKey;
        public readonly PackageActionType Action;

        public PackageActionInfo(AbsolutePackageKey packageKey, PackageActionType action)
        {
            PackageKey = packageKey;
            Action = action;
        }

        public bool Equals(PackageActionInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(PackageKey, other.PackageKey) && Action == other.Action;
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
                return ((PackageKey != null ? PackageKey.GetHashCode() : 0) * 397) ^ (int) Action;
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

        public Dictionary<AbsolutePackageKey, PackageActionInfo> SelectedPackages { get; private set; }

        public static PackageState Default()
        {
            return new PackageState
            {
                SelectedPackages = new Dictionary<AbsolutePackageKey, PackageActionInfo>()
            };
        }

        public static PackageState SelfUpdate(AbsolutePackageKey packageKey)
        {
            return new PackageState
            {
                SelectedPackages = new Dictionary<AbsolutePackageKey, PackageActionInfo>
                {
                    { packageKey, new PackageActionInfo(packageKey, PackageActionType.Install) }
                }
            };
        }
    }

    public interface IPackageEvent { }

    public static class PackageStoreAction
    {
        public static IPackageEvent AddSelectedPackage(AbsolutePackageKey packageKey, PackageActionType action)
        {
            return new AddSelectedPackage
            {
                PackageKey = packageKey,
                Action = action
            };
        }

        public static IPackageEvent TogglePackage(AbsolutePackageKey packageKey, PackageActionType action, bool value)
        {
            return new TogglePackage
            {
                PackageKey = packageKey,
                Action = action,
                Value = value
            };
        }
        
        public static IPackageEvent TogglePackageWithDefaultAction(AbsolutePackageKey packageKey, bool value)
        {
            return new TogglePackageWithDefaultAction
            {
                PackageKey = packageKey,
                Value = value
            };
        }
        
        public static IPackageEvent ToggleGroupWithDefaultAction(AbsolutePackageKey[] packageKeys, bool value)
        {
            return new ToggleGroupWithDefaultAction
            {
                PackageKeys = packageKeys,
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

        public static IPackageEvent RemoveSelectedPackage(AbsolutePackageKey packageKey)
        {
            return new RemoveSelectedPackage
            {
                PackageKey = packageKey
            };
        }
    }
    
    namespace PackageEvent
    {
        internal struct AddSelectedPackage : IPackageEvent
        {
            public AbsolutePackageKey PackageKey;
            public PackageActionType Action;
        }
        
        internal struct RemoveSelectedPackage : IPackageEvent
        {
            public AbsolutePackageKey PackageKey;
        }

        internal struct TogglePackage : IPackageEvent
        {
            public AbsolutePackageKey PackageKey;
            public PackageActionType Action;
            public bool Value;
        }
        
        internal struct TogglePackageWithDefaultAction : IPackageEvent
        {
            public AbsolutePackageKey PackageKey;
            public bool Value;
        }
        
        internal struct ToggleGroupWithDefaultAction : IPackageEvent
        {
            public AbsolutePackageKey[] PackageKeys;
            public bool Value;
        }

        internal struct ToggleGroup : IPackageEvent
        {
            public PackageActionInfo[] PackageActions;
            public bool Value;
        }

        internal struct ResetSelection : IPackageEvent {}
    }
    
    public interface IPackageStore : IStore<PackageState, IPackageEvent> {}
    
    public class PackageStore: IPackageStore
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
                    if (!_pkgServ.IsValidAction(v.PackageKey, v.Action))
                    {
                        break;
                    }

                    state.SelectedPackages[v.PackageKey] = new PackageActionInfo(v.PackageKey, v.Action);
                    break;
                case RemoveSelectedPackage v:
                    state.SelectedPackages.Remove(v.PackageKey);
                    break;
                case ToggleGroupWithDefaultAction v:
                    // Convert into an ordinary ToggleGroup
                    return Reduce(state, PackageStoreAction.ToggleGroup(v.PackageKeys
                        .Select(pkg => new PackageActionInfo(pkg, _pkgServ.DefaultPackageAction(pkg)))
                        .ToArray(), v.Value));
                case ToggleGroup v:
                    if (v.Value)
                    {
                        var filtered = v.PackageActions.Where((x) => _pkgServ.IsValidAction(x.PackageKey, x.Action));
                        foreach (var item in filtered)
                        {
                            state.SelectedPackages[item.PackageKey] = item;
                        }
                    }
                    else
                    {
                        foreach (var item in v.PackageActions)
                        {
                            state.SelectedPackages.Remove(item.PackageKey);
                        }
                    }
                    break;
                case TogglePackageWithDefaultAction v:
                    // Convert into an ordinary TogglePackage
                    return Reduce(state, PackageStoreAction.TogglePackage(v.PackageKey,
                        _pkgServ.DefaultPackageAction(v.PackageKey),
                        v.Value));
                case TogglePackage v:
                    if (v.Value)
                    {
                        if (!_pkgServ.IsValidAction(v.PackageKey, v.Action))
                        {
                            break;
                        }

                        state.SelectedPackages[v.PackageKey] = new PackageActionInfo(v.PackageKey, v.Action);
                    }
                    else
                    {
                        state.SelectedPackages.Remove(v.PackageKey);
                    }
                    break;
            }
            
            Console.WriteLine(string.Join(", ", state.SelectedPackages
                .Select(x => x.Value)
                .Select(x => $"{x.PackageKey.ToString()}:{x.Action}")));
            
            return state;
        }

        public PackageStore(IPackageService pkgServ)
        {
            _pkgServ = pkgServ;
            _store = new RxStore<PackageState, IPackageEvent>(PackageState.Default(), Reduce);
        }
    }
}