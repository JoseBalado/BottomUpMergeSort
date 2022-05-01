using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

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
        var fileName = "Sample.txt"; // Console.ReadLine() ?? "";
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

        var numberOfWords = 5;
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

            // (await BottomUpMergeSort.Sort(concurrentDictionary))
            //     .ToList()
            //     .ForEach(element => Console.WriteLine($"{element.word, -20} {element.occurrences }"));

            var myList = concurrentDictionary
                .ToList()
                .Select(element => new WordOccurrences { word = element.Key, occurrences = element.Value });
            BottomUpMergeSort.MergeSortRecursive(myList.ToList(), 0, concurrentDictionary.Count - 1)
                .ToList()
                .ForEach(element => Console.WriteLine($"{element.word, -20} {element.occurrences}"));
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
    static string ReadFile(string fileName)
    {
        try
        {
            // Open the text file using a stream reader.
            using (var sr = new StreamReader(fileName))
            {
                // Read the stream as a string, and write the string to the console.
                return sr.ReadToEnd().ToString();
            }
        }
        catch (IOException e)
        {
            Console.WriteLine("The file could not be read:");
            Console.WriteLine(e.Message);
            return "";
        }
    }

    static void ProcessArray(List<string> arr, ConcurrentDictionary<string, int> concurrentDictionary, PercentageCounter counter, CancellationToken ct)
    {
        // Was cancellation already requested?
        if (ct.IsCancellationRequested)
        {
            Console.WriteLine("Task cancelled.");
            ct.ThrowIfCancellationRequested();
        }

        // Thread.Sleep(1000);

        foreach(string word in arr)
        {
            concurrentDictionary.AddOrUpdate(
                word,
                1,
                (key, value) => ++value
            );
        }

        counter.Add();

        if (ct.IsCancellationRequested)
        {
            Console.WriteLine("Task cancelled");
            ct.ThrowIfCancellationRequested();
        }
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

class PercentageCounter
{
    private int _numberOfTasks;
    private float _total = 0;

    public PercentageCounter(int numberOfTasks)
    {
        _numberOfTasks = numberOfTasks;
    }

    public void Add()
    {
        lock (this)
        {
            _total = _total + 100 / (float)_numberOfTasks;
            Console.Write($"{_total:N1}% / ");
        }
    }
}

class WordOccurrences
{
    public string word;
    public int occurrences;

}

class BottomUpMergeSort
{
    public static List<WordOccurrences> MergeSortRecursive(List<WordOccurrences> data, int left, int right)
    {
        if (left < right)
        {
            int m = left + (right - left) / 2;

            MergeSortRecursive(data, left, m);
            MergeSortRecursive(data, m + 1, right);
            return Merge(data, left, right);
        }
        return data;
    }
    private static List<WordOccurrences> Merge(List<WordOccurrences> myList, int left, int right)
    {
        return myList
            .Skip(left)
            .Take(right - left)
            .OrderByDescending(element => element.occurrences)
            .ToList();
    }

    // private static BlockingCollection<WordOccurrences> auxBC = new BlockingCollection<WordOccurrences>();

    public static async Task<BlockingCollection<WordOccurrences>> Sort(ConcurrentDictionary<string, int> concurrentDictionary)
    {
       var blockingCollection = new BlockingCollection<WordOccurrences>(concurrentDictionary.Count);

        concurrentDictionary
            .ToList()
            .ForEach(element => blockingCollection.Add(new WordOccurrences { word = element.Key, occurrences = element.Value }));

        var tasks = new List<Task>();


        int N = concurrentDictionary.Count;
        for (int sz = 1; sz < N; sz = sz + sz)
        {
            // sz: subarray size
            for (int lo = 0; lo < N - sz; lo += sz + sz) // lo: subarray index
            {
                tasks.Add(Task.Run(() => merge(blockingCollection, lo, lo + sz-1, Math.Min(lo + sz + sz -1, N - 1))));
                await Task.WhenAll(tasks.ToArray());
            }
            // await Task.WhenAll(tasks.ToArray());
        }

        return blockingCollection;
    }

    public static void merge(BlockingCollection<WordOccurrences> blockingCollection, int lo, int mid, int hi)
    {
        int i = lo, j = mid + 1;

        BlockingCollection<WordOccurrences> auxBC = new BlockingCollection<WordOccurrences>();

        blockingCollection
            .ToList()
            .ForEach(element => auxBC.Add(new WordOccurrences { word = element.word, occurrences = element.occurrences }));

        for (int k = lo; k <= hi; k++)
        {
            if (i > mid)
            {
                blockingCollection.ElementAt(k).word = auxBC.ElementAt(j).word;
                blockingCollection.ElementAt(k).occurrences = auxBC.ElementAt(j).occurrences;
                j++;
            }
            else if (j > hi)
            {
                blockingCollection.ElementAt(k).word = auxBC.ElementAt(i).word;
                blockingCollection.ElementAt(k).occurrences = auxBC.ElementAt(i).occurrences;
                i++;
            }
            else if (auxBC.ElementAt(j).occurrences > auxBC.ElementAt(i).occurrences)
            {

                blockingCollection.ElementAt(k).word = auxBC.ElementAt(j).word;
                blockingCollection.ElementAt(k).occurrences = auxBC.ElementAt(j).occurrences;
                j++;
            }
            else
            {
                blockingCollection.ElementAt(k).word = auxBC.ElementAt(i).word;
                blockingCollection.ElementAt(k).occurrences = auxBC.ElementAt(i).occurrences;
                i++;
            }
        }
    }
}
