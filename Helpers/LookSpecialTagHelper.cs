using Dapper;
using Npgsql;

namespace JKTankDataMigration.Helpers;

public class LookSpecialTagHelper
{
    ///<summary>
    ///在look blog內的所有Special Tag
    ///</summary>
    public static Dictionary<long,long[]> GetBlogSpecialTagsDic()
    {
        const string sql = @"SELECT bst.""Id"",bst.""SpecialTagId"" 
                             FROM ""BlogSpecialTag"" bst";

        var blogSpecialTagsDic = CommonHelper.WatchTime(nameof(GetBlogSpecialTagsDic)
                                                      , () =>
                                                        {
                                                            using var conn = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION);

                                                            var blogSpecialTagsDic = conn.Query<(long Id, long SpecialTagId)>(sql)
                                                                                         .GroupBy(x => x.Id)
                                                                                         .ToDictionary(x => x.Key,
                                                                                                       x => x.Select(y => y.SpecialTagId).ToArray());

                                                            return blogSpecialTagsDic;
                                                        });

        return blogSpecialTagsDic;
    }
}