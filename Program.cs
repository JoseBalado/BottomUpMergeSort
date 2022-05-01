using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UserData;
using static Utils.Helpers;
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

        PercentageCounter percentageCounter = new PercentageCounter(numberOfTasks);

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

            BottomUpMergeSort.Sort(concurrentDictionary)
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
public static class Extensions
{
    public static IEnumerable<IEnumerable<T>> SplitArrayIntoArrays<T>(this T[] arr, int numberOfWords)
    {
        for (var i = 0; i < arr.Length / numberOfWords + 1; i++) {
            yield return arr.Skip(i * numberOfWords).Take(numberOfWords);
        }
    }
}

class BottomUpMergeSort
{
    public static List<DataFormat> Sort(ConcurrentDictionary<string, int> concurrentDictionary)
    {
       var blockingCollection = new List<DataFormat>(concurrentDictionary.Count);

        concurrentDictionary
            .ToList()
            .ForEach(element => blockingCollection.Add(new DataFormat { word = element.Key, occurrences = element.Value }));

        var tasks = new List<Task>();


        int N = concurrentDictionary.Count;
        for (int sz = 1; sz < N; sz = sz + sz)
        {
            List<DataFormat> auxBC = new List<DataFormat>();

            blockingCollection
                .ToList()
                .ForEach(element => auxBC.Add(new DataFormat { word = element.word, occurrences = element.occurrences }));

            // sz: subarray size
            for (int lo = 0; lo < N - sz; lo += sz + sz) // lo: subarray index
            {
                // tasks.Add(Task.Run(() => Merge(blockingCollection, auxBC, lo, lo + sz-1, Math.Min(lo + sz + sz -1, N - 1))));
                Merge(blockingCollection, auxBC, lo, lo + sz-1, Math.Min(lo + sz + sz -1, N - 1));
                // await Task.WhenAll(tasks.ToArray());
            }
                Console.WriteLine("Hello World");
            // await Task.WhenAll(tasks.ToArray());
        }

        return blockingCollection;
    }

    public static void Merge(List<DataFormat> blockingCollection, List<DataFormat> auxBC, int lo, int mid, int hi)
    {
        int i = lo, j = mid + 1;

        for (int k = lo; k <= hi; k++)
        {
            if (i > mid)
            {
                blockingCollection[k].word = auxBC[j].word;
                blockingCollection[k].occurrences = auxBC[j].occurrences;
                j++;
            }
            else if (j > hi)
            {
                blockingCollection[k].word = auxBC[i].word;
                blockingCollection[k].occurrences = auxBC[i].occurrences;
                i++;
            }
            else if (auxBC[j].occurrences > auxBC[i].occurrences)
            {
                blockingCollection[k].word = auxBC[j].word;
                blockingCollection[k].occurrences = auxBC[j].occurrences;
                j++;
            }
            else
            {
                blockingCollection[k].word = auxBC[i].word;
                blockingCollection[k].occurrences = auxBC[i].occurrences;
                i++;
            }
        }
    }
}
