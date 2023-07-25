using Dapper;
using MySql.Data.MySqlClient;

namespace JLookDataMigration.Helpers;

public class BlogHelper
{
    public static HashSet<long> GetBlogIdHash()
    {
        const string sql = @"SELECT blogid from pre_home_blog";

        var blogIdHash = CommonHelper.WatchTime(nameof(GetBlogIdHash)
                                              , () =>
                                                {
                                                    using var conn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);

                                                    var blogIdHash = conn.Query<long>(sql)
                                                                         .ToHashSet();

                                                    return blogIdHash;
                                                });

        return blogIdHash;
    }
}