using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows.Threading;
using Bahkat.Models;
using Bahkat.Util;

namespace Bahkat.Service
{
    public struct ProcessResult
    {
        public Package Package;
        public PackageAction Action;
        public int ExitCode;
        public string Output;
        public string Error;

        public bool IsSuccess => ExitCode == 0;
    }
    
    public struct OnStartPackageInfo
    {
        public Package Package;
        public PackageAction Action;
        public long Count;
        public long Remaining;
    }

    public class PackageProcessInfo
    {
        public PackageInstallInfo[] ToInstall;
        public PackageUninstallInfo[] ToUninstall;
    }

    public interface IInstallService
    {
        IObservable<ProcessResult> Process(PackageProcessInfo process, Subject<OnStartPackageInfo> onStartPackage, CancellationToken token);
    }
    
    public class InstallService : IInstallService
    {
        protected virtual IReactiveProcess CreateProcess(string path, string args)
        {
            return new ReactiveProcess(path, args);
        }

        private IObservable<ProcessResult> ProcessPackage(Package package, PackageAction action, IReactiveProcess process)
        {
            return Observable.Zip(
                    process.Output.ToList().Select(x => string.Join("\n", x)),
                    process.Error.ToList().Select(x => string.Join("\n", x)),
                    process.Start(),
                    (output, error, exit) => new ProcessResult
                    {
                        Package = package,
                        Action = action,
                        ExitCode = exit,
                        Output = output,
                        Error = error
                    })
                .Take(1);
        }
        
        private string ArgsForUninstallerType(string type, PackageUninstallInfo info)
        {
            switch (type)
            {
                case "inno":
                    return "/VERYSILENT /SP- /SUPPRESSMSGBOXES /NORESTART";
                case "msi":
                    return $"/x \"{info.Package.Installer.ProductCode}\" /qn /norestart";
                default:
                    return info.Package.Installer.UninstallArgs;
            }
        }

        private IObservable<ProcessResult> UninstallPackage(PackageUninstallInfo info, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return null;
            }
            
            var package = info.Package;
            var installer = package.Installer;
            var process = info.Path.StartsWith("msiexec", StringComparison.OrdinalIgnoreCase)
                ? CreateProcess("msiexec", ArgsForUninstallerType("msi", info))
                : CreateProcess(info.Path, ArgsForUninstallerType(installer.Type, info));
            
            return ProcessPackage(package, PackageAction.Uninstall, process);
        }

        private string ArgsForInstallerType(string type, PackageInstallInfo info)
        {
            switch (type)
            {
                case "inno":
                    return "/VERYSILENT /SP- /SUPPRESSMSGBOXES /NORESTART";
                case "msi":
                    return $"/i \"{info.Path}\" /qn /norestart";
                case "nsis":
                    return "/SD";
                default:
                    return info.Package.Installer.Args;
            }
        }

        private IObservable<ProcessResult> InstallPackage(PackageInstallInfo info, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return null;
            }
            
            var package = info.Package;
            var installer = package.Installer;
            var process = info.Path.EndsWith(".msi", true, CultureInfo.InvariantCulture)
                ? CreateProcess("msiexec", ArgsForInstallerType("msi", info))
                : CreateProcess(info.Path, ArgsForInstallerType(installer.Type, info));

            return ProcessPackage(package, PackageAction.Install, process);
        }

        private IObservable<ProcessResult> Install(PackageInstallInfo[] packages,
            Subject<OnStartPackageInfo> onStartPackage, CancellationToken token)
        {
            return packages
                .Select(t =>
                {
                    onStartPackage.OnNext(new OnStartPackageInfo {
                        Package = t.Package,
                        Action = PackageAction.Install
                    });
                    return InstallPackage(t, token);
                }).Concat();
        }

        private IObservable<ProcessResult> Uninstall(PackageUninstallInfo[] packages,
            Subject<OnStartPackageInfo> onStartPackage, CancellationToken token)
        {
            return packages
                .Select(t =>
                {
                    onStartPackage.OnNext(new OnStartPackageInfo {
                        Package = t.Package,
                        Action = PackageAction.Uninstall
                    });
                    return UninstallPackage(t, token);
                }).Concat();
        }

        public IObservable<ProcessResult> Process(PackageProcessInfo process, Subject<OnStartPackageInfo> onStartPackage, CancellationToken token)
        {
            var total = process.ToInstall.LongLength + process.ToUninstall.LongLength;
            var remaining = process.ToInstall.LongLength + process.ToUninstall.LongLength;
            var counterSubject = new Subject<OnStartPackageInfo>();

            // This code makes me feel bad.
            counterSubject.Subscribe(info =>
            {
                // Inject the current status into the info before hitting the UI.
                info.Count = total - remaining;
                info.Remaining = remaining;
                remaining--;
                onStartPackage.OnNext(info);
            });

            return Observable.Concat(
                Uninstall(process.ToUninstall, counterSubject, token),
                Install(process.ToInstall, counterSubject, token)
            );
        }
    }
}