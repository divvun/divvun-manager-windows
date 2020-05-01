using Divvun.Installer.Models.SelectionEvent;
using Divvun.Installer.Sdk;

namespace Divvun.Installer.Models
{
    public static class UserSelectionAction
    {
        public static ISelectionEvent AddSelectedPackage(PackageKey packageKey, PackageAction action) {
            return new AddSelectedPackage {
                PackageKey = packageKey,
                Action = action
            };
        }

        public static ISelectionEvent TogglePackage(PackageKey packageKey, PackageAction action, bool value) {
            return new TogglePackage {
                PackageKey = packageKey,
                Action = action,
                Value = value
            };
        }

        public static ISelectionEvent TogglePackageWithDefaultAction(PackageKey packageKey, bool value) {
            return new TogglePackageWithDefaultAction {
                PackageKey = packageKey,
                Value = value
            };
        }

        public static ISelectionEvent ToggleGroupWithDefaultAction(PackageKey[] packageKeys, bool value) {
            return new ToggleGroupWithDefaultAction {
                PackageKeys = packageKeys,
                Value = value
            };
        }

        public static ISelectionEvent ToggleGroup(PackageActionInfo[] packageActions, bool value) {
            return new ToggleGroup {
                PackageActions = packageActions,
                Value = value
            };
        }

        public static ISelectionEvent ResetSelection => new ResetSelection();

        public static ISelectionEvent RemoveSelectedPackage(PackageKey packageKey) {
            return new RemoveSelectedPackage {
                PackageKey = packageKey
            };
        }
    }
}