using Dapper;
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

    public static IEnumerable<MassageBlog> QueryBlogMassages(IEnumerable<long> articleIds)
    {
        var idStr = string.Join(",",articleIds);
        
        var sql = $@"SELECT a.""Id"" AS {nameof(MassageBlog.Id)}, 
                                     a.""Title"" AS {nameof(MassageBlog.Title)}, 
                                     a.""Cover"" AS {nameof(MassageBlog.CoverId)}, 
                                     a.""CategoryId"" AS {nameof(MassageBlog.RegionId)}, 
                                     a.""PinExpirationDate"" AS {nameof(MassageBlog.ExpirationDate)},
                                     a.""CreationDate"" AS {nameof(MassageBlog.CreationDate)},
                                     a.""CreatorId"" AS {nameof(MassageBlog.CreatorId)},
                                     a.""ContentSummary"" AS {nameof(MassageBlog.Description)} 
                                     FROM ""Article"" a
                                     WHERE a.""Id"" IN({idStr})";

        using var cn = new NpgsqlConnection(Setting.NEW_FORUM_CONNECTION);

        var massageBlogs = cn.Query<MassageBlog>(sql).ToArray();

        return massageBlogs;
    }
}