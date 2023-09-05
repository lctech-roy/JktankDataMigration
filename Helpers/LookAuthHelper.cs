using Dapper;
using Npgsql;

namespace JKTankDataMigration.Helpers;

public static class LookAuthHelper
{
    ///<summary>
    ///在look auth內的所有member角色
    ///</summary>
    public static Dictionary<long,long[]> GetLookAuthUserRoleDic()
    {
        const string sql = @"SELECT ""Id"",""RoleId"" FROM ""UserRole""";

        var userDic = CommonHelper.WatchTime(nameof(GetLookAuthUserRoleDic)
                                           , () =>
                                             {
                                                 using var conn = new NpgsqlConnection(Setting.NEW_AUTH_CONNECTION);

                                                 var userDic = conn.Query<(long Id, long RoleId)>(sql)
                                                                   .GroupBy(x => x.Id)
                                                                   .ToDictionary(x => x.Key,
                                                                                 x => x.Select(y => y.RoleId).ToArray());

                                                 return userDic;
                                             });

        return userDic;
    }
}