using System;

namespace Lercher
{
    public class SampleClient
    {
        private Random rnd;
        private int[] sequential;

        public void Run(int rounds, int count, int sides)
        {
            rnd = new Random(0); // use a seed to be reproducable
            sequential = new int[count * sides];

            for(var i = 0; i < rounds; i++)
            {
                var dice = rollDice0(count, sides);
                sequential[dice]++;
            }
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            for(var i = 0; i < sequential.Length; i++)
                sb.AppendFormat("{0,3:n0} -> {1,5:n0}\n", i, sequential[i]);
            return sb.ToString();
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
