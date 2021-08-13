using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoFixture;
using Genius.Atom.UI.Forms.ViewModels;
using Genius.PriceChecker.UI.Validation;
using Xunit;

namespace Genius.PriceChecker.UI.Tests.Validation
{
    public class MustBeUniqueValidationRuleTests
    {
        private readonly Fixture _fixture = new();
        private readonly MustBeUniqueValidationRule _sut;
        private readonly TestViewModel _testVm = new();

        public MustBeUniqueValidationRuleTests()
        {
            _sut = new MustBeUniqueValidationRule(_testVm, nameof(TestViewModel.SampleSet));
        }

        [Fact]
        public void Value__Not_string__Returns_valid()
        {
            // Act
            var result = _sut.Validate(new object(), _fixture.Create<CultureInfo>());

            // Verify
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Value__Already_exists_twice_in_collection__Returns_not_valid()
        {
            // Arrange
            _testVm.SampleSet = _fixture.CreateMany<string>().ToList();
            _testVm.SampleSet.Add(_testVm.SampleSet[1]);

            // Act
            var result = _sut.Validate(_testVm.SampleSet[1], _fixture.Create<CultureInfo>());

            // Verify
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Value__Only_one_exists_in_collection__Returns_valid()
        {
            // Arrange
            _testVm.SampleSet = _fixture.CreateMany<string>().ToList();

            // Act
            var result = _sut.Validate(_testVm.SampleSet[1], _fixture.Create<CultureInfo>());

            // Verify
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Value__Not_exists_in_collection__Returns_valid()
        {
            // Arrange
            _testVm.SampleSet = _fixture.CreateMany<string>().ToList();
            var valueToValidate = _fixture.Create<string>();

            // Act
            var result = _sut.Validate(valueToValidate, _fixture.Create<CultureInfo>());

            // Verify
            Assert.True(result.IsValid);
        }

        class TestViewModel : ViewModelBase
        {
            public List<string> SampleSet { get; set; }
        }
    }
}
