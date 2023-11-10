using Dapper;
using Npgsql;

namespace JKTankDataMigration.Helpers;

public class LookHelper
{
    ///<summary>
    ///在look內的所有blogId
    ///</summary>
    public static HashSet<long> GetLookBlogIdHash()
    {
        const string sql = @"SELECT ""Id"" FROM ""Blog""";

        var hashSet = CommonHelper.WatchTime(nameof(GetLookBlogIdHash)
                                           , () =>
                                             {
                                                 using var conn = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION);

                                                 var idHash = conn.Query<long>(sql).ToHashSet();

                                                 return idHash;
                                             });

        return hashSet;
    }
}