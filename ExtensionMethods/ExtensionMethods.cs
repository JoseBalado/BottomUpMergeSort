namespace ExtensionMethods
{
    public static class Extensions
    {
        public static IEnumerable<IEnumerable<T>> SplitArrayIntoArrays<T>(this T[] arr, int numberOfWords)
        {
            for (var i = 0; i < arr.Length / numberOfWords + 1; i++)
            {
                yield return arr.Skip(i * numberOfWords).Take(numberOfWords);
            }
        }
    }
}