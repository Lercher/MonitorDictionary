using System;
using System.Diagnostics;
using System.Threading;

namespace Lercher
{
    public class SampleClient2
    {
        private int[] interlocked;
        private int[] monitored;

        private MonitorDictionary<int> transactions = new MonitorDictionary<int>();


        public void Run(int count, int spincount)
        {
            var cd = new CountdownEvent(1);
            var threads = new CountdownEvent(1);
            interlocked = new int[count];
            monitored = new int[count];

            for (var i = 0; i < count; i++)
            {
                var index = i;
                
                var tr = new Thread((o) => { 
                    // System.Console.WriteLine("reader {0}", index);
                    var rnd = new Random(0);
                    var sleep = 0;
                    while(!cd.IsSet) {
                        sleep = rollDice0(rnd, 3, 33);
                        reader(index, sleep);
                        sleep = rollDice0(rnd, 3, 33);
                        Thread.Sleep(sleep);
                    }
                    // System.Console.WriteLine("reader {0} stopped", index);
                    threads.Signal();
                });
                tr.IsBackground = true;
                tr.Start();
                threads.AddCount();

                var tw1 = new Thread((o) => { 
                    // System.Console.WriteLine("writer1 {0}", index);
                    var rnd = new Random(0);
                    var sleep = 0;
                    while(!cd.IsSet) {
                        writer(index, spincount);
                        sleep = rollDice0(rnd, index, 10);
                        Thread.Sleep(sleep);
                    }
                    // System.Console.WriteLine("writer1 {0} stopped", index);
                    threads.Signal();
                });
                tw1.IsBackground = true;
                tw1.Start();
                threads.AddCount();
            }
            System.Console.WriteLine("Stopping in 30s ....");
            Thread.Sleep(TimeSpan.FromSeconds(30));
            cd.Signal();
            threads.Signal();
            threads.Wait();
        }

        private void writer(int index, int spincount)
        {
            using (transactions.Guard(index))
            {
                Interlocked.Increment(ref interlocked[index]);
                operate(monitored, index, spincount);
            }
        }
        private void reader(int index, int sleep)
        {
            using (transactions.Guard(index))
            {
                var realcount = Interlocked.Add(ref interlocked[index], 0);
                Thread.Sleep(sleep);
                var monitoredCount = monitored[index];
                if (realcount != monitoredCount)
                    System.Console.WriteLine("Count error on index {0}: interlocked={1}, monitored={2}", index, realcount, monitoredCount);
            }

        }
        private void operate(int[] arr, int index, int spincount)
        {
            var count = arr[index];
            for(var i = 0; i < spincount; i++)
            {
                // spin
            }
            arr[index] = count + 1;
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            DescribeTo(sb, interlocked, nameof(interlocked));
            DescribeTo(sb, monitored, nameof(monitored));
            return sb.ToString();
        }

        private void DescribeTo(System.Text.StringBuilder sb, int[] arr, string title)
        {
            sb.AppendFormat("{0} {1} {0}\n", "-----------", title);
            var sum = 0;
            for (var i = 0; i < arr.Length; i++)
            {
                sb.AppendFormat("{0,3:n0} -> {1,5:n0}{2}\n", i, arr[i], arr[i] == interlocked[i] ? "" : " !");
                sum += arr[i];
            }
            sb.AppendFormat("Sum: {0,7:n0}\n\n", sum);
        }

        private int rollDice0(Random rnd, int count, int sides)
        {
            var sum = 0;
            for (var i = 0; i < count; i++)
                sum += rnd.Next(sides);
            return sum;
        }

    }

}
