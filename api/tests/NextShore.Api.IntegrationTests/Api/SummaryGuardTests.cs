using NextShore.Application.Assessments;

namespace NextShore.Api.IntegrationTests.Api;

public class SummaryGuardTests
{
    [Theory]
    [InlineData("You have a good chance with Express Entry.", "chance")]
    [InlineData("Your chances improve with French.", "chances")]
    [InlineData("The probability of approval is high.", "probability")]
    [InlineData("You will probably qualify.", "probably")]
    [InlineData("The odds favour Germany.", "odds")]
    [InlineData("A Blue Card is guaranteed.", "guaranteed")]
    [InlineData("You will get the 189 visa.", "You will get")]
    [InlineData("You meet 85% of the target.", "%")]
    public void Odds_and_promises_are_banned(string text, string expected)
    {
        Assert.Equal(expected, SummaryGuard.FindBannedPhrase(text));
    }

    [Fact]
    public void Readiness_and_grades_are_allowed()
    {
        Assert.Null(SummaryGuard.FindBannedPhrase(
            "Your best match is the EU Blue Card in Germany: Eligible, Readiness 100 / 100. Changes in your profile matter."));
    }

    [Fact]
    public void Text_within_the_limit_is_kept()
    {
        Assert.Equal("Short summary.", SummaryGuard.FitToLimit("  Short summary.  "));
    }

    [Fact]
    public void Long_text_is_cut_after_the_last_whole_sentence_that_fits()
    {
        var sentence = string.Join(' ', Enumerable.Repeat("word", 49)) + " end.";   // 50 words
        var text = string.Join(' ', Enumerable.Repeat(sentence, 5));                   // 250 words

        var fitted = SummaryGuard.FitToLimit(text)!;

        Assert.Equal(200, fitted.Split(' ').Length);
        Assert.EndsWith("end.", fitted);
    }

    [Fact]
    public void Long_text_without_a_sentence_break_does_not_fit()
    {
        Assert.Null(SummaryGuard.FitToLimit(string.Join(' ', Enumerable.Repeat("word", 250))));
    }
}
