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

        // Store references to the tasks so that we can wait on them and
        // observe their status after cancellation.
        // Task t;

        Console.WriteLine("Please, enter filename ...");
        var fileName = "sdd.txt"; // Console.ReadLine() ?? "";
        Console.WriteLine();

        Console.WriteLine("Press any key to begin tasks...");
        Console.WriteLine("To terminate the example, press 'c' to cancel and exit...");
        Console.ReadKey(true);
        Console.WriteLine();

        // Try asynchronous reading
        var text = ReadFile(fileName);
        // var concurrentDictionary = new ConcurrentDictionary<string, int>();

        // Split the text in as many arrays as proccessors.
        var wordsArray = text.Split();

        var numberOfWords = 500;
        var numberOfTasks = wordsArray.Length / numberOfWords + 1;
        var concurrencyLevel = Environment.ProcessorCount / 2;
        var concurrentDictionary = new ConcurrentDictionary<string, int>(concurrencyLevel, wordsArray.Count());

        var arrays = wordsArray.SplitArrayIntoArrays(numberOfWords);

        ILogger percentageCounter = new PercentageLogger(numberOfTasks);

        Console.WriteLine("Start processing");

        Task t = Task.Run(async () =>
        {

            var tasks = new ConcurrentBag<Task>();
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
            Console.WriteLine("100%");

            Console.WriteLine();
            Console.WriteLine("Sorting results.");
            Console.WriteLine($"{"word", -20} occurrence");
            // concurrentDictionary
            //     .OrderByDescending(element => element.Value)
            //     .ToList()
            //     .ForEach(element => Console.WriteLine($"{element.Key, -20} {element.Value}"));

            Sort(concurrentDictionary)
                .ToList()
                .ForEach(element => Console.WriteLine($"{element.word, -20} {element.occurrences }"));
        }, token);

        // Request cancellation from the UI thread.
        char ch = Console.ReadKey().KeyChar;
        if (ch == 'c' || ch == 'C')
        {
            tokenSource.Cancel();
            Console.WriteLine("\nTask cancellation requested.");

            // Optional: Observe the change in the Status property on the task.
            // It is not necessary to wait on tasks that have canceled. However,
            // if you do wait, you must enclose the call in a try-catch block to
            // catch the TaskCanceledExceptions that are thrown. If you do
            // not wait, no exception is thrown if the token that was passed to the
            // Task.Run method is the same token that requested the cancellation.
        }

        try
        {
            await t;
            Console.WriteLine("End");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"\n{nameof(OperationCanceledException)} thrown\n");
        }
        finally
        {
            tokenSource.Dispose();
        }

        // Display status of all tasks.
        // foreach (var task in tasks)
        //     Console.WriteLine("Task {0} status is now {1}", task.Id, task.Status);
    }
}
