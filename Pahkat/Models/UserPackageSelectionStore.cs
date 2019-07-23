using System;
using System.Linq;
using Pahkat.Util;
using Pahkat.Models.SelectionEvent;
using Pahkat.Extensions;

namespace Pahkat.Models
{
    public interface IUserPackageSelectionStore : IStore<PackageState, ISelectionEvent> { }

    public class UserPackageSelectionStore : IUserPackageSelectionStore
    {
        private RxStore<PackageState, ISelectionEvent> _store;

        public UserPackageSelectionStore()
        {
            _store = new RxStore<PackageState, ISelectionEvent>(PackageState.Default(), Reduce);
        }

        public IObservable<PackageState> State => _store.State;

        public void Dispatch(ISelectionEvent e)
        {
            _store.Dispatch(e);
        }

        private PackageState Reduce(PackageState state, ISelectionEvent e)
        {
            switch (e)
            {
                case null:
                    return state;
                case ResetSelection v:
                    state.SelectedPackages.Clear();
                    break;
                case AddSelectedPackage v:
                    if (!v.PackageKey.IsValidAction(v.Action))
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
                    return Reduce(state, UserSelectionAction.ToggleGroup(v.PackageKeys
                        .Select(pkg => new PackageActionInfo(pkg))
                        .ToArray(), v.Value));
                case ToggleGroup v:
                    if (v.Value)
                    {
                        var filtered = v.PackageActions.Where((x) => x.PackageKey.IsValidAction(x.Action));
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
                    return Reduce(state, UserSelectionAction.TogglePackage(v.PackageKey,
                        v.PackageKey.DefaultPackageAction(),
                        v.Value));
                case TogglePackage v:
                    if (v.Value)
                    {
                        if (!v.PackageKey.IsValidAction(v.Action))
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
    }
}