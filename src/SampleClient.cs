using System;
using System.Diagnostics;
using System.Threading;

namespace Lercher
{
    public class SampleClient
    {
        private Random rnd;
        private int[] sequential;
        private int[] unlocked;
        private int[] monitored;
        private int[] globallock;

        private MonitorDictionary<int> transactions = new MonitorDictionary<int>();

        private void operate(int[] arr, int dice, TimeSpan runtime)
        {
            var count = arr[dice];
            if (runtime > TimeSpan.Zero)
                Thread.Sleep(runtime);
            arr[dice] = count + 1;
        }
        public void Run(int rounds, int count, int sides, TimeSpan runtime)
        {
            int possibleSums0 = count * (sides - 1) + 1;
            sequential = new int[possibleSums0];
            unlocked = new int[possibleSums0];
            monitored = new int[possibleSums0];
            globallock = new int[possibleSums0];

            Run(nameof(sequential), rounds, count, sides, (dice, cd) => 
            {
                operate(sequential, dice, runtime);
                cd.Signal();
            });

            Run(nameof(unlocked), rounds, count, sides, (dice, cd) => ThreadPool.QueueUserWorkItem((o) =>
            {
                operate(unlocked, dice, runtime);
                cd.Signal();
            }));

            Run(nameof(monitored), rounds, count, sides, (dice, cd) => ThreadPool.QueueUserWorkItem((o) =>
            {
                using (transactions.Guard(dice))
                    operate(monitored, dice, runtime);
                cd.Signal();
            }));

            Run(nameof(globallock), rounds, count, sides, (dice, cd) => ThreadPool.QueueUserWorkItem((o) =>
            {
                lock (globallock)
                    operate(globallock, dice, runtime);
                cd.Signal();
            }));
        }

        private void Run(string name, int rounds, int count, int sides, Action<int, CountdownEvent> each)
        {
            var sw = Stopwatch.StartNew();
            var cd = new CountdownEvent(rounds);
            rnd = new Random(0); // use a seed to be reproducable
            for (var i = 0; i < rounds; i++)
            {
                var dice = rollDice0(count, sides);
                each(dice, cd);
            }
            cd.Wait();
            sw.Stop();
            System.Console.WriteLine("{0}: {1}", name, sw.Elapsed);
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            DescribeTo(sb, sequential, nameof(sequential));
            DescribeTo(sb, unlocked, nameof(unlocked));
            DescribeTo(sb, monitored, nameof(monitored));
            DescribeTo(sb, globallock, nameof(globallock));
            return sb.ToString();
        }

        private void DescribeTo(System.Text.StringBuilder sb, int[] arr, string title)
        {
            sb.AppendLine(title);
            sb.AppendLine("-----------");
            for (var i = 0; i < arr.Length; i++)
                sb.AppendFormat("{0,3:n0} -> {1,5:n0}{2}\n", i, arr[i], arr[i] == sequential[i] ? "" : " !");
            sb.AppendLine();
        }

        private int rollDice0(int count, int sides)
        {
            var sum = 0;
            for (var i = 0; i < count; i++)
                sum += rnd.Next(sides);
            return sum;
        }

    }

}
