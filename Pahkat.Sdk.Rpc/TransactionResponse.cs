using System;
using JsonSubTypes;
using Newtonsoft.Json;
using OneOf;

namespace Pahkat.Sdk.Rpc {

public class TransactionResponseValue : OneOfBase<
    TransactionResponseValue.DownloadProgress,
    TransactionResponseValue.DownloadComplete,
    TransactionResponseValue.InstallStarted,
    TransactionResponseValue.UninstallStarted,
    TransactionResponseValue.TransactionProgress,
    TransactionResponseValue.TransactionError,
    TransactionResponseValue.TransactionStarted,
    TransactionResponseValue.TransactionComplete,
    TransactionResponseValue.TransactionQueued,
    TransactionResponseValue.VerificationFailed
>, IEquatable<TransactionResponseValue> {
    public enum SubstateType {
        Download,
        Install,
        Error,
        Starting,
        Completion,
    }

    protected TransactionResponseValue(
        OneOf<DownloadProgress, DownloadComplete, InstallStarted, UninstallStarted, TransactionProgress,
            TransactionError, TransactionStarted, TransactionComplete, TransactionQueued, VerificationFailed> input) :
        base(input) {
    }

    public bool IsDownloadState => IsT0 || IsT1;
    public bool IsInstallState => IsT2 || IsT3 || IsT4;
    public bool IsErrorState => IsT5 || IsT9;
    public bool IsStartingState => IsT6 || IsT8;
    public bool IsCompletionState => IsT7;

    public SubstateType Substate {
        get {
            if (IsStartingState) {
                return SubstateType.Starting;
            }

            if (IsDownloadState) {
                return SubstateType.Download;
            }

            if (IsInstallState) {
                return SubstateType.Install;
            }

            if (IsCompletionState) {
                return SubstateType.Completion;
            }

            return SubstateType.Error;
        }
    }

    public DownloadProgress? AsDownloadProgress => IsT0 ? AsT0 : null;
    public DownloadComplete? AsDownloadComplete => IsT1 ? AsT1 : null;
    public InstallStarted? AsInstallStarted => IsT2 ? AsT2 : null;
    public UninstallStarted? AsUninstallStarted => IsT3 ? AsT3 : null;
    public TransactionProgress? AsTransactionProgress => IsT4 ? AsT4 : null;
    public TransactionError? AsTransactionError => IsT5 ? AsT5 : null;
    public TransactionStarted? AsTransactionStarted => IsT6 ? AsT6 : null;
    public TransactionComplete? AsTransactionComplete => IsT7 ? AsT7 : null;
    public TransactionQueued? AsTransactionQueued => IsT8 ? AsT8 : null;
    public VerificationFailed? AsVerificationFailed => IsT9 ? AsT9 : null;

    public bool Equals(TransactionResponseValue? other) {
        return false;
    }

    public static JsonConverter JsonConvertor() {
        return JsonSubtypesConverterBuilder.Of(typeof(TransactionResponseValue), "type")
            .RegisterSubtype(typeof(DownloadProgress), nameof(DownloadProgress))
            .RegisterSubtype(typeof(DownloadComplete), nameof(DownloadComplete))
            .RegisterSubtype(typeof(InstallStarted), nameof(InstallStarted))
            .RegisterSubtype(typeof(UninstallStarted), nameof(UninstallStarted))
            .RegisterSubtype(typeof(TransactionProgress), nameof(TransactionProgress))
            .RegisterSubtype(typeof(TransactionError), nameof(TransactionError))
            .RegisterSubtype(typeof(TransactionStarted), nameof(TransactionStarted))
            .RegisterSubtype(typeof(TransactionComplete), nameof(TransactionComplete))
            .RegisterSubtype(typeof(TransactionQueued), nameof(TransactionQueued))
            .RegisterSubtype(typeof(VerificationFailed), nameof(VerificationFailed))
            .Build();
    }

    public static implicit operator TransactionResponseValue(DownloadProgress _) {
        return new TransactionResponseValue(_);
    }

    public static implicit operator TransactionResponseValue(DownloadComplete _) {
        return new TransactionResponseValue(_);
    }

    public static implicit operator TransactionResponseValue(InstallStarted _) {
        return new TransactionResponseValue(_);
    }

    public static implicit operator TransactionResponseValue(UninstallStarted _) {
        return new TransactionResponseValue(_);
    }

    public static implicit operator TransactionResponseValue(TransactionProgress _) {
        return new TransactionResponseValue(_);
    }

    public static implicit operator TransactionResponseValue(TransactionError _) {
        return new TransactionResponseValue(_);
    }

    public static implicit operator TransactionResponseValue(TransactionStarted _) {
        return new TransactionResponseValue(_);
    }

    public static implicit operator TransactionResponseValue(TransactionComplete _) {
        return new TransactionResponseValue(_);
    }

    public static implicit operator TransactionResponseValue(TransactionQueued _) {
        return new TransactionResponseValue(_);
    }

    public static implicit operator TransactionResponseValue(VerificationFailed _) {
        return new TransactionResponseValue(_);
    }

    public override bool Equals(object? obj) {
        return false;
    }

    public class DownloadProgress {
        public ulong Current;

        [JsonProperty(PropertyName = "package_id")]
        public PackageKey PackageKey;

        public ulong Total;
    }

    public class DownloadComplete {
        [JsonProperty(PropertyName = "package_id")]
        public PackageKey PackageKey;
    }

    public class InstallStarted {
        [JsonProperty(PropertyName = "package_id")]
        public PackageKey PackageKey;
    }

    public class UninstallStarted {
        [JsonProperty(PropertyName = "package_id")]
        public PackageKey PackageKey;
    }

    public class TransactionProgress {
        public ulong Current;
        public string Message;

        [JsonProperty(PropertyName = "package_id")]
        public PackageKey PackageKey;

        public ulong Total;
    }

    public class TransactionError {
        public string? Error;

        [JsonProperty(PropertyName = "package_id")]
        public string? PackageKey;
    }

    public class TransactionStarted {
        public ResolvedAction[] Actions;
        public bool IsRebootRequired;
    }

    public class TransactionComplete {
    }

    public class TransactionQueued {
    }

    public class VerificationFailed {
    }
}

public class TransactionResponse {
    public TransactionResponseValue Value;
}

}