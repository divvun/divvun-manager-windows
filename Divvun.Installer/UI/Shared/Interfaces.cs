﻿namespace Divvun.Installer.UI.Shared {

public interface IPageView {
}

public interface IWindowView {
    void Show();
}

public interface IWindowPageView : IWindowView {
    void ShowPage(IPageView pageView);
}

public interface IMainWindowView : IWindowPageView {
}

}