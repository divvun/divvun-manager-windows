using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bahkat.Models;
using Bahkat.Service;

namespace Bahkat.UI.Settings
{
    public class SettingsWindowPresenter
    {
        private readonly ISettingsWindowView _view;
        private readonly RepositoryService _repoServ;
        private readonly AppConfigStore _config;
        
        public SettingsWindowPresenter(ISettingsWindowView view, RepositoryService repoServ, AppConfigStore config)
        {
            _view = view;
            _repoServ = repoServ;
            _config = config;
        }

        private IDisposable BindRepoStatus()
        {
            return _repoServ.System
                .Select(x => x.RepoResult)
                .DistinctUntilChanged()
                .Select(x =>
                {
                    if (x == null)
                    {
                        return Strings.Error;
                    }

                    if (x.Error != null)
                    {
                        return x.Error.Message;
                    }

                    if (x.Repository == null)
                    {
                        return Strings.Loading;
                    }

                    return x.Repository.Meta.NativeName;
                })
                .Subscribe(_view.SetRepositoryStatus, _view.HandleError);
        }

        private IDisposable BindSaveClicked()
        {
            return _view.OnSaveClicked()
                .Select(_ => _view.SettingsFormData())
                .Subscribe(data =>
                {
                    _config.Dispatch(AppConfigAction.SetInterfaceLanguage(data.InterfaceLanguage));
                    _config.Dispatch(AppConfigAction.SetUpdateCheckInterval(data.UpdateCheckInterval));
                    _config.Dispatch(AppConfigAction.SetRepositoryUrl(data.RepositoryUrl));
                    
                    _view.Close();
                });
        }
        
        public IDisposable Start()
        {
            _config.State.Take(1).Subscribe(x =>
            {
                _view.SetRepository(x.RepositoryUrl.AbsoluteUri);
                // HACK: we probably can't just use the language part of the tag forever.
                var langCode = x.InterfaceLanguage.Split('-')[0];
                _view.SetInterfaceLanguage(langCode);
                _view.SetUpdateFrequency(x.UpdateCheckInterval);
                _view.SetUpdateFrequencyStatus(x.NextUpdateCheck.ToLocalTime());
            });
            
            return new CompositeDisposable(
                BindRepoStatus(),
                _view.OnCancelClicked().Subscribe(_ => _view.Close()),
                BindSaveClicked());
        }
    }
}