using Dapper;
using Lctech.JLook.Core.Domain.Entities;
using Npgsql;

namespace JLookDataMigration.Helpers;

public class MassageHelper
{
    public static HashSet<long> GetMassageArticleIdHash()
    {
        const string sql = @"SELECT ""Id"" FROM ""Article""
        WHERE ""BoardId"" = 1128 AND ""VisibleTime"" <= (select extract(epoch from now()))";

        var idHash = CommonHelper.WatchTime(nameof(GetMassageArticleIdHash)
                                          , () =>
                                            {
                                                using var conn = new NpgsqlConnection(Setting.NEW_FORUM_CONNECTION);

                                                var idHash = conn.Query<long>(sql).ToHashSet();

                                                return idHash;
                                            });

        return idHash;
    }

    public static MassageBlog[] QueryBlogMassages(IEnumerable<long> articleIds)
    {
        const string sql = $@"SELECT a.""Id"" AS {nameof(MassageBlog.Id)}, 
                                     a.""Title"" AS {nameof(MassageBlog.Title)}, 
                                     a.""Cover"" AS {nameof(MassageBlog.CoverId)}, 
                                     a.""PinExpirationDate"" AS {nameof(MassageBlog.ExpirationDate)},
                                     a.""CreationDate"" AS {nameof(MassageBlog.CreationDate)},
                                     a.""CreatorId"" AS {nameof(MassageBlog.CreatorId)},
                                     asm.""ContentSummary"" AS {nameof(MassageBlog.Description)} 
                                     FROM ""Article"" a
                                     INNER JOIN ""ArticleSummary"" asm ON a.""Id"" = asm.""Id""
                                     WHERE a.""Id"" = ANY(@Ids)";

        using var cn = new NpgsqlConnection(Setting.NEW_FORUM_CONNECTION);

        var massageBlogs = cn.Query<MassageBlog>(sql, new { Ids = articleIds }).ToArray();

        return massageBlogs;
    }
}