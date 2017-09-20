using System;
using System.Threading;
using System.Collections.Generic;

namespace Lercher
{
    public interface IBlock<T> : IDisposable
    {

    }

    public class MonitorDictionary<T>
    {
        private Dictionary<T, Resource> dictionary = new Dictionary<T, Resource>();


        public class Resource : IBlock<T>
        {
            private CountdownEvent countDown = new CountdownEvent(1);
            private MonitorDictionary<T> container;
            public readonly T Key;

            internal Resource(MonitorDictionary<T> container, T key)
            {
                this.container = container;
                this.Key = key;
            }

            internal Resource Use()
            {
                countDown.AddCount();
                return this;
            }

            public void Dispose()
            {
                try
                {
                    lock(container) 
                    {
                        countDown.Signal();
                        if (countDown.IsSet)
                        {
                            container.Remove(this);
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(this);
                }
            }
        }

        private void Remove(Resource resource)
        {
            dictionary.Remove(resource.Key);
        }

        public IBlock<T> Guard(T key)
        {
            Resource res;
            lock(this)
            {
                if (!dictionary.TryGetValue(key, out res))
                {
                    res = new Resource(this, key);
                    dictionary.Add(key, res);
                    // don't add to the countdown, because we already started with 1                     
                    Monitor.Enter(res); // The Monitor can't lock here, because we created res newly
                    return res;
                }
                // existing resource: we need to add 1 to the use count while holding the dictionary lock
                // because otherwise a res.Dispose() call on a different thread would try to remove res from the dictionary
                res.Use(); 
            }
            // existing resource, lock outside the dictionary lock to avoid a deadlock:
            Monitor.Enter(res);
            return res;
        }
    }
}