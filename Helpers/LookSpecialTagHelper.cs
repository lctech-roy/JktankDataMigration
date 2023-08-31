using Dapper;
using Npgsql;

namespace JLookDataMigration.Helpers;

public class LookSpecialTagHelper
{
    ///<summary>
    ///在look blog內的所有Special Tag
    ///</summary>
    public static Dictionary<long,string[]> GetBlogSpecialTagsDic()
    {
        const string sql = @"SELECT bst.""BlogsId"",st.""Name"" 
                             FROM ""BlogSpecialTag"" bst
                             INNER JOIN ""SpecialTag"" st ON bst.""SpecialTagsId"" = st.""Id""";

        var blogSpecialTagsDic = CommonHelper.WatchTime(nameof(GetBlogSpecialTagsDic)
                                                      , () =>
                                                        {
                                                            using var conn = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION);

                                                            var blogSpecialTagsDic = conn.Query<(long Id, string Name)>(sql)
                                                                                         .GroupBy(x => x.Id)
                                                                                         .ToDictionary(x => x.Key,
                                                                                                       x => x.Select(y => y.Name).ToArray());

                                                            return blogSpecialTagsDic;
                                                        });

        return blogSpecialTagsDic;
    }
}