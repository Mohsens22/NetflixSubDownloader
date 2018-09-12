using NetSub.Core;
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Crawler.Crawl(2);
            Console.ReadLine();
        }
    }
}
