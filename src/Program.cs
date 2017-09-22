using System;

namespace Lercher
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("This is test program for the MonitorDictionary class.");
            System.Console.WriteLine("(C) 2018 by Martin Lercher");
            System.Console.WriteLine();
            var c2 = new SampleClient2();
            c2.Run(10, 100_000);
            System.Console.WriteLine(c2);
            
            var c = new SampleClient();
            c.Run(10_000, 2, 6, 1_000_000);
            System.Console.WriteLine(c);
        }
    }

}
