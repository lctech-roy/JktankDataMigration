using System.Text;
using JKTankDataMigration.Extensions;
using JKTankDataMigration.Helpers;
using Lctech.JKTank.Core.Domain.Entities;
using Lctech.JKTank.Core.Enums;
using MySql.Data.MySqlClient;

namespace JKTankDataMigration;

public class BlogReactMigration
{
    private static readonly HashSet<long> BlogIdHash = TankHelper.GetBlogIdHash();

    private const string COPY_BLOG_REACT_PREFIX = $"COPY \"{nameof(BlogReact)}\" " +
                                                  $"(\"{nameof(BlogReact.Id)}\",\"{nameof(BlogReact.Type)}\"" +
                                                  Setting.COPY_ENTITY_SUFFIX;

    private static readonly string QueryBlogReactSql = @"SELECT uid, id , clickid ,dateline FROM pre_home_clickuser 
                                                          WHERE idtype = 'blogid' ORDER BY dateline DESC";

    private const string BLOG_REACT_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(BlogReact)}";

    public async Task MigrationAsync(CancellationToken cancellationToken)
    {
        FileHelper.RemoveFiles(new[] { BLOG_REACT_PATH });

        var blogReactSb = new StringBuilder();

        await using var conn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);

        await conn.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(QueryBlogReactSql, conn);

        var reader = await command.ExecuteReaderAsync(cancellationToken);

        var distinctHash = new HashSet<(long, long)>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var memberId = reader.GetInt64(0);
            var id = reader.GetInt64(1);

            if (!BlogIdHash.Contains(id))
                continue;

            if (distinctHash.Contains((id, memberId)))
                continue;

            distinctHash.Add((id, memberId));

            var clickId = reader.GetInt32(2);
            var dateLine = reader.GetInt64(3);
            var createDate = DateTimeOffset.FromUnixTimeSeconds(dateLine);

            var type = clickId switch
                       {
                           1 => ReactType.ComeBy,
                           2 => ReactType.Amazing,
                           3 => ReactType.ShakeHands,
                           4 => ReactType.Flower,
                           5 => ReactType.Confuse,
                           _ => ReactType.None
                       };

            blogReactSb.AppendValueLine(id, (int)type,
                                        createDate, memberId, createDate, memberId, 0);
        }

        await reader.CloseAsync();

        FileHelper.WriteToFile(BLOG_REACT_PATH, $"{nameof(BlogReact)}.sql", COPY_BLOG_REACT_PREFIX, blogReactSb);
    }
}