using System.Text.RegularExpressions;

namespace JKTankDataMigration.Helpers;

public static class KeywordHelper
{
    // emoji range \u1f600-\u1f64f\u200d-\u27bf
    private static readonly Regex RegexSymbols = new(@"[^\u4e00-\u9fa5a-zA-Z0-9\.\-:\/]+", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    private static readonly Regex RegexSymbols2 = new(@"[^\u4e00-\u9fa5a-zA-Z0-9\.\-:\/]{2,}", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    private static readonly Regex RegexMedia = new(@"\[(?:img|video|embed|file|stick)[^\]]*\].*\[\/(?:img|video|embed|file|stick)[^\]]*\]", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    private static readonly Regex RegexBbc = new(@"\[\/?[^\]]+\]", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    private static readonly Regex RegexHtml = new(@"<\/?[^>]+>", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    private static readonly Regex RegexEncodeHtml = new(@"&[^;]+;", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    public static string ReplaceEsWords(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        var temp = content;

        temp = RegexHtml.Replace(temp, "");
        temp = RegexMedia.Replace(temp, "");
        temp = RegexBbc.Replace(temp, "");
        temp = RegexEncodeHtml.Replace(temp, "");
        temp = RegexSymbols2.Replace(temp, "");
        temp = RegexSymbols.Replace(temp, "𢡄");

        return temp.Trim();
    }
}