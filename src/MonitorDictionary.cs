using System;
using System.Threading;
using System.Collections.Generic;

namespace Lercher
{
    public class MonitorDictionary<T>
    {
        private Dictionary<T, Resource> dictionary = new Dictionary<T, Resource>();
        private int maxEntries = 0;
        private int maxUseCount = 0;

        public void AssertIsClearAfterUse()
        {
            System.Console.WriteLine("{0} current keys, {1} max keys, {2} max concurrent use count", dictionary.Count, maxEntries, maxUseCount);
            if (dictionary.Count > 0)
                throw new Exception("The dictionary should be clear now");
            if (maxEntries == 0)
                throw new Exception("This MonitorDictionary was not used at all");
            if (maxUseCount <= 1)
                throw new Exception("This MonitorDictionary was not used concurrently");
        }

        public class Resource : IDisposable
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
                container.maxUseCount = Math.Max(container.maxUseCount, countDown.CurrentCount);
                return this;
            }

            public void Dispose()
            {
                try
                {
                    lock (container)
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

        public IDisposable Guard(T key)
        {
            Resource res;
            lock (this)
            {
                if (!dictionary.TryGetValue(key, out res))
                {
                    res = new Resource(this, key);
                    dictionary.Add(key, res);
                    maxEntries = Math.Max(maxEntries, dictionary.Count);
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