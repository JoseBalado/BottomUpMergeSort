namespace Utils
{
    class Helper
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


    }
}