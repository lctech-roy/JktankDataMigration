using Dapper;
using Npgsql;

namespace JLookDataMigration.Helpers;

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

                                                 var idDic = conn.Query<long>(sql).ToHashSet();

                                                 return idDic;
                                             });

        return hashSet;
    }
}