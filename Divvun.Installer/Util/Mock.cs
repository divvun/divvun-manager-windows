// namespace Divvun.Installer.Util
// {
//     class MockPahkatClient : IPahkatClient
//     {
//         public CancellationTokenSource ProcessTransaction(PackageAction[] actions, Action<TransactionResponseValue> callback) {
//             callback(new TransactionResponseValue.TransactionStarted() {
//                 Actions = new ResolvedAction[] {}
//             });
//             callback(new TransactionResponseValue.TransactionComplete());
//             return new CancellationTokenSource();
//         }
//
//         public PackageStatus Status(PackageKey packageKey) {
//             return PackageStatus.NotInstalled;
//         }
//
//         public Dictionary<Uri, LoadedRepository> RepoIndexes() {
//             return new Dictionary<Uri, LoadedRepository>();
//         }
//     }
//
//     public static class Mock
//     {
//         public static (TransactionResponseValue, ResolvedAction[]) MakeStart() {
//             var names = new Dictionary<string, string>();
//             names["en"] = "Test Package";
//
//
//             var actions = new[] {
//                 new ResolvedAction() {
//                     Action = new PackageAction(
//                         PackageKey.From("https://x.brendan.so/divvun-pahkat-repo/packages/speller-sme"),
//                         InstallAction.Install),
//                     Name = names,
//                     Version = "420.0"
//
//                 },
//                 new ResolvedAction() {
//                     Action = new PackageAction(
//                         PackageKey.From("https://x.brendan.so/divvun-pahkat-repo/packages/speller-sma"),
//                         InstallAction.Install),
//                     Name = names,
//                     Version = "1.0.0"
//
//                 }
//             };
//
//             var value = new TransactionResponseValue.TransactionStarted {
//                 Actions = actions
//             };
//
//             return (value, new ResolvedAction[] { });
//         }
//
//         public static (TransactionResponseValue, ResolvedAction[]) MakeDownloadProgress() {
//             var names = new Dictionary<string, string>();
//             names["en"] = "Test Package";
//
//             var key = PackageKey.From("https://x.brendan.so/divvun-pahkat-repo/packages/speller-sme");
//             var actions = new[] {
//                 new ResolvedAction() {
//                     Action = new PackageAction(key, InstallAction.Install),
//                     Name = names,
//                     Version = "420.0"
//
//                 },
//                 new ResolvedAction() {
//                     Action = new PackageAction(
//                         PackageKey.From("https://x.brendan.so/divvun-pahkat-repo/packages/speller-sma"),
//                         InstallAction.Install),
//                     Name = names,
//                     Version = "1.0.0"
//
//                 }
//             };
//
//             var value = new TransactionResponseValue.DownloadProgress() {
//                 PackageKey = key,
//                 Current = 10000,
//                 Total = 100000
//             };
//
//             return (value, actions);
//         }
//
//         public static (TransactionResponseValue, ResolvedAction[]) MakeInstall() {
//             var names = new Dictionary<string, string>();
//             names["en"] = "Test Package";
//
//             var key = PackageKey.From("https://x.brendan.so/divvun-pahkat-repo/packages/speller-sme");
//             var actions = new[] {
//                 new ResolvedAction() {
//                     Action = new PackageAction(key, InstallAction.Install),
//                     Name = names,
//                     Version = "420.0"
//
//                 },
//                 new ResolvedAction() {
//                     Action = new PackageAction(
//                         PackageKey.From("https://x.brendan.so/divvun-pahkat-repo/packages/speller-sma"),
//                         InstallAction.Install),
//                     Name = names,
//                     Version = "1.0.0"
//
//                 }
//             };
//
//
//             var value = new TransactionResponseValue.InstallStarted() {
//                 PackageKey = PackageKey.From("https://x.brendan.so/divvun-pahkat-repo/packages/speller-sme")
//             };
//
//             return (value, actions);
//         }
//
//         public static (TransactionResponseValue, ResolvedAction[]) MakeDone() {
//
//             var actions = new[] {
//                 new ResolvedAction() {
//                     Action = new PackageAction(
//                         PackageKey.From("https://x.brendan.so/divvun-pahkat-repo/packages/speller-sme"),
//                         InstallAction.Install),
//                     Name = new Dictionary<string, string>(),
//                     Version = "420.0"
//
//                 }
//             };
//
//             var value = new TransactionResponseValue.TransactionComplete();
//
//             return (value, actions);
//         }
//     }
//
//
//
// }