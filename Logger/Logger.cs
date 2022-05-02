
namespace Logger
{
    interface ILogger
    {
        void Add();
        void Finish();
    }
    public class PercentageLogger : ILogger
    {
        private int _numberOfTasks;
        private decimal _total = 0;

        public PercentageLogger(int numberOfTasks)
        {
            _numberOfTasks = numberOfTasks;
        }

        public void Add()
        {
            lock (this)
            {
                var newTotal = _total + 100 / _numberOfTasks;
                if(newTotal > 99) newTotal = 99;
                if (Decimal.Truncate(newTotal) == Decimal.Truncate(_total))
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
