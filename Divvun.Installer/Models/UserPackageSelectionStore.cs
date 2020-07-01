using System;
using Iterable;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Divvun.Installer.Models.SelectionEvent;
using Divvun.Installer.Util;
using Divvun.Installer.Extensions;

namespace Divvun.Installer.Models
{
    public interface IUserPackageSelectionStore : IStore<PackageState, ISelectionEvent>
    { }

    public class UserPackageSelectionStore : IUserPackageSelectionStore
    {
        private RxStore<PackageState, ISelectionEvent> _store;

        public UserPackageSelectionStore() {
            _store = new RxStore<PackageState, ISelectionEvent>(PackageState.Default(), Reduce);
        }

        public IObservable<PackageState> State => _store.State;

        public PackageState Value => _store.State.Take(1).ToTask().GetAwaiter().GetResult();

        public void Dispatch(ISelectionEvent e) {
            _store.Dispatch(e);
        }

        private PackageState Reduce(PackageState state, ISelectionEvent e) {
            switch (e) {
                case null:
                    return state;
                case SetPackages p:
                    var actions = p.Actions;
                    state.SelectedPackages.Clear();

                    foreach (var action in actions) {
                        state.SelectedPackages[action.PackageKey] = action;
                    }

                    break;
                case ResetSelection v:
                    state.SelectedPackages.Clear();
                    break;
                case AddSelectedPackage v:
                    if (!v.PackageKey.IsValidAction(v.Action)) {
                        break;
                    }

                    state.SelectedPackages[v.PackageKey] = v.Action;
                    break;
                case RemoveSelectedPackage v:
                    state.SelectedPackages.Remove(v.PackageKey);
                    break;
                case ToggleGroupWithDefaultAction v:
                    // Convert into an ordinary ToggleGroup
                    var evt = UserSelectionAction.ToggleGroup(
                        v.PackageKeys.Map(key => key.DefaultPackageAction()).ToArray(),
                        v.Value);
                    return Reduce(state, evt);
                case ToggleGroup v:
                    if (v.Value) {
                        var filtered = v.PackageActions.Filter((x) => x.PackageKey.IsValidAction(x));
                        foreach (var item in filtered) {
                            state.SelectedPackages[item.PackageKey] = item;
                        }
                    }
                    else {
                        foreach (var item in v.PackageActions) {
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
                    if (v.Value) {
                        if (!v.PackageKey.IsValidAction(v.Action)) {
                            break;
                        }

                        state.SelectedPackages[v.PackageKey] = v.Action;
                    }
                    else {
                        state.SelectedPackages.Remove(v.PackageKey);
                    }

                    break;
            }

            Console.WriteLine(string.Join(", ", state.SelectedPackages
                .Map(x => x.Value)
                .Map(x => $"{x.PackageKey.ToString()}:{x.Action}")));

            return state;
        }
    }
}