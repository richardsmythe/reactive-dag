using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReactiveDAG.Core.Models
{
    public abstract class BaseCell : IObservable<object>
    {
        private readonly List<IObserver<object>> _observers = new();
        public int Index { get; set; }
        public CellType CellType { get; set; }

        public abstract IDisposable Subscribe(Func<object, Task> onChanged);

        public IDisposable Subscribe(IObserver<object> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
            return new Unsubscriber(_observers, observer);
        }

        protected void NotifyObservers(object value)
        {
            foreach (var observer in _observers)
            {
                observer.OnNext(value);
            }
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<object>> _observers;
            private IObserver<object> _observer;

            public Unsubscriber(List<IObserver<object>> observers, IObserver<object> observer)
            {
                _observers = observers;
                _observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                    _observers.Remove(_observer);
            }
        }
    }
}
