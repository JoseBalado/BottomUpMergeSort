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
        var tasks = new ConcurrentBag<Task>();

        Console.WriteLine("Please, enter filename ...");
        var fileName = "Sample.txt"; // Console.ReadLine() ?? "";
        Console.WriteLine("To terminate the example, press 'c' to cancel and exit...");
        Console.WriteLine();

        Console.WriteLine("Press any key to begin tasks...");
        Console.ReadKey(true);
        Console.WriteLine("To terminate the example, press 'c' to cancel and exit...");
        Console.WriteLine();

        // Try asynchronous reading
        var text = ReadFile(fileName);
        var concurrentDictionary = new ConcurrentDictionary<string, int>();

        // Split the text in as many arrays as proccessors.
        var wordsArray = text.Split();

        var processorCount = Environment.ProcessorCount;
        var size = wordsArray.Length / (processorCount - 2);
        if (size == 0) size = 1;
        var numberOfTasks = wordsArray.Length / size + 1;
        var arrays = wordsArray.SplitArrayIntoArrays(size);

        PercentageCounter counter = new PercentageCounter(numberOfTasks);

        foreach (var array in arrays)
        {
            Console.WriteLine(String.Join(", ", array));
        }

        Console.WriteLine("Start processing");

        foreach (var array in arrays)
        {
            tasks.Add(Task.Run(() => ProcessFile(array.ToList(), concurrentDictionary, counter, token), token));
        }

        /*

        tasks.Add(t);

        // Request cancellation of a task and its children. Note the token is passed
        // to (1) the user delegate and (2) as the second argument to Task.Run, so
        // that the task instance can correctly handle the OperationCanceledException.
        t = Task.Run(() =>
        {
            // Create some cancelable child tasks.
            Task tc;
            for (int i = 3; i <= 10; i++)
            {
                // For each child task, pass the same token
                // to each user delegate and to Task.Run.
                tc = Task.Run(() => ProcessFile(text, token), token);
                Console.WriteLine("Task {0} executing", tc.Id);
                tasks.Add(tc);
                // Pass the same token again to do work on the parent task.
                // All will be signaled by the call to tokenSource.Cancel below.
                ProcessFile(text, token);
            }
        }, token);

        Console.WriteLine("Task {0} executing", t.Id);
        tasks.Add(t);

        */


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
            await Task.WhenAll(tasks.ToArray());

            Console.WriteLine();
            Console.WriteLine("100% - Finished.");
            Console.WriteLine();

            Console.WriteLine($"{"word", -20} occurrence");
            concurrentDictionary
                .OrderByDescending(element => element.Value)
                .ToList()
                .ForEach(element => Console.WriteLine($"{element.Key, -20} {element.Value}"));
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
        foreach (var task in tasks)
            Console.WriteLine("Task {0} status is now {1}", task.Id, task.Status);
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

    static void ProcessFile(List<string> arr, ConcurrentDictionary<string, int> concurrentDictionary, PercentageCounter counter, CancellationToken ct)
    {
        // Was cancellation already requested?
        if (ct.IsCancellationRequested)
        {
            Console.WriteLine("Task cancelled.");
            ct.ThrowIfCancellationRequested();
        }

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
    public static IEnumerable<IEnumerable<T>> SplitArrayIntoArrays<T>(this T[] arr, int size)
    {
        for (var i = 0; i < arr.Length / size + 1; i++) {
            yield return arr.Skip(i * size).Take(size);
        }
    }
}

 class PercentageCounter
 {
        private int _numberOfTasks;
        private int _total = 0;

        public PercentageCounter(int numberOfTasks)
        {
            _numberOfTasks = numberOfTasks;
        }

        public void Add()
        {
            lock(this)
            {
                _total = _total + 100 / _numberOfTasks;
                Console.Write($"{_total}% / ");
            }
        }
    }
