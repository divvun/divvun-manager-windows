using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace Pahkat.Util
{
    public interface IReactiveProcess
    {
        IObserver<string> Input { get; }
        IObservable<string> Output { get; }
        IObservable<string> Error { get; }
        IObservable<int> Start();
    }
    
    public class ReactiveProcess : IReactiveProcess
    {
        public IObserver<string> Input { get; private set; }
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
            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.UseShellExecute = false;

            Input = Observer.Create<string>(input =>
            {
                Console.WriteLine($"[Process {_process.Id} I] '{input}'");
                _process.StandardInput.WriteLine(input);
            });

            Output = Observable.Create<string>(observer =>
            {
                Console.WriteLine($"[Process {_process.Id}] OBSERVING STDOUT");

                while (!_process.HasExited)
                {
                    var str = _process.StandardOutput.ReadLine();
                    Console.WriteLine($"[Process {_process.Id} O] '{str}'");

                    if (str == null)
                    {
                        continue;
                    }

                    if (_process.StandardOutput.CurrentEncoding != Encoding.UTF8)
                    {
                        var raw = _process.StandardOutput.CurrentEncoding.GetBytes(str);
                        observer.OnNext(Encoding.UTF8.GetString(raw));
                    }
                    else
                    {
                        observer.OnNext(str);
                    }
                }

                Console.WriteLine($"Process {_process.Id} done");
                observer.OnCompleted();

                return (IDisposable)observer;
            }).SubscribeOn(NewThreadScheduler.Default).Publish();

            Error = Observable.Create<string>(observer =>
            {
                Console.WriteLine($"[Process {_process.Id}] OBSERVING STDERR");

                while (!_process.HasExited)
                {
                    var str = _process.StandardError.ReadLine();
                    Console.WriteLine($"[Process {_process.Id} E] '{str}'");
                    observer.OnNext(str);
                }

                observer.OnCompleted();

                return (IDisposable)observer;
            }).SubscribeOn(NewThreadScheduler.Default).Publish();
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

            (Output as IConnectableObservable<string>).Connect();
            (Error as IConnectableObservable<string>).Connect();

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