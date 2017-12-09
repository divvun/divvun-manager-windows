using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Feedback;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Bahkat.MainPageEvent;
using Bahkat.Models.PackageManager;
using Bahkat.RepositoryEvent;
using Bahkat.UI.Main;

namespace Bahkat
{
     public struct PackageState
    {
        public HashSet<Package> SelectedPackages { get; private set; }

        public static PackageState Default()
        {
            var state = new PackageState {SelectedPackages = new HashSet<Package>()};
            return state;
        }
        
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

    public interface IRepositoryEvent : IStoreEvent { }

    public static class RepositoryAction
    {
        public static IRepositoryEvent AddSelectedPackage(Package package)
        {
            return new AddSelectedPackage
            {
                Package = package
            };
        }
        
        public static IRepositoryEvent RemoveSelectedPackage(Package package)
        {
            return new RemoveSelectedPackage
            {
                Package = package
            };
        }
    }
    
    namespace RepositoryEvent
    {
        public struct AddSelectedPackage : IRepositoryEvent
        {
            public Package Package;
        }
        
        public struct RemoveSelectedPackage : IRepositoryEvent
        {
            public Package Package;
        }
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
            Console.WriteLine(e);
            _dispatcher.OnNext(e);
        }

        public RxStore(TState initialState, params Func<TState, IStoreEvent, TState>[] reducers)
        {
            State = Feedback.System(
                initialState,
                (i, e) => reducers.Aggregate(i, (state, next) => next(state, e)),
                _ => _dispatcher.AsObservable());
        }
    }
    
    public class PackageStore : RxStore<PackageState>
    {
        private static PackageState Reduce(PackageState state, IStoreEvent e)
        {
            Console.WriteLine(e);
            
            switch (e as IRepositoryEvent)
            {
                case null:
                    return state;
                case AddSelectedPackage v:
                    Console.WriteLine(v);
                    state.SelectedPackages.Add(v.Package);
                    break;
            }

            return state;
        }

        public PackageStore() : base(PackageState.Default(), Reduce) {}
    }
}