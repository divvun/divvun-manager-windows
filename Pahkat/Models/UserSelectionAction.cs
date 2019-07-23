using Pahkat.Sdk;
using Pahkat.Models.SelectionEvent;

namespace Pahkat.Models
{
    public static class UserSelectionAction
    {
        public static ISelectionEvent AddSelectedPackage(AbsolutePackageKey packageKey, PackageAction action)
        {
            return new AddSelectedPackage
            {
                PackageKey = packageKey,
                Action = action
            };
        }

        public static ISelectionEvent TogglePackage(AbsolutePackageKey packageKey, PackageAction action, bool value)
        {
            return new TogglePackage
            {
                PackageKey = packageKey,
                Action = action,
                Value = value
            };
        }

        public static ISelectionEvent TogglePackageWithDefaultAction(AbsolutePackageKey packageKey, bool value)
        {
            return new TogglePackageWithDefaultAction
            {
                PackageKey = packageKey,
                Value = value
            };
        }

        public static ISelectionEvent ToggleGroupWithDefaultAction(AbsolutePackageKey[] packageKeys, bool value)
        {
            return new ToggleGroupWithDefaultAction
            {
                PackageKeys = packageKeys,
                Value = value
            };
        }

        public static ISelectionEvent ToggleGroup(PackageActionInfo[] packageActions, bool value)
        {
            return new ToggleGroup
            {
                PackageActions = packageActions,
                Value = value
            };
        }

        public static ISelectionEvent ResetSelection => new ResetSelection();

        public static ISelectionEvent RemoveSelectedPackage(AbsolutePackageKey packageKey)
        {
            return new RemoveSelectedPackage
            {
                PackageKey = packageKey
            };
        }
    }
}