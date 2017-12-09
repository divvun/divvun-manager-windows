using System;
using System.Reactive.Concurrency;
using System.Reactive.Feedback;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Bahkat.UI.Main.MainWindowEvent;
using Bahkat.UI.Settings;

namespace Bahkat.UI.Main
{
    public interface IMainWindowView
    {
        void ShowPage(IPageView pageView);
        IObservable<EventArgs> OnShowSettingsClicked();
    }

    public class MainWindowState
    {
        public ISettingsWindowView SettingsWindow { get; internal set; }
        public IPageView CurrentPage { get; internal set; }

        static public MainWindowState Default()
        {
            return new MainWindowState
            {
                SettingsWindow = null,
                CurrentPage = new MainPage()
            };
        }
    }

    public interface IMainWindowEvent
    {
        
    }

    namespace MainWindowEvent
    {
        public class OpenSettingsWindow : IMainWindowEvent
        {
            
        }

        public class SetPage : IMainWindowEvent
        {
            public IPageView Page;

            public static SetPage Create(IPageView page)
            {
                return new SetPage()
                {
                    Page = page
                };
            }
        }
    }

    public interface IWindowRouter
    {
        void ShowPage(IPageView pageView);
    }

    public class MainWindowPresenter : IWindowRouter
    {
        private PackageStore _repoStore;
        internal Subject<IPageView> _pageSubject;
        internal IObservable<MainWindowState> System;
        
        public MainWindowPresenter(IMainWindowView mainWindow, PackageStore repoStore, Func<IWindowRouter, ISettingsWindowView> createSettingsWindow, IScheduler scheduler)
        {
            _repoStore = repoStore;
            
            System = Feedback.System(
                MainWindowState.Default(),
                (state, e) =>
                {
                    switch (e)
                    {
                        case SetPage v:
                            state.CurrentPage = v.Page;
                            return state;
                        case OpenSettingsWindow _:
                            if (state.SettingsWindow == null)
                            {
                                state.SettingsWindow = createSettingsWindow(this);
                            }
                            state.SettingsWindow.Show();
                            return state;
                    }
                    
                    return state;
                },
                scheduler,
                Feedback.Bind<MainWindowState, IMainWindowEvent>(state =>
                {
                    return new UIBindings<IMainWindowEvent>(new IDisposable[]
                    {
                        state.Select(s => s.CurrentPage)
                            .DistinctUntilChanged()
                            .Subscribe(mainWindow.ShowPage)
                    }, new IObservable<IMainWindowEvent>[]
                    {
                        mainWindow.OnShowSettingsClicked()
                            .Select(_ => new OpenSettingsWindow()),
                        _pageSubject.Select(page => new SetPage { Page = page })
                    });
                })
            );
        }

        public void ShowPage(IPageView pageView)
        {
            _pageSubject.OnNext(pageView);
        }
        
        void Start()
        {
            
        }
    }
}