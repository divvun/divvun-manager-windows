using System.Reactive.Concurrency;
using Pahkat.UI.Shared;

namespace Pahkat.UI.Main
{
    public class MainWindowPresenter
    {
        private readonly IMainWindowView _mainWindow;
        
        public MainWindowPresenter(IMainWindowView mainWindow, IScheduler scheduler)
        {
            _mainWindow = mainWindow;
        }

        public void ShowPage(IPageView pageView)
        {
            _mainWindow.ShowPage(pageView);
        }
        
        public void Start()
        {
            _mainWindow.ShowMainPage();
        }
    }
}