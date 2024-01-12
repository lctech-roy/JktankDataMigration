using Dapper;
using JKTankDataMigration.Models;
using Lctech.JKTank.Core.Domain.Entities;
using Npgsql;

namespace JKTankDataMigration.Helpers;

public class NewForumHelper
{
    public static HashSet<long>? GetMassageArticleIdHash(long? articleId)
    {
        var sql = @"SELECT ""Id"" FROM ""Article""
        WHERE ""BoardId"" = 1128 AND ""VisibleTime"" <= (select extract(epoch from now()))";

        if (articleId.HasValue)
            sql += """ AND "Id" = @articleId""";

        var idHash = CommonHelper.WatchTime(nameof(GetMassageArticleIdHash)
                                          , () =>
                                            {
                                                using var conn = new NpgsqlConnection(Setting.NEW_FORUM_CONNECTION);

                                                var idHash = conn.Query<long>(sql, new { articleId }).ToHashSet();

                                                return idHash;
                                            });

        return idHash;
    }

    public static IEnumerable<MassageArticle> QueryBlogMassages(IEnumerable<long> articleIds)
    {
        var idStr = string.Join(",", articleIds);

        var sql = $@"SELECT a.""Id"" AS {nameof(MassageArticle.Id)}, 
                                     a.""Title"" AS {nameof(MassageArticle.Title)}, 
                                     a.""Cover"" AS {nameof(MassageArticle.CoverId)}, 
                                     a.""CategoryId"" AS {nameof(MassageArticle.RegionId)}, 
                                     a.""PinExpirationDate"" AS {nameof(MassageArticle.ExpirationDate)},
                                     a.""CreationDate"" AS {nameof(MassageArticle.CreationDate)},
                                     a.""CreatorId"" AS {nameof(MassageArticle.CreatorId)},
                                     a.""ContentSummary"" AS {nameof(MassageArticle.Description)} 
                                     FROM ""Article"" a
                                     WHERE a.""Id"" IN ({idStr})";

        using var cn = new NpgsqlConnection(Setting.NEW_FORUM_CONNECTION);

        var massageBlogs = cn.Query<MassageArticle>(sql).ToArray();

        return massageBlogs;
    }
}