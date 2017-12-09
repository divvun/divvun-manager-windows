namespace System.Reactive.Feedback
{
    public abstract class RxSystem<TState, TEvent> : IObservable<TState>
    {
        protected abstract TState Reduce(TState state, TEvent e);
        protected abstract IDisposable[] Subscriptions(ObservableSchedulerContext<TState> state);
        protected abstract IObservable<TEvent>[] Events(ObservableSchedulerContext<TState> state);

        private readonly IObservable<TState> _system;
        
        public RxSystem(TState initialState)
        {
            _system = Feedback.System(
                initialState,
                Reduce,
                Feedback.Bind<TState, TEvent>(state => 
                    UIBindings<TEvent>.Create(Subscriptions(state), Events(state))));
        }

        public IDisposable Subscribe(IObserver<TState> observer)
        {
            return _system.Subscribe(observer);
        }
    }
}