using Dapper;
using JLookDataMigration.Models;
using MySql.Data.MySqlClient;
using Npgsql;

namespace JLookDataMigration;

public class BlogPinMigration
{
    private const string QUERY_BLOG_PIN_SQL = @"SELECT stickblogs FROM pre_common_member_field_home WHERE stickblogs != ''";

    public async Task MigrationAsync(CancellationToken cancellationToken)
    {
        await using var cn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);
        var command = new CommandDefinition(QUERY_BLOG_PIN_SQL, cancellationToken: cancellationToken);
        var pinBlogIds = (await cn.QueryAsync<string>(command)).ToArray();

        var maxPinBlogIds = pinBlogIds.Select(x =>
                          {
                              var blogIds = x.Split(',');

                              var maxBlogId = blogIds.Max(Convert.ToInt64);

                              return maxBlogId;
                          });
        
        await using var lookCn = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION);
        await lookCn.ExecuteAsync(@$"UPDATE ""Blog"" SET ""IsPinned"" = TRUE WHERE ""Id"" IN ({string.Join(',',maxPinBlogIds)})",cancellationToken);
    }
}