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
    
    ///<summary>
    ///封鎖帳號
    ///</summary>
    public static HashSet<long> GetProhibitMemberIdHash()
    {
        const string sql = @"SELECT m.""Id"" FROM ""MemberGroup"" mg 
        INNER JOIN ""Member"" m ON m.""Id""  = mg.""Id"" 
        WHERE ""GroupId"" = 202359554410496";

        var hashSet = CommonHelper.WatchTime(nameof(GetProhibitMemberIdHash)
                                           , () =>
                                             {
                                                 using var conn = new NpgsqlConnection(Setting.NEW_MEMBER_CONNECTION);

                                                 var idDic = conn.Query<long>(sql).ToHashSet();

                                                 return idDic;
                                             });

        return hashSet;
    }
}