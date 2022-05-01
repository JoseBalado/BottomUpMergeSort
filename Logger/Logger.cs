
interface ILogger
{
    void Add();
    void Finish();
}
namespace Logger
{
    public class PercentageLogger : ILogger
    {
        private int _numberOfTasks;
        private float _total = 0;

        public PercentageLogger(int numberOfTasks)
        {
            _numberOfTasks = numberOfTasks;
        }

        public void Add()
        {
            lock (this)
            {
                var newTotal = _total + 100 / (float)_numberOfTasks;
                if (((int)newTotal) == ((int)_total))
                {
                    _total = newTotal;
                }
                else
                {
                    _total = newTotal;
                    Console.Write($"{_total:N0}% / ");
                }
            }
        }

        public void Finish()
        {
                Console.WriteLine("100%");
        }
    }
}
