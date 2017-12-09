using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Feedback;
using System.Reactive.Linq;
using Windows.UI.Xaml.Controls;
using Bahkat.Models.PackageManager;
using Bahkat.Service;

namespace Bahkat.UI.Main
{
    public interface IMainPageView : IPageView
    {
        IObservable<Package> OnPackageSelected();
        IObservable<Package> OnPackageDeselected();
        IObservable<EventArgs> OnPrimaryButtonPressed();
        void UpdateSelectedPackages(IEnumerable<Package> packages);
        void ShowDownloadPage();
        void UpdatePackageList(Repository repo);
    }
    
    public class MainPageState
    {
          
    }
    
    public class MainPagePresenter
    {
        internal IObservable<MainPageState> System;
        
        private MainPageState Reduce(MainPageState state, IMainPageEvent e)
        {
            switch (e)
            {
        
            }
            
            return state;
        }

        private IDisposable BindSelectedPackagesToView(IMainPageView view, PackageStore store)
        {
            return store.State
                .Select(s => s.SelectedPackages)
                .DistinctUntilChanged()
                .Subscribe(view.UpdateSelectedPackages);
        }

        private IDisposable BindAddSelectedPackage(IMainPageView view, PackageStore store)
        {
            return view.OnPackageSelected()
                .Select(RepositoryAction.AddSelectedPackage)
                .Subscribe(store.Dispatch);
        }
        
        private IDisposable BindRemoveSelectedPackage(IMainPageView view, PackageStore store)
        {
            return view.OnPackageDeselected()
                .Select(RepositoryAction.RemoveSelectedPackage)
                .Subscribe(store.Dispatch);
        }

        private IDisposable BindPrimaryButton(IMainPageView view)
        {
            return view.OnPrimaryButtonPressed()
                .Subscribe(_ => view.ShowDownloadPage());
        }

        private IDisposable BindUpdatePackageList(IMainPageView view, RepositoryService repoServ)
        {
            return repoServ.System.AsObservable()
                .Select(x =>
                {
                    Console.WriteLine("JJIJIJIJI");
                    return x.Repository;
                })
                .DistinctUntilChanged()
                .Select(x =>
                {
                    Console.WriteLine("DO A THING YOU SHIT");
                    return x;
                })
                .Subscribe(view.UpdatePackageList);
        }

        public MainPagePresenter(IMainPageView view, RepositoryService repoServ, PackageStore store, IScheduler scheduler)
        {
            System = Feedback.System(new MainPageState(),
                Reduce,
                scheduler,
                Feedback.Bind<MainPageState, IMainPageEvent>(state =>
                {
                    return new UIBindings<IMainPageEvent>(new[]
                    {
                        BindSelectedPackagesToView(view, store),
                        BindAddSelectedPackage(view, store),
                        BindRemoveSelectedPackage(view, store),
                        BindPrimaryButton(view),
                        BindUpdatePackageList(view, repoServ),
                        repoServ.System.ObserveOn(scheduler).SubscribeOn(scheduler).Subscribe(s => Console.WriteLine("LOLOL!!"))
                    }, new IObservable<IMainPageEvent>[]
                    {
                    });
                }));
        }
    }
    
    public class MainPage : Page, IMainPageView
    {
        public IObservable<Package> OnPackageSelected()
        {
            throw new NotImplementedException();
        }

        public IObservable<Package> OnPackageDeselected()
        {
            throw new NotImplementedException();
        }

        public IObservable<EventArgs> OnPrimaryButtonPressed()
        {
            throw new NotImplementedException();
        }

        public void UpdateSelectedPackages(IEnumerable<Package> packages)
        {
            throw new NotImplementedException();
        }

        public void ShowDownloadPage()
        {
            throw new NotImplementedException();
        }

        public void UpdatePackageList(Repository repo)
        {
            throw new NotImplementedException();
        }

        public void ShowPage(IPageView page)
        {
            throw new NotImplementedException();
        }
    }
}