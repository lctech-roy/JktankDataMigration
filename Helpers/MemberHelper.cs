using Dapper;
using MySql.Data.MySqlClient;
using Npgsql;

namespace JLookDataMigration.Helpers;

public class MemberHelper
{
    public static Dictionary<long, DateTimeOffset> GetMemberFirstPostDateDic()
    {
        const string sql = @"SELECT uid,MIN(dateline) as dateline FROM pre_home_blog GROUP BY uid HAVING 'status' = 0";

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

        var memberHash = CommonHelper.WatchTime(nameof(GetMemberFirstPostDateDic)
                                              , () =>
                                                {
                                                    using var conn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);

                                                    var memberHash = conn.Query<long>(sql)
                                                                         .ToHashSet();

                                                    return memberHash;
                                                });

        return memberHash;
    }
}