using System;
using System.Threading;

namespace Lercher
{
    public class SampleClient
    {
        private Random rnd;
        private int[] sequential;
        private int[] unlocked;
        private int[] monitored;
        private MonitorDictionary<int> transactions = new MonitorDictionary<int>();

        public void Run(int rounds, int count, int sides)
        {
            rnd = new Random(0); // use a seed to be reproducable
            int possibleSums0 = count * (sides - 1) + 1;
            sequential = new int[possibleSums0];
            unlocked = new int[possibleSums0];
            monitored = new int[possibleSums0];

            var cd = new CountdownEvent(rounds * 2);
            for (var i = 0; i < rounds; i++)
            {
                var dice = rollDice0(count, sides);
                
                // sequential
                sequential[dice]++;

                // unlocked
                ThreadPool.QueueUserWorkItem((o) =>
                    {
                        unlocked[dice]++;
                        cd.Signal();
                    }
                );

                // monitored
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    using(var tr = transactions.Use(dice))
                        monitored[dice]++;
                    cd.Signal();
                });
            }
            cd.Wait();
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            DescribeTo(sb, sequential, nameof(sequential));
            DescribeTo(sb, unlocked, nameof(unlocked));
            DescribeTo(sb, monitored, nameof(monitored));
            return sb.ToString();
        }

        private void DescribeTo(System.Text.StringBuilder sb, int[] arr, string title)
        {
            sb.AppendLine(title);
            sb.AppendLine("-----------");
            for (var i = 0; i < arr.Length; i++)
                sb.AppendFormat("{0,3:n0} -> {1,5:n0}{2}\n", i, arr[i], arr[i]==sequential[i] ? "" : " !");
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
