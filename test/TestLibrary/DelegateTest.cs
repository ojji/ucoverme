using System.Threading.Tasks;

namespace TestLibrary
{
    class DelegateTest
    {
        public void HasSimpleTaskWithLambda()
        {
            var t = new Task(() => { });
        }
    }

}
