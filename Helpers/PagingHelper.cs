using System.Runtime.CompilerServices;
using Dapper;
using JKTankDataMigration.Models;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace JKTankDataMigration.Helpers;

public class PagingHelper
{
    public static long[] GetPagingFirstIds(string tableName, string idName, int limit, string whereCondition = "")
    {
        var sql = @$"SELECT {idName} 
                             FROM ( 
                                 SELECT 
                                     @row := @row +1 AS rownum, {idName}
                                 FROM ( SELECT @row :=0) r, {tableName} {whereCondition}
                                 ORDER BY {idName}
                                 ) ranked 
                             WHERE rownum % {limit} = 1";

        var ids = CommonHelper.WatchTime(nameof(GetPagingFirstIds)
                                       , () =>
                                         {
                                             using var conn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);

                                             conn.Open();

                                             return conn.Query<long>(sql, new { row = 0 }).ToArray();
                                         });

        return ids;
    }
}