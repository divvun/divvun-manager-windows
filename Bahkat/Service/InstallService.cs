using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Threading;
using Bahkat.Models;
using Bahkat.Util;

namespace Bahkat.Service
{
    public struct ProcessResult
    {
        public Package Package;
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
        IObservable<ProcessResult> Process(PackageProcessInfo process, Subject<OnStartPackageInfo> onStartPackage);
    }
    
    public class InstallService : IInstallService
    {
        protected virtual IReactiveProcess CreateProcess(string path, string args)
        {
            return new ReactiveProcess(path, args);
        }

        private IObservable<ProcessResult> ProcessPackage(Package package, IReactiveProcess process)
        {
            return Observable.Zip(
                    process.Output.ToList().Select(x => string.Join("\n", x)),
                    process.Error.ToList().Select(x => string.Join("\n", x)),
                    process.Start(),
                    (output, error, exit) => new ProcessResult
                    {
                        Package = package,
                        ExitCode = exit,
                        Output = output,
                        Error = error
                    })
                .Take(1);
        }

        private IObservable<ProcessResult> UninstallPackage(Package package, string path, string args)
        {
            var process = path.StartsWith("msiexec", StringComparison.OrdinalIgnoreCase)
                ? CreateProcess("msiexec", $"/x {package.Installer.ProductCode} /qn /norestart")
                : CreateProcess(path, args);
            return ProcessPackage(package, CreateProcess(path, args));
        }

        private IObservable<ProcessResult> InstallPackage(Package package, string path, string args)
        {
            var process = path.EndsWith(".msi") 
                ? CreateProcess("msiexec", args + " " + path)
                : CreateProcess(path, args);

            return ProcessPackage(package, process);
        }

        private IObservable<ProcessResult> Install(PackageInstallInfo[] packages, Subject<OnStartPackageInfo> onStartPackage)
        {
            return packages
                .Select(t =>
                {
                    onStartPackage.OnNext(new OnStartPackageInfo {
                        Package = t.Package,
                        Action = PackageAction.Install
                    });
                    return InstallPackage(t.Package, t.Path, t.Package.Installer.Args);
                }).Concat();
        }

        private IObservable<ProcessResult> Uninstall(PackageUninstallInfo[] packages,
            Subject<OnStartPackageInfo> onStartPackage)
        {
            return packages
                .Select(t =>
                {
                    onStartPackage.OnNext(new OnStartPackageInfo {
                        Package = t.Package,
                        Action = PackageAction.Uninstall
                    });
                    return UninstallPackage(t.Package, t.Path, t.Args);
                }).Concat();
        }

        public IObservable<ProcessResult> Process(PackageProcessInfo process, Subject<OnStartPackageInfo> onStartPackage)
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
                Uninstall(process.ToUninstall, counterSubject),
                Install(process.ToInstall, counterSubject)
            );
        }
    }
}