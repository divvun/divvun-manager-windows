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
        public long Count;
        public long Remaining;
    }

    public interface IInstallService
    {
        IObservable<ProcessResult> Process(PackagePath[] packages, Subject<OnStartPackageInfo> onStartPackage);
    }
    
    public class InstallService : IInstallService
    {
        protected virtual IReactiveProcess CreateProcess(string path, string args)
        {
            return new ReactiveProcess(path, args);
        }

        private IObservable<ProcessResult> Install(Package package, string path, string args)
        {
            var process = path.EndsWith(".msi") 
                ? CreateProcess("msiexec", args + " " + path)
                : CreateProcess(path, args);

            return Observable.Zip(
                process.Output.ToList().Select(x => string.Join("\n", x)),
                process.Error.ToList().Select(x => string.Join("\n", x)),
                process.Start(),
                (output, error, exit) =>
                {
                    return new ProcessResult
                        {
                            Package = package,
                            ExitCode = exit,
                            Output = output,
                            Error = error
                        };
                })
                .Take(1);
        }

        public IObservable<ProcessResult> Process(PackagePath[] packages, Subject<OnStartPackageInfo> onStartPackage)
        {
            var total = packages.Length;
            var i = 0;
            
            return packages.Where(t => t.Package.Installer != null)
                .Select(t =>
                {
                    var remaining = total - i;
                    onStartPackage.OnNext(new OnStartPackageInfo {
                        Package = t.Package,
                        Remaining = remaining,
                        Count = i
                    });
                    i++;
                    var args = t.Package.Installer.SilentArgs;
                    return Install(t.Package, t.Path, args);
                }).Concat();
        }
    }
}