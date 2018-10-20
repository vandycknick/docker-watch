using Xunit;

namespace DockerWatch.Test
{
    public class ProgramTest
    {

        [Fact]
        public void HasTheCorrectDefaultValuesSet()
        {
            // Arrange
            var program = new Program();

            // Assert
            Assert.False(program.Verbose);
            Assert.True(program.Container == "*");
        }

    }
}
