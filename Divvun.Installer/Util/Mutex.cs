using System;
using System.Threading;

namespace Divvun.Installer.Util
{
    public struct Mutex<T>
    {
        public struct Guard: IDisposable
        {
            private Mutex<T> _mutex;
            
            internal Guard(Mutex<T> mutex) {
                _mutex = mutex;
            }
            
            public T Value {
                get => _mutex._value;
                set => _mutex._value = value;
            }
            
            public void Dispose() {
                // Console.WriteLine("[Mutex] Dropping");
                _mutex._mutex.ReleaseMutex();
            }
        }
        
        private T _value;
        private Mutex _mutex;

        public Guard Lock() {
            // Console.WriteLine("[Mutex] Locking");
            _mutex.WaitOne();
            
            // Console.WriteLine("[Mutex] Got lock");
            return new Guard(this);
        }

        public Mutex(T value) {
            _value = value;
            _mutex = new Mutex();
        }
    }
}