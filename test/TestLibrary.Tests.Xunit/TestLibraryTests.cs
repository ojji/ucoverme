using System.Threading.Tasks;
using Xunit;

namespace TestLibrary.Tests.Xunit
{
    public class TestLibraryTests
    {
        [Theory]
        [InlineData(2, 2, 4)]
        [InlineData(3, 4, 7)]
        public void Two_plus_two_should_be_four(int first, int second, int result)
        {
            var subject = new TestLibrary();
            Assert.Equal(result, subject.Add(first, second));
        }

        [Fact]
        public void TernaryIfTest()
        {
            var subject = new TestLibrary();
            Assert.Equal(0, subject.TernaryIfTest(2, 3));
        }

        [Fact]
        public async Task TaskTest()
        {
            var subject = new TaskTest();
            var elapsedTime = await subject.Run();
            Assert.True(elapsedTime < 9000);
        }
    }
}
