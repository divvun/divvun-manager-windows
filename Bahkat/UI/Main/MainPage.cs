using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Feedback;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using Bahkat.Extensions;
using Bahkat.Models;
using Bahkat.Service;
using Bahkat.UI.Shared;

namespace Bahkat.UI.Main
{
    public interface IMainPageView : IPageView
    {
        IObservable<Package> OnPackageSelected();
        IObservable<Package> OnPackageDeselected();
        IObservable<EventArgs> OnPrimaryButtonPressed();
        void UpdateTitle(string title);
        void SetPackagesModel(ObservableCollection<PackageMenuItem> model);
        void ShowDownloadPage();
        void UpdatePrimaryButton(bool isEnabled, string label);
        void SetPackageFilter<T>() where T : class, IValueConverter, new();
        void HandleError(Exception error);
    }

    public class MainPagePresenter
    {
        private ObservableItemList<PackageMenuItem> _listItems =
            new ObservableItemList<PackageMenuItem>();
        private IMainPageView _view;
        private RepositoryService _repoServ;
        private PackageService _pkgServ;
        private PackageStore _store;

        private Repository _currentRepo;
        
        private IDisposable BindFilter(IMainPageView view, RepositoryService repoServ)
        {
            return repoServ.System
                .Where(x => x.RepoResult?.Repository != null)
                .Select(x => x.RepoResult.Repository.Meta.PrimaryFilter)
                .DistinctUntilChanged()
                .Subscribe(filter =>
                {
                    switch (filter)
                    {
                        case "category":
                            view.SetPackageFilter<CategoryFilter>();
                            break;
                        case "language":
                            view.SetPackageFilter<LanguageFilter>();
                            break;
                    }
                });
        }

        private IDisposable BindAddSelectedPackage(IMainPageView view, PackageStore store)
        {
            return view.OnPackageSelected()
                .Select(PackageAction.AddSelectedPackage)
                .Subscribe(store.Dispatch);
        }
        
        private IDisposable BindRemoveSelectedPackage(IMainPageView view, PackageStore store)
        {
            return view.OnPackageDeselected()
                .Select(PackageAction.RemoveSelectedPackage)
                .Subscribe(store.Dispatch);
        }

        private IDisposable BindPrimaryButton(IMainPageView view)
        {
            return view.OnPrimaryButtonPressed()
                .Subscribe(_ => view.ShowDownloadPage());
        }

        private void RefreshPackageList()
        {
            _listItems.Clear();

            if (_currentRepo == null)
            {
                Console.WriteLine("Repository empty.");
                return;
            }
            
            // The package items should probably have a listener for registry changes and amend themselves when their parent changes
            foreach (var item in _currentRepo.PackagesIndex.Values.Select(x => new PackageMenuItem(x, _pkgServ, _store)))
            {
                _listItems.Add(item);
            }
            
            Console.WriteLine("Added packages.");
        }

        private IDisposable BindUpdatePackageList(RepositoryService repoServ, PackageService pkgServ, PackageStore store)
        {
            return repoServ.System
                .Where(x => x.RepoResult?.Repository != null)
                .Select(x => x.RepoResult.Repository)
                .DistinctUntilChanged()
                .Subscribe(repo =>
                {
                    _currentRepo = repo;
                    RefreshPackageList();
                }, _view.HandleError);
        }

        private IDisposable BindPrimaryButton(IMainPageView view, PackageStore store)
        {
            return store.State
                .Select(state => state.SelectedPackages)
                .Subscribe(packages =>
                {
                    if (packages.Count > 0)
                    {
                        view.UpdatePrimaryButton(true, string.Format(Strings.InstallNPackages, packages.Count));
                    }
                    else
                    {
                        view.UpdatePrimaryButton(false, Strings.NoPackagesSelected);
                    }
                });
        }

        public MainPagePresenter(IMainPageView view, RepositoryService repoServ, PackageService pkgServ, PackageStore store)
        {
            _repoServ = repoServ;
            _pkgServ = pkgServ;
            _view = view;
            _store = store;
        }

        public IDisposable Start()
        {
            _view.SetPackagesModel(_listItems);

            return new CompositeDisposable(
                BindPrimaryButton(_view, _store),
                BindUpdatePackageList(_repoServ, _pkgServ, _store),
                BindAddSelectedPackage(_view, _store),
                BindRemoveSelectedPackage(_view, _store),
                BindPrimaryButton(_view),
                BindFilter(_view, _repoServ)
            );
        }
    }
}