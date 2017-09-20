﻿using System;

namespace Lercher
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("This is test program for the MonitorDictionary class.");
            System.Console.WriteLine("(C) 2018 by Martin Lercher");
            System.Console.WriteLine();
            var c = new SampleClient();
            c.Run(100, 3, 6, TimeSpan.FromMilliseconds(10));
            System.Console.WriteLine(c);
        }
    }

}
