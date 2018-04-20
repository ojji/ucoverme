using System.Threading.Tasks;
using NUnit.Framework;

namespace TestLibrary.Tests.Nunit
{
    [TestFixture]
    public class TestLibraryTests
    {
        [TestCase(2, 2, 4)]
        [TestCase(3, 4, 7)]
        public void Two_plus_two_should_be_four(int first, int second, int result)
        {
            var subject = new TestLibrary();
            var fullname = TestContext.CurrentContext.Test.FullName;
            Assert.That(subject.Add(first, second), Is.EqualTo(result));
        }

        [Test]
        public void TernaryIfTest()
        {
            var subject = new TestLibrary();
            Assert.That(subject.TernaryIfTest(2, 3), Is.EqualTo(0));
        }

        [Test]
        public async Task TaskTest()
        {
            var subject = new TaskTest();
            var elapsedTime = await subject.Run();
            Assert.That(elapsedTime, Is.LessThan(9000));
        }
    }
}
