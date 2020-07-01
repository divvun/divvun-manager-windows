using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Divvun.Installer.Util;
using OneOf;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Serilog;

namespace Divvun.Installer.Models
{
    public abstract class TransactionState : OneOfBase<
        TransactionState.NotStarted,
        TransactionState.InProgress,
        TransactionState.Error
    >, IEquatable<TransactionState>
    {
        public class NotStarted : TransactionState
        { }
        
        public class InProgress : TransactionState
        {

            public abstract class TransactionProcessState : OneOfBase<
                TransactionProcessState.DownloadState,
                TransactionProcessState.InstallState,
                TransactionProcessState.CompleteState
            >, IEquatable<TransactionProcessState> {
                public class DownloadState : TransactionProcessState
                {
                    public ConcurrentDictionary<PackageKey, (long, long)> Progress
                        = new ConcurrentDictionary<PackageKey, (long, long)>();
                }

                public class InstallState : TransactionProcessState
                {
                    public PackageKey CurrentItem;
                }

                public class CompleteState : TransactionProcessState
                { }

                public TransactionProcessState.DownloadState? AsDownloadState => IsT0 ? AsT0 : null;
                public TransactionProcessState.InstallState? AsInstallState => IsT1 ? AsT1 : null;
                
                public bool Equals(TransactionProcessState? other) {
                    return false;
                }

                public override bool Equals(object? obj) {
                    return false;
                }
            }
            
            public ResolvedAction[] Actions;
            public bool IsRebootRequired;
            
            public TransactionProcessState State;

            public bool IsDownloading => State.AsDownloadState != null;

            public InProgress IntoInstall(PackageKey packageKey) {
                this.State = new TransactionProcessState.InstallState() {
                    CurrentItem = packageKey
                };
                return this;
            }
            
            public InProgress IntoComplete() {
                this.State = new TransactionProcessState.CompleteState();
                return this;
            }
        }

        public class Error : TransactionState
        {
            public string Message;
        }

        public bool IsNotStarted => IsT0;
        public bool IsInProgress => IsT1;
        public bool IsInProgressDownloading => AsInProgress?.State.AsDownloadState != null;
        public bool IsInProgressInstalling => AsInProgress?.State.AsInstallState != null;
        public bool IsError => IsT2;

        public InProgress? AsInProgress => IsInProgress ? AsT1 : null;

        private bool EnsureInstallState(TransactionState state, out TransactionState newState) {
            newState = state;
            
            if (state.IsInProgress) {
                if (state.IsInProgressDownloading) {
                    // First install or uninstall event, time to set up shop
                    return true;
                }

                if (state.IsInProgressInstalling) {
                    // Not new here, just update the state
                    return true;
                }

                // We're getting a late event while in the completion view, just silently drop it.
                return false;
            }
            
            // We should never get here, silently return current state.
            return false;
        }

        private Dictionary<PackageKey, (long, long)> DefaultDownloadState(ResolvedAction[] actions) {
            var dict = new Dictionary<PackageKey, (long, long)>();

            foreach (var resolvedAction in actions) {
                if (resolvedAction.Action.Action == InstallAction.Install) {
                    dict.Add(resolvedAction.Action.PackageKey, (0, long.MaxValue));
                }
            }
            
            return dict;
        }
        
        public TransactionState Reduce(TransactionResponseValue value) {
            var state = this;
            
            return value.Match(
                downloadProgress => {
                    if (state.AsInProgress?.IsDownloading ?? false) {
                        var dl = state.AsInProgress!.State.AsDownloadState!;
                        dl.Progress[downloadProgress.PackageKey] = ((long) downloadProgress.Current, (long) downloadProgress.Total);
                    }

                    return state;
                },
                downloadComplete => {
                    if (state.AsInProgress?.IsDownloading ?? false) {
                        var dl = state.AsInProgress!.State.AsDownloadState!;
                        dl.Progress[downloadComplete.PackageKey] = (Int64.MaxValue, Int64.MaxValue);
                    }

                    return state;
                },
                installStarted => {
                    if (EnsureInstallState(this, out var newState)) {
                        return newState.AsInProgress!.IntoInstall(installStarted.PackageKey);
                    };

                    return state;
                },
                uninstallStarted => {
                    if (EnsureInstallState(this, out var newState)) {
                        return newState.AsInProgress!.IntoInstall(uninstallStarted.PackageKey);
                    }

                    return state;
                },
                transactionProgress => state,
                transactionError => new Error {
                    Message = transactionError.Error
                },
                transactionStarted => new InProgress {
                    Actions = transactionStarted.Actions,
                    IsRebootRequired = transactionStarted.IsRebootRequired,
                    State = new InProgress.TransactionProcessState.DownloadState {
                        Progress = new ConcurrentDictionary<PackageKey, (long, long)>()
                    }
                },
                transactionComplete => {
                    if (state.IsInProgress) {
                        var newState = state;
                        return newState.AsInProgress!.IntoComplete();
                    }

                    return state;
                },
                transactionQueued => {
                    Log.Information("Queued; waiting for transaction.");
                    return state;
                }
            );
        }

        public bool Equals(TransactionState? other) {
            return false;
        }

        public override bool Equals(object? obj) {
            return false;
        }
    }
}