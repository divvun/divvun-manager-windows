using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Bahkat.Models;

namespace Bahkat.Util
{
    public interface IStore<TState, TEvent>
    {
        IObservable<TState> State { get; }
        void Dispatch(TEvent e);
    }
    
    public class RxStore<TState, TEvent> : IStore<TState, TEvent>
    {
        public IObservable<TState> State { get; }
        private readonly Subject<TEvent> _dispatcher = new Subject<TEvent>();

        public void Dispatch(TEvent e)
        {
            _dispatcher.OnNext(e);
        }

        public RxStore(TState initialState, params Func<TState, TEvent, TState>[] reducers)
        {
            State = Feedback.System(
                    initialState,
                    (i, e) => reducers.Aggregate(i, (state, next) => next(state, e)),
                    _ => _dispatcher.AsObservable())
                .Replay(1)
                .RefCount();
        }
    }
}