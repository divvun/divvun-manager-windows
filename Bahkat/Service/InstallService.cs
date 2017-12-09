using System;
using System.Linq;
using System.Reactive.Linq;
using Bahkat.Models.PackageManager;
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

    public interface IInstallWorker
    {
        IObservable<ProcessResult> Process();
    }
    
    public class InstallWorker : IInstallWorker
    {
        private readonly PackagePath[] _packages;

        protected virtual IReactiveProcess CreateProcess(string path, string args)
        {
            return new ReactiveProcess(path, args);
        }

        private IObservable<ProcessResult> Install(Package package, string path, string args)
        {
            var process = path.EndsWith(".msi") 
                ? CreateProcess("msiexec", args + " " + path)
                : CreateProcess(path, args);

            return Observable.CombineLatest(
                process.Output.ToList().Select(x => string.Join("\n", x)),
                process.Error.ToList().Select(x => string.Join("\n", x)),
                process.Start(),
                (output, error, exit) => new ProcessResult
                {
                    Package = package,
                    ExitCode = exit,
                    Output = output,
                    Error = error
                });
        }
        
        public InstallWorker(PackagePath[] packages)
        {
            _packages = packages;
        }

        public IObservable<ProcessResult> Process()
        {
            return _packages.Where(t => t.Package.Installer.HasValue)
                .Select(t =>
                {
                    var args = t.Package.Installer.Value.SilentArgs;
                    return Install(t.Package, t.Path, args);
                }).Concat();
        }

        public static IObservable<ProcessResult> Process(PackagePath[] packages)
        {
            return new InstallWorker(packages).Process();
        }
    }
}