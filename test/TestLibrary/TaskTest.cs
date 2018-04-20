using System.Diagnostics;
using System.Threading.Tasks;

namespace TestLibrary
{
    public class TaskTest
    {
        public async Task<long> Run()
        {
            var timer = Stopwatch.StartNew();

            var t1 = FirstTask();
            var t2 = SecondTask();
            var t3 = ThirdTask();

            await Task.WhenAll(t1, t2, t3);
            return timer.ElapsedMilliseconds;
        }

        public async Task FirstTask()
        {
            await Task.Delay(4000);
        }

        public async Task SecondTask()
        {
            await Task.Delay(2000);
        }

        public async Task ThirdTask()
        {
            await Task.Delay(3000);
        }
    }
}