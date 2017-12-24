using System.Reactive.Concurrency;

namespace Bahkat.UI.Main
{
    public interface IWindowView
    {
        void Show();
    }

    public interface IWindowPageView : IWindowView
    {
        void ShowPage(IPageView pageView);
    }
    
    public interface IMainWindowView : IWindowPageView
    {
        void ShowMainPage();
    }
    
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