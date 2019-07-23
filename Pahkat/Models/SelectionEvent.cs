using Pahkat.Sdk;

namespace Pahkat.Models
{
    public interface ISelectionEvent { }

    namespace SelectionEvent
    {
        internal struct AddSelectedPackage : ISelectionEvent
        {
            public AbsolutePackageKey PackageKey;
            public PackageAction Action;
        }

        internal struct RemoveSelectedPackage : ISelectionEvent
        {
            public AbsolutePackageKey PackageKey;
        }

        internal struct TogglePackage : ISelectionEvent
        {
            public AbsolutePackageKey PackageKey;
            public PackageAction Action;
            public bool Value;
        }

        internal struct TogglePackageWithDefaultAction : ISelectionEvent
        {
            public AbsolutePackageKey PackageKey;
            public bool Value;
        }

        internal struct ToggleGroupWithDefaultAction : ISelectionEvent
        {
            public AbsolutePackageKey[] PackageKeys;
            public bool Value;
        }

        internal struct ToggleGroup : ISelectionEvent
        {
            public PackageActionInfo[] PackageActions;
            public bool Value;
        }

        internal struct ResetSelection : ISelectionEvent { }
    }
}