using System.Collections.Concurrent;
using Logger;
namespace Utils
{
    class Helpers
    {
        public static string ReadFile(string fileName)
        {
            try
            {
                // Open the text file using a stream reader.
                using (var sr = new StreamReader($"text_files{Path.DirectorySeparatorChar}{fileName}"))
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

        public static void ProcessArray(List<string> arr, ConcurrentDictionary<string, int> concurrentDictionary, ILogger counter, CancellationToken ct)
        {
            // Was cancellation already requested?
            if (ct.IsCancellationRequested)
            {
                Console.WriteLine("Task cancelled.");
                ct.ThrowIfCancellationRequested();
            }

            // Thread.Sleep(1000);

            foreach (string word in arr)
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
}
