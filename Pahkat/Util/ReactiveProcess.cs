using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Pahkat.Util
{
    public interface IReactiveProcess
    {
        IObservable<string> Output { get; }
        IObservable<string> Error { get; }
        IObservable<int> Start();
    }
    
    public class ReactiveProcess : IReactiveProcess
    {
        public IObservable<string> Output { get; private set; }
        public IObservable<string> Error { get; private set; }
        
        internal readonly Process _process;
        
        public ReactiveProcess(Action<ProcessStartInfo> configure)
        {
            _process = new Process();
            configure(_process.StartInfo);
            ConfigureProcess();
        }

        public ReactiveProcess(string path, string args = "")
        {
            _process = new Process
            {
                StartInfo =
                {
                    FileName = path,
                    Arguments = args
                }
            };
            ConfigureProcess();
        }
        
        private void ConfigureProcess()
        {
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.UseShellExecute = false;

            Output = Observable.Create<string>(observer =>
            {
                while (!_process.HasExited)
                {
                    observer.OnNext(_process.StandardOutput.ReadLine());
                }
            
                observer.OnCompleted();

                return (IDisposable) observer;
            }).SubscribeOn(NewThreadScheduler.Default);
            
            Error = Observable.Create<string>(observer =>
            {
                while (!_process.HasExited)
                {
                    observer.OnNext(_process.StandardError.ReadLine());
                }
            
                observer.OnCompleted();
                
                return (IDisposable) observer;
            }).SubscribeOn(NewThreadScheduler.Default);
        }

        public IObservable<int> Start()
        {
            var exit = Observable.Create<int>(observer =>
            {   
                _process.WaitForExit();
                observer.OnNext(_process.ExitCode);
                observer.OnCompleted();
                
                return (IDisposable) observer;
            }).SubscribeOn(NewThreadScheduler.Default);
            
            _process.Start();

            return exit;
        }
        
        public void ShamefullyKill()
        {
            if (!_process.HasExited)
            {
                _process.Kill();
            }
        }

        internal void ShamefullyWaitForExit()
        {
            _process.WaitForExit();
        }
    }
}