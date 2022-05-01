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
                using (var sr = new StreamReader($"text_files{Path.DirectorySeparatorChar}{fileName}"))
                {
                    return sr.ReadToEnd().ToString();
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                Console.WriteLine("\t ------ Ending the application ------");
                throw;
            }
        }

        public static void ProcessArray(List<string> arr, ConcurrentDictionary<string, int> concurrentDictionary, ILogger counter, CancellationToken ct)
        {
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
        }
    }
}
