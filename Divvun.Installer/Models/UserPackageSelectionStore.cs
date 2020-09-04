using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Divvun.Installer.Extensions;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;

namespace Divvun.Installer.Models
{
    
    public class UserPackageSelectionStore : IDisposable
    {
        private BehaviorSubject<PackageState> _state = new BehaviorSubject<PackageState>(new PackageState());
        public PackageState State => _state.Value;
        public IObservable<PackageState> Observe() => _state.AsObservable();
        public IObservable<Dictionary<PackageKey, PackageAction>> SelectedPackages() => Observe().Map(state => state.SelectedPackages);

        public async Task ToggleGroupWithDefaultAction(PackageKey[] keys, bool value) {
            var actions = new List<PackageAction>();
            foreach (var key in keys) {
                actions.Add(await key.DefaultPackageAction());
            }

            var selectedPackages = State.SelectedPackages;

            if (value) {
                foreach (var action in actions) {
                    if (await action.PackageKey.IsValidAction(action)) {
                        selectedPackages[action.PackageKey] = action;
                    }
                }
            } else {
                foreach (var action in actions) {
                    selectedPackages.Remove(action.PackageKey);
                }
            }

            var state = State;
            state.SelectedPackages = selectedPackages;
            _state.OnNext(state);
        }

        public async Task TogglePackageWithDefaultAction(PackageKey key, bool value) {
            var action = await key.DefaultPackageAction();

            var selectedPackages = State.SelectedPackages;

            if (value) {
                if (await action.PackageKey.IsValidAction(action)) {
                    selectedPackages[action.PackageKey] = action;
                }
            } else {
                selectedPackages.Remove(action.PackageKey);
            }

            var state = State;
            state.SelectedPackages = selectedPackages;
            _state.OnNext(state);
        }
        
        public void ResetSelection() {
            var state = State;
            state.SelectedPackages.Clear();
            _state.OnNext(state);
        }

        public void Dispose() {
            _state.Dispose();
        }
    }
}