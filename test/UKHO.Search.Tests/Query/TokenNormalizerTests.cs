using Shouldly;
using UKHO.Search.Query;
using Xunit;

namespace UKHO.Search.Tests.Query
{
    public sealed class TokenNormalizerTests
    {
        private readonly TokenNormalizer _subject = new();

        [Theory]
        [InlineData("Foo", "foo")]
        [InlineData("  Bar  ", "bar")]
        [InlineData("S-100", "s-100", "s100")]
        [InlineData("s-57", "s-57", "s57")]
        [InlineData("s-63", "s-63", "s63")]
        [InlineData("s-100x", "s-100x")]
        [InlineData("s-101", "s-101", "s101")]
        [InlineData("s-abc", "s-abc")]
        [InlineData("s100", "s100")]
        public void NormalizeToken_returns_expected_values(string? token, params string[] expected)
        {
            var actual = _subject.NormalizeToken(token)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            actual.ShouldBe(expected.OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void NormalizeToken_returns_empty_for_invalid_input(string? token)
        {
            var actual = _subject.NormalizeToken(token);

            actual.ShouldBeEmpty();
        }
    }
}
