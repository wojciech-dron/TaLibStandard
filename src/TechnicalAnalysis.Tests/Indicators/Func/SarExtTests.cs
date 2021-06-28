using System.Linq;
using AutoFixture;
using FluentAssertions;
using Xunit;

namespace TechnicalAnalysis.Tests.Indicators.Func
{
    public class SarExtTests
    {
        [Fact]
        public void SarExtDouble()
        {
            // Arrange
            Fixture fixture = new();
            const int StartIdx = 0;
            const int EndIdx = 99;
            double[] high = fixture.CreateMany<double>(count: 100).ToArray();
            double[] low = fixture.CreateMany<double>(count: 100).ToArray();
            
            // Act
            var actualResult = TAMath.SarExt(
                StartIdx,
                EndIdx,
                high,
                low);

            // Assert
            actualResult.Should().NotBeNull();
            actualResult.RetCode.Should().Be(RetCode.Success);
        }
        
        [Fact]
        public void SarExtFloat()
        {
            // Arrange
            Fixture fixture = new();
            const int StartIdx = 0;
            const int EndIdx = 99;
            double[] high = fixture.CreateMany<double>(count: 100).ToArray();
            double[] low = fixture.CreateMany<double>(count: 100).ToArray();
            
            // Act
            var actualResult = TAMath.SarExt(
                StartIdx,
                EndIdx,
                high,
                low);

            // Assert
            actualResult.Should().NotBeNull();
            actualResult.RetCode.Should().Be(RetCode.Success);
        }
    }
}