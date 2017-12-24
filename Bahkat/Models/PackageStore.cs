using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Feedback;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Bahkat.Models.MainPageEvent;
using Bahkat.Models.PackageEvent;
using Newtonsoft.Json;

namespace Bahkat.Models
{
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

        public HashSet<Package> SelectedPackages { get; private set; }

        public static PackageState Default()
        {
            var state = new PackageState {SelectedPackages = new HashSet<Package>()};
            return state;
        }
    }
    
    public interface IMainPageEvent : IStoreEvent { }
    
    public static class MainPageAction
    {
        public static IMainPageEvent SetRepository(Repository repo)
        {
            return new SetRepository()
            {
                Repository = repo
            };
        }
        public static IMainPageEvent ProcessSelectedPackages()
        {
            return new ProcessSelectedPackages();
        }
    }

    namespace MainPageEvent
    {
        public class ProcessSelectedPackages : IMainPageEvent { }

        public class SetRepository : IMainPageEvent
        {
            public Repository Repository;
        }
    }

    public interface IPackageEvent : IStoreEvent { }

    public static class PackageAction
    {
        public static IPackageEvent AddSelectedPackage(Package package)
        {
            return new AddSelectedPackage
            {
                Package = package
            };
        }

        public static IPackageEvent TogglePackage(Package package, bool value)
        {
            return new TogglePackage
            {
                Package = package,
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
        public struct AddSelectedPackage : IPackageEvent
        {
            public Package Package;
        }
        
        public struct RemoveSelectedPackage : IPackageEvent
        {
            public Package Package;
        }

        public struct TogglePackage : IPackageEvent
        {
            public Package Package;
            public bool Value;
        }
        
        public struct ResetSelection : IPackageEvent {}
    }

    public interface IStoreEvent
    {
    }

    public class RxStore<TState>
    {
        public IObservable<TState> State { get; private set; }
        private readonly Subject<IStoreEvent> _dispatcher = new Subject<IStoreEvent>();

        public void Dispatch(IStoreEvent e)
        {
            _dispatcher.OnNext(e);
        }

        public RxStore(TState initialState, params Func<TState, IStoreEvent, TState>[] reducers)
        {
            State = Feedback.System(
                initialState,
                (i, e) => reducers.Aggregate(i, (state, next) => next(state, e)),
                _ => _dispatcher.AsObservable())
                .Replay(1)
                .RefCount();
        }
    }
    
    public class PackageStore : RxStore<PackageState>
    {
        private static PackageState Reduce(PackageState state, IStoreEvent e)
        {   
            Console.WriteLine(e);
            
            switch (e as IPackageEvent)
            {
                case null:
                    return state;
                case ResetSelection v:
                    state.SelectedPackages.Clear();
                    break;
                case AddSelectedPackage v:
                    state.SelectedPackages.Add(v.Package);
                    break;
                case RemoveSelectedPackage v:
                    state.SelectedPackages.Remove(v.Package);
                    break;
                case TogglePackage v:
                    if (v.Value)
                    {
                        state.SelectedPackages.Add(v.Package);
                    }
                    else
                    {
                        state.SelectedPackages.Remove(v.Package);
                    }
                    break;
            }
            
            Console.WriteLine(JsonConvert.SerializeObject(state.SelectedPackages.Select(x => x.Id).ToArray()));

            return state;
        }

        public PackageStore() : base(PackageState.Default(), Reduce) {}
    }
}