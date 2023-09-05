using System.Text;
using JKTankDataMigration.Extensions;
using JKTankDataMigration.Helpers;
using Lctech.JKTank.Core.Domain.Entities;
using MySql.Data.MySqlClient;

namespace JKTankDataMigration;

public class MemberFavoriteMigration
{
    private const string COPY_MEMBER_FAVORITE_PREFIX = $"COPY \"{nameof(MemberFavorite)}\" " +
                                                       $"(\"{nameof(MemberFavorite.Id)}\",\"{nameof(MemberFavorite.BlogId)}\"" + Setting.COPY_ENTITY_SUFFIX;

    private const string QUERY_MEMBER_FAVORITE_SQL = $@"SELECT uid AS Id, id AS {nameof(MemberFavorite.BlogId)}, dateline
                                          FROM pre_home_favorite
                                          WHERE idtype = 'blogid'";

    private const string MEMBER_FAVORITE_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(MemberFavorite)}";

    public async Task MigrationAsync(CancellationToken cancellationToken)
    {
        FileHelper.RemoveFiles(new[] { MEMBER_FAVORITE_PATH });

        var memberFavoriteSb = new StringBuilder();

        var blogIdHash = BlogHelper.GetBlogIdHash();

        await using var conn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);

        await conn.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(QUERY_MEMBER_FAVORITE_SQL, conn);

        var reader = await command.ExecuteReaderAsync(cancellationToken);

        var distinctHash = new HashSet<(long, long)>();
        
        while (await reader.ReadAsync(cancellationToken))
        {
            var blogId = reader.GetInt64(1);

            if (!blogIdHash.Contains(blogId))
                continue;

            var memberId = reader.GetInt64(0);
            
            if(distinctHash.Contains((memberId,blogId)))
                continue;

            distinctHash.Add((memberId, blogId));
            
            var dateLine = reader.GetInt64(2);

            var createDate = DateTimeOffset.FromUnixTimeSeconds(dateLine);

            memberFavoriteSb.AppendValueLine(memberId, blogId,
                                             createDate, memberId, createDate, memberId, 0);
        }

        await reader.CloseAsync();

        FileHelper.WriteToFile(MEMBER_FAVORITE_PATH, $"{nameof(MemberFavorite)}.sql", COPY_MEMBER_FAVORITE_PREFIX, memberFavoriteSb);
    }
}