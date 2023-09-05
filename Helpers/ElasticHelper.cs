using Lctech.JKTank.Core.Domain.Documents;
using Netcorext.Extensions.Commons;

namespace JKTankDataMigration.Helpers;

public static class ElasticHelper
{
    public const string WHITESPACE_ANALYZER = "whitespace";
    
    public static string GetBlogIndex(string? prefix = null)
    {
        const string documentName = nameof(Blog);

        return GetIndex(prefix, documentName);
    }

    public static string GetHashtagIndex(string? prefix = null)
    {
        const string documentName = nameof(Hashtag);

        return GetIndex(prefix, documentName);
    }

    public static string GetMemberIndex(string? prefix = null)
    {
        const string documentName = nameof(Member);

        return GetIndex(prefix, documentName);
    }

    public static string GetStatisticIndex(string? prefix = null)
    {
        const string documentName = nameof(Statistic);

        return GetIndex(prefix, documentName);
    }

    private static string GetIndex(string? prefix, string documentName)
        => prefix.IsEmpty()
               ? documentName.ToLower()
               : $"{prefix}-{documentName.ToLower()}";
}