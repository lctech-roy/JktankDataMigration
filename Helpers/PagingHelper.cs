using System.Runtime.CompilerServices;
using JLookDataMigration.Models;
using Microsoft.EntityFrameworkCore;

namespace JLookDataMigration.Helpers;

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

        var fSql = FormattableStringFactory.Create(sql);

        var ids = CommonHelper.WatchTime(nameof(GetPagingFirstIds)
                                       , () =>
                                         {
                                             var options = new DbContextOptionsBuilder<DbContext>().UseMySql(Setting.OLD_FORUM_CONNECTION, ServerVersion.AutoDetect(Setting.OLD_FORUM_CONNECTION)).Options;

                                             using var ctx = new DbContext(options);

                                             return ctx.Database.SqlQuery<long>(fSql).ToArray();
                                         });

        return ids;
    }
}