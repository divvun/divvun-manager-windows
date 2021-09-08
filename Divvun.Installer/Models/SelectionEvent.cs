using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;

namespace Divvun.Installer.Models {

public interface ISelectionEvent {
}

namespace SelectionEvent {

internal struct AddSelectedPackage : ISelectionEvent {
    public PackageKey PackageKey;
    public PackageAction Action;
}

internal struct RemoveSelectedPackage : ISelectionEvent {
    public PackageKey PackageKey;
}

internal struct TogglePackage : ISelectionEvent {
    public PackageKey PackageKey;
    public PackageAction Action;
    public bool Value;
}

internal struct TogglePackageWithDefaultAction : ISelectionEvent {
    public PackageKey PackageKey;
    public bool Value;
}

internal struct ToggleGroupWithDefaultAction : ISelectionEvent {
    public PackageKey[] PackageKeys;
    public bool Value;
}

internal struct ToggleGroup : ISelectionEvent {
    public PackageAction[] PackageActions;
    public bool Value;
}

internal struct ResetSelection : ISelectionEvent {
}

internal struct SetPackages : ISelectionEvent {
    public PackageAction[] Actions;
}

}

}