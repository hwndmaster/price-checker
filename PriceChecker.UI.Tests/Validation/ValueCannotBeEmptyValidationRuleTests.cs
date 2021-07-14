using System.Globalization;
using AutoFixture;
using Genius.PriceChecker.UI.Validation;
using Xunit;

namespace Genius.PriceChecker.UI.Tests.Validation
{
    public class ValueCannotBeEmptyValidationRuleTests
    {
        private readonly Fixture _fixture = new();
        private readonly ValueCannotBeEmptyValidationRule _sut = new();

        [Fact]
        public void Value__Null__Returns_not_valid()
        {
            // Act
            var result = _sut.Validate(null, _fixture.Create<CultureInfo>());

            // Verify
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Value__Not_string__Returns_not_valid()
        {
            // Act
            var result = _sut.Validate(new object(), _fixture.Create<CultureInfo>());

            // Verify
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Value__Null_string__Returns_not_valid()
        {
            // Arrange
            string value = null;

            // Act
            var result = _sut.Validate(value, _fixture.Create<CultureInfo>());

            // Verify
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Value__Empty_string__Returns_not_valid()
        {
            // Arrange
            string value = string.Empty;

            // Act
            var result = _sut.Validate(value, _fixture.Create<CultureInfo>());

            // Verify
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Value__Whitespaced_string__Returns_not_valid()
        {
            // Arrange
            string value = "   ";

            // Act
            var result = _sut.Validate(value, _fixture.Create<CultureInfo>());

            // Verify
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Value__String__Returns_valid()
        {
            // Arrange
            string value = _fixture.Create<string>();

            // Act
            var result = _sut.Validate(value, _fixture.Create<CultureInfo>());

            // Verify
            Assert.True(result.IsValid);
        }
    }
}
