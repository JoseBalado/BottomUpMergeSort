using System.Collections.Concurrent;
using UserData;

namespace Algorithms
{
    class BottomUpMergeSort
    {
        public static List<DataFormat> Sort(ConcurrentDictionary<string, int> concurrentDictionary, ILogger logger, CancellationToken ct)
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
                    if (ct.IsCancellationRequested)
                    {
                        Console.WriteLine("Task cancelled.");
                        ct.ThrowIfCancellationRequested();
                    }

                    Merge(blockingCollection, auxBC, lo, lo + sz - 1, Math.Min(lo + sz + sz - 1, N - 1));
                }

                logger.Add();
                // Thread.Sleep(500);
            }

            logger.Finish();
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
}
