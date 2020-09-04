// using System;
// using System.Collections.ObjectModel;
// using System.Reactive.Concurrency;
// using System.Reactive.Disposables;
// using System.Reactive.Linq;
//
// namespace Divvun.Installer.UI.Settings
// {
//     public class SettingsWindowPresenter
//     {
//         private readonly ISettingsWindowView _view;
//         private ObservableCollection<RepoDataGridItem> _data;
//
//         public SettingsWindowPresenter(ISettingsWindowView view) {
//             _view = view;
//         }
//
//         private IDisposable BindAddRepo() {
//             return _view.OnRepoAddClicked().Subscribe(_ => {
//                 _data.Add(RepoDataGridItem.Empty);
//                 _view.MapLastRow();
//             });
//         }
//
//         private IDisposable BindRemoveRepo() {
//             return _view.OnRepoRemoveClicked()
//                 .ObserveOn(app.Dispatcher)
//                 .SubscribeOn(app.Dispatcher)
//                 .Subscribe(index => {
//                     _data.RemoveAt(index);
//                     _view.MapRow(index);
//                 }, error => { throw error; });
//         }
//
//         // private IDisposable BindSaveClicked() {
//         //     return _view.OnSaveClicked()
//         //         .Map(_ => _view.SettingsFormData())
//         //         .Subscribe(data => {
//         //             List<RepoRecord> repos;
//         //             try {
//         //                 repos = _data.Map(x => { return new RepoRecord(new Uri(x.Url), x.Channel); }).ToList();
//         //             }
//         //             catch (Exception e) {
//         //                 _view.HandleError(e);
//         //                 return;
//         //             }
//         //
//         //             _config.Dispatch(AppConfigAction.SetInterfaceLanguage(data.InterfaceLanguage));
//         //             // _config.Dispatch(AppConfigAction.SetUpdateCheckInterval(data.UpdateCheckInterval));
//         //             _config.Dispatch(AppConfigAction.SetRepositories(repos));
//         //
//         //             _view.Close();
//         //         });
//         // }
//
//         // private IDisposable InitInterface() {
//         //     return _config.State.Take(1).Subscribe(x => {
//         //         _view.SetInterfaceLanguage(x.InterfaceLanguage);
//         //         _data = new ObservableCollection<RepoDataGridItem>(
//         //             x.Repositories.Map(r => new RepoDataGridItem(r.Url.AbsoluteUri, r.Channel)));
//         //         _view.SetRepoItemSource(_data);
//         //     });
//         // }
//
//         public IDisposable Start() {
//             return new CompositeDisposable(
//                 // InitInterface(),
//                 //BindRepoStatus(),
//                 BindAddRepo(),
//                 BindRemoveRepo(),
//                 _view.OnCancelClicked().Subscribe(_ => _view.Close())
//                 // BindSaveClicked()
//                 );
//         }
//     }
// }