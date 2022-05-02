using System.Collections.Concurrent;
using UserData;
using static Utils.Helpers;
using static Algorithms.BottomUpMergeSort;
using ExtensionMethods;
using Logger;

public class Example
{
    public static async Task Main(string[] args)
    {
        var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        Console.WriteLine("\nPlease, enter filename ...");
        var fileName = Console.ReadLine() ?? "";
        Console.WriteLine("Reading file...");
        var text = ReadFile(fileName);

        Console.WriteLine("Press any key to begin tasks.");
        Console.WriteLine("After the program starts press 'c' to cancel.");
        Console.ReadKey(true);
        Console.WriteLine();

        Console.WriteLine("Spliting the array...");
        var wordsArray = text.Split();

        // Divide the total work to be done in several arrays.
        // We make depend the size of the arrays on the total number of proccessors.
        // But any other number could have been chosen.
        var numberOfWordsPerArray = wordsArray.Length / Environment.ProcessorCount * 4;

        // Total number of tasks that will be created is used by the logger to show
        // an approximate percentage of work done as a guess.
        var numberOfTasks = wordsArray.Length / numberOfWordsPerArray + 1;
        if (numberOfTasks == 0) numberOfTasks = 1;

        // Number of simultaneous tasks for proccessing words by occurrence.
        // Number chosen depends on number of proccessors.
        var concurrencyLevel = Environment.ProcessorCount;
        var concurrentDictionary = new ConcurrentDictionary<string, int>(concurrencyLevel, wordsArray.Count());

        Console.WriteLine("Spliting the array into various arrays...");
        var arrays = wordsArray.SplitArrayIntoArrays(numberOfWordsPerArray);

        ILogger percentageCounter = new PercentageLogger(numberOfTasks);

        var sortedArray = new List<DataFormat>();
        Console.Write("Start processing: ");

        Task t = Task.Run(async () =>
        {
            var tasks = new ConcurrentBag<Task>();
            // Do not launch more Tasks than concurrencyLevel indicates.
            int counter = 0;
            foreach (var array in arrays)
            {
                if(counter < concurrencyLevel)
                {
                    tasks.Add(Task.Run(() => ProcessArray(array.ToList(), concurrentDictionary, percentageCounter, token), token));
                    counter ++;
                }
                else
                {
                    await Task.WhenAll(tasks.ToArray());
                    counter = 0;
                }
            }

            await Task.WhenAll(tasks.ToArray());
            percentageCounter.Finish();

            Console.WriteLine();
            Console.Write("Sorting results: ");

            sortedArray = Sort(concurrentDictionary, new PercentageLogger((int)Math.Log(wordsArray.Length)), token);
            Console.WriteLine("Press any key to show results");
        }, token);

        char ch = Console.ReadKey().KeyChar;
        if (ch == 'c' || ch == 'C')
        {
            tokenSource.Cancel();
            Console.WriteLine("\nTask cancellation requested.\n");
        }

        try
        {
            await t;
            Console.WriteLine($"\n{"Word", -20} Occurrence");
            sortedArray
                .ToList()
                .ForEach(element => Console.WriteLine($"{element.word, -20} {element.occurrences }"));
            Console.WriteLine("\t -- End --");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"\n{nameof(OperationCanceledException)} thrown\n");
        }
        finally
        {
            tokenSource.Dispose();
        }
    }
}
