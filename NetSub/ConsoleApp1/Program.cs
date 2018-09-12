using NetSub.Core;
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Crawling");
            Crawler.Crawl(1457);
            Console.ReadLine();
        }
    }
}
