﻿using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Castle.Core.Internal;
using Divvun.Installer.Extensions;
using Divvun.Installer.UI.Shared;
using Iterable;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Serilog;
using Iter = Iterable.Iterable;

namespace Divvun.Installer.UI.Main {

public interface IDownloadPageView : IPageView {
    void DownloadCancelled();
    void HandleError(Exception error);
}

/// <summary>
///     Interaction logic for DownloadPage.xaml
/// </summary>
public partial class DownloadPage : Page, IDownloadPageView, IDisposable {
    private readonly CompositeDisposable _bag = new CompositeDisposable();
    private NavigationService? _navigationService;

    // private void SetProgress(TransactionResponseValue.DownloadProgress progress) {
    //     SetProgress(progress.PackageKey, (long) progress.Current, (long) progress.Total);
    // }

    public DownloadPage() {
        InitializeComponent();
    }

    // Dispose

    public void Dispose() {
        Log.Verbose("Dispose called");
        _bag.Dispose();
    }

    public void DownloadCancelled() {
        BtnCancel.IsEnabled = false;
        LvPrimary.ItemsSource = null;
        this.ReplacePageWith(new MainPage());
    }

    public void HandleError(Exception error) {
        MessageBox.Show(error.Message, Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        // DownloadCancelled();
    }

    private void InitProgressList(ResolvedAction[] actions) {
        var x = actions
            .Filter(x => x.Action.Action == InstallAction.Install)
            .Map(x => new DownloadListItem(x.Action.PackageKey, x.NativeName(), x.Version));
        LvPrimary.ItemsSource = new ObservableCollection<DownloadListItem>(x);
    }

    private void SetProgress(PackageKey packageKey, long current, long total) {
        if (LvPrimary.ItemsSource == null) {
            return;
        }

        var source = (ObservableCollection<DownloadListItem>)LvPrimary.ItemsSource;
        if (source.IsNullOrEmpty()) {
            return;
        }

        var item = source.First(x => x.Key.Equals(packageKey));

        if (item != null) {
            Log.Verbose("{hash}: Setting progress for {package}: {current}/{total}", GetHashCode(),
                packageKey.ToString(), current, total);
            item.FileSize = total;
            item.Downloaded = current;
        }
    }

    private void OnClickCancel(object sender, RoutedEventArgs e) {
        //     var res = MessageBox.Show(
        //         Strings.CancelDownloadsBody,
        //         Strings.CancelDownloadsTitle,
        //         MessageBoxButton.YesNo,
        //         MessageBoxImage.Warning);
        //
        //     if (res != MessageBoxResult.Yes) {
        //         // TODO: something
        //         return;
        //     }
        //
        BtnCancel.IsEnabled = false;
    }

    // Page hacks

    private void Page_Loaded(object sender, RoutedEventArgs e) {
        var svc = NavigationService;
        if (svc != null) {
            _navigationService = svc;
            svc.Navigating += NavigationService_Navigating;
        }

        var app = (PahkatApp)Application.Current;

        if (app.CurrentTransaction.Value.IsT1 && app.CurrentTransaction.Value.AsT1.State.IsT0) {
            var actions = app.CurrentTransaction.Value.AsT1.Actions;

            // Try to initialise the downloads with the information we have
            InitProgressList(actions);
        }
        else {
            return;
        }

        // Control the state of the current view
        app.CurrentTransaction.AsObservable()
            // Resolve down the events to Download-related ones only
            .Filter(x => x.IsInProgressDownloading)
            .Map(x => x.AsInProgress!.State.AsDownloadState!.Progress)
            .ObserveOn(app.Dispatcher)
            .SubscribeOn(app.Dispatcher)
            .Subscribe(state => {
                foreach (var keyValuePair in state) {
                    SetProgress(keyValuePair.Key,
                        keyValuePair.Value.Item1,
                        keyValuePair.Value.Item2);
                }
            })
            .DisposedBy(_bag);
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e) {
        var svc = _navigationService;
        if (svc != null) {
            svc.Navigating -= NavigationService_Navigating;
        }

        _bag.Dispose();
    }

    private void NavigationService_Navigating(object sender, NavigatingCancelEventArgs e) {
        if (e.NavigationMode == NavigationMode.Back) {
            e.Cancel = true;
        }
    }
}

}