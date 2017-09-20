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
                this.container = container; this.Key = key;
            }

            internal Resource Use(int count)
            {
                Monitor.Enter(this);
                countDown.AddCount(count);
                return this;
            }

            public void Dispose()
            {
                try
                {
                    countDown.Signal();
                    if (countDown.IsSet)
                    {
                        container.Remove(this);
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
            lock (dictionary)
                dictionary.Remove(resource.Key);
        }

        public IBlock<T> Use(T key)
        {
            Resource res;
            var count01 = 1;
            lock (dictionary)
            {
                if (!dictionary.TryGetValue(key, out res))
                {
                    res = new Resource(this, key);
                    dictionary.Add(key, res);
                    count01 = 0; // don't add to the countdown, because we already started with 1
                }
            }
            // outside the dictionary lock:
            return res.Use(count01);
        }
    }
}