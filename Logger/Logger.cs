
namespace Logger
{
    public class PercentageCounter
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
                Console.Write($"{_total:N0}% / ");
            }
        }
    }
}