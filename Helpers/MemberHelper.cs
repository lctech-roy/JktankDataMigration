using Dapper;
using MySql.Data.MySqlClient;
using Npgsql;

namespace JKTankDataMigration.Helpers;

public class MemberHelper
{
    public static Dictionary<long, DateTimeOffset> GetMemberFirstPostDateDic()
    {
        const string sql = "SELECT uid,MIN(dateline) as dateline FROM pre_home_blog GROUP BY uid HAVING 'status' = 0";

        var dic = CommonHelper.WatchTime(nameof(GetMemberFirstPostDateDic)
                                       , () =>
                                         {
                                             using var conn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);

                                             var dic = conn.Query<(long memberId, long dateline)>(sql)
                                                             .ToDictionary(t => t.memberId, t =>  DateTimeOffset.FromUnixTimeSeconds(t.dateline));

                                             return dic;
                                         });

        return dic;
    }
    
    public static HashSet<long> GetLifeStyleMemberHash()
    {
        const string sql = @"SELECT * from pre_hidespace";

        var memberHash = CommonHelper.WatchTime(nameof(GetLifeStyleMemberHash)
                                              , () =>
                                                {
                                                    using var conn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);

                                                    var memberHash = conn.Query<long>(sql)
                                                                         .ToHashSet();

                                                    return memberHash;
                                                });

        return memberHash;
    }
    
    public static long[] GetNotInCenterMemberIds()
    {
        const string sql = @"SELECT pcm.uid FROM pre_common_member pcm
                             LEFT JOIN pre_ucenter_members pum ON pum.uid = pcm.uid
                             WHERE pum.uid is null";

        var memberIds = CommonHelper.WatchTime(nameof(GetNotInCenterMemberIds)
                                              , () =>
                                                {
                                                    using var conn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);

                                                    var memberIds = conn.Query<long>(sql)
                                                                         .ToArray();

                                                    return memberIds;
                                                });

        return memberIds;
    }
    

    
    ///<summary>
    ///封鎖帳號
    ///</summary>
    public static HashSet<long> GetProhibitMemberIdHash()
    {
        const string sql = @"SELECT uid FROM pre_common_member WHERE groupid = 5";

        var hashSet = CommonHelper.WatchTime(nameof(GetProhibitMemberIdHash)
                                           , () =>
                                             {
                                                 using var conn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);

                                                 var idDic = conn.Query<long>(sql).ToHashSet();

                                                 return idDic;
                                             });

        return hashSet;
    }
    
    ///<summary>
    ///有在blog發過文的帳號
    ///</summary>
    public static HashSet<long> GetBlogCreatorMemberIdHash()
    {
        const string sql = @"SELECT DISTINCT uid FROM pre_home_blog";

        var hashSet = CommonHelper.WatchTime(nameof(GetProhibitMemberIdHash)
                                           , () =>
                                             {
                                                 using var conn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);

                                                 var idDic = conn.Query<long>(sql).ToHashSet();

                                                 return idDic;
                                             });

        return hashSet;
    }
}