using Dapper;
using Npgsql;

namespace JKTankDataMigration.Helpers;

public class LookMemberHelper
{
    ///<summary>
    ///在look內的所有member
    ///</summary>
    public static HashSet<long> GetLookMemberIdHash()
    {
        const string sql = @"SELECT ""Id"" FROM ""Member""";

        var hashSet = CommonHelper.WatchTime(nameof(GetLookMemberIdHash)
                                           , () =>
                                             {
                                                 using var conn = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION);

                                                 var idHash = conn.Query<long>(sql).ToHashSet();

                                                 return idHash;
                                             });

        return hashSet;
    }

    ///<summary>
    ///在look內的所有member
    ///</summary>
    public static IEnumerable<long> GetLookMemberId()
    {
        const string sql = @"SELECT ""Id"" FROM ""Member""";

        var ids = CommonHelper.WatchTime(nameof(GetLookMemberId)
                                       , () =>
                                         {
                                             using var conn = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION);

                                             var ids = conn.Query<long>(sql);

                                             return ids;
                                         });

        return ids;
    }

    ///<summary>
    ///所有member的粉絲數
    ///</summary>
    public static Dictionary<long, int> GetLookMemberFollowerCountDic()
    {
        const string sql = @"SELECT ""Id"",COUNT(""Id"") AS FollowerCount 
                             FROM ""MemberRelation"" WHERE ""IsFollower"" = true
                             GROUP BY ""Id""";

        var hashSet = CommonHelper.WatchTime(nameof(GetLookMemberFollowerCountDic)
                                           , () =>
                                             {
                                                 using var conn = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION);

                                                 var idDic = conn.Query<(long id, int followerCount)>(sql)
                                                                 .ToDictionary(x => x.id, x => x.followerCount);

                                                 return idDic;
                                             });

        return hashSet;
    }
}