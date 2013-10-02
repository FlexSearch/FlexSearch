namespace FlexSearch.Benchmark
{
    using System;

    internal class Program
    {
        #region Methods

        private static void Main(string[] args)
        {
            //new WikipediaTestDataGenerator(@"F:\wikipedia\extracted", false, 1, true);
            //new WikipediaTestDataGenerator(@"F:\wikipedia\output", false, 4, true);
            //new WikipediaTestDataGenerator(@"F:\wikipedia\output", true, 0, true);
            new WikipediaIndexingPluginTests(@"F:\wikipedia\wikidump1KB.txt");
            Console.ReadLine();
        }

        #endregion
    }
}