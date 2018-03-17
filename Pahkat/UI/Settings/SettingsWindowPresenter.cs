using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Pahkat.Models;
using Pahkat.Service;

namespace Pahkat.UI.Settings
{
    public class SettingsWindowPresenter
    {
        private readonly ISettingsWindowView _view;
        //private readonly RepositoryService _repoServ;
        private readonly AppConfigStore _config;

        private ObservableCollection<RepoDataGridItem> _data;
        
        public SettingsWindowPresenter(ISettingsWindowView view, AppConfigStore config)
        {
            _view = view;
            //_repoServ = repoServ;
            _config = config;
        }

        private IDisposable BindAddRepo()
        {
            return _view.OnRepoAddClicked().Subscribe(_ =>
            {
                _data.Add(RepoDataGridItem.Empty);
                _view.SelectLastRow();
            });
        }

        private IDisposable BindRemoveRepo()
        {
            return _view.OnRepoRemoveClicked()
                .ObserveOn(DispatcherScheduler.Current)
                .SubscribeOn(DispatcherScheduler.Current)
                .Subscribe(index =>
                {
                    _data.RemoveAt(index);
                    _view.SelectRow(index);
                }, error =>
                {
                    throw error;
                });
        }

        private IDisposable BindSaveClicked()
        {
            return _view.OnSaveClicked()
                .Select(_ => _view.SettingsFormData())
                .Subscribe(data =>
                {
                    RepoConfig[] repos;
                    try
                    {
                        repos = _data.Select(x =>
                        {
                            return new RepoConfig(new Uri(x.Url), x.Channel);
                        }).ToArray();
                    } catch (Exception e)
                    {
                        _view.HandleError(e);
                        return;
                    }

                    _config.Dispatch(AppConfigAction.SetInterfaceLanguage(data.InterfaceLanguage));
                    _config.Dispatch(AppConfigAction.SetUpdateCheckInterval(data.UpdateCheckInterval));
                    _config.Dispatch(AppConfigAction.SetRepositories(repos));
                    
                    _view.Close();
                });
        }
        
        public IDisposable Start()
        {
            _config.State.Take(1).Subscribe(x =>
            {
                //_view.SetRepository(x.RepositoryUrl.AbsoluteUri);
                // TODO: fix hack
                // HACK: we probably can't just use the language part of the tag forever.
                var langCode = x.InterfaceLanguage.Split('-')[0];
                _view.SetInterfaceLanguage(langCode);
                _view.SetUpdateFrequency(x.UpdateCheckInterval);
                _view.SetUpdateFrequencyStatus(x.NextUpdateCheck.ToLocalTime());

                _data = new ObservableCollection<RepoDataGridItem>(
                    x.Repositories.Select(r => new RepoDataGridItem(r.Url.AbsoluteUri, r.Channel)));
                _view.SetRepoItemSource(_data);
            });
            
            return new CompositeDisposable(
                //BindRepoStatus(),
                BindAddRepo(),
                BindRemoveRepo(),
                _view.OnCancelClicked().Subscribe(_ => _view.Close()),
                BindSaveClicked());
        }
    }
}