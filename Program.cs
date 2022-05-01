using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
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

        Console.WriteLine("Please, enter filename ...");
        var fileName = Console.ReadLine() ?? "";
        var text = ReadFile(fileName);

        Console.WriteLine("Press any key to begin tasks...");
        Console.WriteLine("To terminate the example, press 'c' to cancel and exit...");
        Console.ReadKey(true);
        Console.WriteLine();

        // Split the text in as many arrays as proccessors.
        var wordsArray = text.Split();

        var numberOfWordsPerArray = wordsArray.Length / 32;
        var numberOfTasks = wordsArray.Length / numberOfWordsPerArray + 1;
        if (numberOfTasks == 0) numberOfTasks = 1;
        var concurrencyLevel = Environment.ProcessorCount;
        var concurrentDictionary = new ConcurrentDictionary<string, int>(concurrencyLevel, wordsArray.Count());

        var arrays = wordsArray.SplitArrayIntoArrays(numberOfWordsPerArray);

        ILogger percentageCounter = new PercentageLogger(numberOfTasks);

        var sortedArray = new List<DataFormat>();
        Console.Write("Start processing: ");

        Task t = Task.Run(async () =>
        {
            var tasks = new ConcurrentBag<Task>();
            // Do not lunch more Tasks than concurrencyLevel indicates.
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
            Console.WriteLine("Task cancellation requested.");
        }

        try
        {
            await t;
            Console.WriteLine($"{"word", -20} occurrence");
            sortedArray
                .ToList()
                .ForEach(element => Console.WriteLine($"{element.word, -20} {element.occurrences }"));
            Console.WriteLine("\t -- End --");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"{nameof(OperationCanceledException)} thrown\n");
        }
        finally
        {
            tokenSource.Dispose();
        }
    }
}
