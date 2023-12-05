using System.Text;
using System.Text.RegularExpressions;

namespace JKTankDataMigration.Extensions;

public static class StringExtension
{
    private static readonly Regex RegexImg = new("\\[(?<tag>(?:attach)?img)(?<attr>[^\\]]*)\\](?<content>[^\\[]+)\\[\\/(?:(?:attach)?img)]",
                                                 RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    public static List<string> GetExternalImageUrls(this string content)
    {
        var imageUrls = new List<string>();
        var matches = RegexImg.Matches(content);

        foreach (Match match in matches)
        {
            var contentResult = match.Groups["content"].Value.TrimStart();

            if (contentResult.StartsWith("http") && !contentResult.Contains("www.mymypic.net"))
                imageUrls.Add(contentResult);
        }

        return imageUrls;
    }

    public static string ToCopyText(this string? str)
    {
        if (string.IsNullOrEmpty(str))
            return str.ToCopyValue();

        var sb = new StringBuilder(str);

        sb.Replace(Setting.D, "");
        sb.Replace("\\", "\\\\");
        sb.Replace("\r", "\\r");
        sb.Replace("\n", "\\n");
        sb.Replace("\u0000", "");

        return sb.ToString();
    }

    public static string ToCopyArray(this string[] strs)
    {
        return strs.Length == 0 ? "{}" : $"{{{string.Join(",", strs.Where(x => !string.IsNullOrWhiteSpace(x))).ToCopyText()}}}";
    }

    public static string ToCopyArray(this IEnumerable<long> longs)
    {
        var stringArray = longs.Select(x => x.ToString()).ToArray();

        return stringArray.ToCopyArray();
    }
}