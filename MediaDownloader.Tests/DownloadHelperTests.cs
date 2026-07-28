using MediaDownloader.Download.Utilities;

namespace MediaDownloader.Tests;

public class DownloadHelperTests
{
    [Theory]
    [InlineData("Plain Name", "Plain Name")]
    [InlineData("a/b\\c:d", "a_b_c_d")]
    [InlineData("quote\"pipe|question?star*", "quote_pipe_question_star_")]
    [InlineData("", "")]
    public void SanitizeFileName_ReplacesInvalidCharacters(string input, string expected)
    {
        Assert.Equal(expected, DownloadHelper.SanitizeFileName(input));
    }
}
