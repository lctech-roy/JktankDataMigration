using Dapper;
using Npgsql;

namespace JKTankDataMigration.Helpers;

public class TankHelper
{
    ///<summary>
    ///在tank內的所有blogId
    ///</summary>
    public static HashSet<long> GetBlogIdHash()
    {
        const string sql = """
                           SELECT "Id" FROM "Blog"
                           """;

        var hashSet = CommonHelper.WatchTime(nameof(GetBlogIdHash)
                                           , () =>
                                             {
                                                 using var conn = new NpgsqlConnection(Setting.TANK_CONNECTION);

                                                 var idHash = conn.Query<long>(sql).ToHashSet();

                                                 return idHash;
                                             });

        return hashSet;
    }
    
     ///<summary>
    ///在tank內的所有member
    ///</summary>
    public static HashSet<long> GetMemberIdHash()
    {
        const string sql = """
                           SELECT "Id" FROM "Member"
                           """;

        var hashSet = CommonHelper.WatchTime(nameof(GetMemberIdHash)
                                           , () =>
                                             {
                                                 using var conn = new NpgsqlConnection(Setting.TANK_CONNECTION);

                                                 var idHash = conn.Query<long>(sql).ToHashSet();

                                                 return idHash;
                                             });

        return hashSet;
    }

    ///<summary>
    ///在tank內的所有member
    ///</summary>
    public static IEnumerable<long> GetMemberId()
    {
        const string sql = """
                           SELECT "Id" FROM "Member"
                           """;

        var ids = CommonHelper.WatchTime(nameof(GetMemberId)
                                       , () =>
                                         {
                                             using var conn = new NpgsqlConnection(Setting.TANK_CONNECTION);

                                             var ids = conn.Query<long>(sql);

                                             return ids;
                                         });

        return ids;
    }

    ///<summary>
    ///所有member的粉絲數
    ///</summary>
    public static Dictionary<long, int> GetMemberFollowerCountDic()
    {
        const string sql = """
                           SELECT "Id",COUNT("Id") AS FollowerCount
                           FROM "MemberRelation" 
                           WHERE "IsFollower" = true
                           GROUP BY "Id"
                           """;

        var hashSet = CommonHelper.WatchTime(nameof(GetMemberFollowerCountDic)
                                           , () =>
                                             {
                                                 using var conn = new NpgsqlConnection(Setting.TANK_CONNECTION);

                                                 var idDic = conn.Query<(long id, int followerCount)>(sql)
                                                                 .ToDictionary(x => x.id, x => x.followerCount);

                                                 return idDic;
                                             });

        return hashSet;
    }
    
    ///<summary>
    ///在tank blog內的所有Special Tag
    ///</summary>
    public static Dictionary<long,long[]> GetBlogSpecialTagsDic()
    {
        const string sql = """
                           SELECT bst."Id",bst."SpecialTagId" FROM "BlogSpecialTag" bst
                           """;

        var blogSpecialTagsDic = CommonHelper.WatchTime(nameof(GetBlogSpecialTagsDic)
                                                      , () =>
                                                        {
                                                            using var conn = new NpgsqlConnection(Setting.TANK_CONNECTION);

                                                            var blogSpecialTagsDic = conn.Query<(long Id, long SpecialTagId)>(sql)
                                                                                         .GroupBy(x => x.Id)
                                                                                         .ToDictionary(x => x.Key,
                                                                                                       x => x.Select(y => y.SpecialTagId).ToArray());

                                                            return blogSpecialTagsDic;
                                                        });

        return blogSpecialTagsDic;
    }
    
    ///<summary>
    ///取得所有member角色
    ///</summary>
    public static Dictionary<long,long[]> GetAuthUserRoleDic()
    {
        const string sql = """
                           SELECT "Id","RoleId" FROM "UserRole"
                           """;

        var userDic = CommonHelper.WatchTime(nameof(GetAuthUserRoleDic)
                                           , () =>
                                             {
                                                 using var conn = new NpgsqlConnection(Setting.TANK_AUTH_CONNECTION);

                                                 var userDic = conn.Query<(long Id, long RoleId)>(sql)
                                                                   .GroupBy(x => x.Id)
                                                                   .ToDictionary(x => x.Key,
                                                                                 x => x.Select(y => y.RoleId).ToArray());

                                                 return userDic;
                                             });

        return userDic;
    }
}