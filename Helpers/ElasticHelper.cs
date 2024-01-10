using Lctech.JKTank.Core.Documents;
using Netcorext.Extensions.Commons;

namespace JKTankDataMigration.Helpers;

public static class ElasticHelper
{
    public static string GetBlogIndex(string? prefix = null)
    {
        const string documentName = nameof(Blog);

        return GetIndex(prefix, documentName);
    }

    public static string GetCommentIndex(string? prefix = null)
    {
        const string documentName = nameof(Comment);

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

    private static string GetIndex(string? prefix, string documentName)
    {
        return prefix.IsEmpty()
                   ? documentName.ToLower()
                   : $"{prefix}-{documentName.ToLower()}";
    }
}