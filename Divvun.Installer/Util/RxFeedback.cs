using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Divvun.Installer.Util
{
    public struct ObservableSchedulerContext<T> : IObservable<T>
    {
        internal IObservable<T> Source;
        internal IScheduler Scheduler;

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return Source.Subscribe(observer);
        }
    }

    internal static class ObservableExtensions
    {
        internal static IObservable<T> DoOnSubscribe<T>(this IObservable<T> source, Action action)
        {
            return Observable.Defer(() =>
            {
                action();
                return source;
            });
        }

        internal static void DisposedBy(this IDisposable disposable, CompositeDisposable bag)
        {
            bag.Add(disposable);
        }
    }

    public static class Feedback
    {
        public static IObservable<TState> System<TState, TEvent>(TState initialState,
            Func<TState, TEvent, TState> reduce,
            IScheduler scheduler,
            params Func<ObservableSchedulerContext<TState>, IObservable<TEvent>>[] scheduledFeedback
        )
        {
            return Observable.Defer(() =>
            {
                var replaySubject = new ReplaySubject<TState>(1);

                IObservable<TEvent> events = Observable.Merge(scheduledFeedback.Select(feedback =>
                {
                    var state = new ObservableSchedulerContext<TState>()
                    {
                        Source = replaySubject.AsObservable(),
                        Scheduler = scheduler
                    };
                    var result = feedback(state);
                    return result.ObserveOn(DispatcherScheduler.Current);
                }));

                return events.Scan(initialState, reduce)
                    .DoOnSubscribe(() => replaySubject.OnNext(initialState))
                    .Do(output => replaySubject.OnNext(output))
                    .SubscribeOn(scheduler)
                    .StartWith(initialState)
                    .ObserveOn(scheduler);
            });
        }

        public static IObservable<TState> System<TState, TEvent>(TState initialState,
            Func<TState, TEvent, TState> reduce,
            params Func<ObservableSchedulerContext<TState>, IObservable<TEvent>>[] scheduledFeedback
        )
        {
            return System(initialState, reduce, Scheduler.CurrentThread, scheduledFeedback);
        }
        
        public static Func<ObservableSchedulerContext<TState>, IObservable<TEvent>>
            Bind<TState, TEvent>(Func<ObservableSchedulerContext<TState>, UIBindings<TEvent>> bindingsFn)
        {
            return state =>
            {
                return Observable.Using(() => bindingsFn(state), bindings =>
                {
                    return Observable.Merge(bindings.Events)
                        .ObserveOn(state.Scheduler)
                        .SubscribeOn(state.Scheduler);
                });
            };
        }
    }

    public class UIBindings<TEvent> : IDisposable
    {
        private IDisposable[] subscriptions;
        internal IObservable<TEvent>[] Events;

        public static UIBindings<TEvent> Create(IDisposable subscription, params IObservable<TEvent>[] events)
        {
            return new UIBindings<TEvent>(new IDisposable[] { subscription }, events);
        }

        public static UIBindings<TEvent> Create(IDisposable[] subscriptions, params IObservable<TEvent>[] events)
        {
            return new UIBindings<TEvent>(subscriptions, events);
        }

        internal UIBindings(IDisposable[] subscriptions, IObservable<TEvent>[] events)
        {
            this.subscriptions = subscriptions;
            Events = events;
        }

        public void Dispose()
        {
            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }
        }
    }
}
