using System.Text;
using JLookDataMigration.Extensions;
using JLookDataMigration.Helpers;
using Lctech.JLook.Core.Domain.Entities;
using MySql.Data.MySqlClient;

namespace JLookDataMigration;

public class MemberFollowerMigration
{
    private const string COPY_MEMBER_FOLLOWER_PREFIX = $"COPY \"{nameof(MemberFollower)}\" " +
                                                       $"(\"{nameof(MemberFollower.Id)}\",\"{nameof(MemberFollower.FollowerId)}\",\"{nameof(MemberFollower.IsFriend)}\"" 
                                                     + Setting.COPY_ENTITY_SUFFIX;

    private const string QUERY_MEMBER_FOLLOWER_SQL = $@"SELECT followuid AS Id, uid AS {nameof(MemberFollower.FollowerId)}, dateline
                                                        FROM pre_home_follow";

    private const string MEMBER_FOLLOWER_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(MemberFollower)}";

    public async Task MigrationAsync(CancellationToken cancellationToken)
    {
        FileHelper.RemoveFiles(new[] { MEMBER_FOLLOWER_PATH });

        var memberFollowerSb = new StringBuilder();

        await using var conn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);

        await conn.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(QUERY_MEMBER_FOLLOWER_SQL, conn);

        var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var memberId = reader.GetInt64(0);

            var followerId = reader.GetInt64(1);

            var dateLine = reader.GetInt64(2);

            var createDate = DateTimeOffset.FromUnixTimeSeconds(dateLine);

            memberFollowerSb.AppendValueLine(memberId, followerId, false,
                                             createDate, followerId, createDate, followerId, 0);
        }

        await reader.CloseAsync();

        FileHelper.WriteToFile(MEMBER_FOLLOWER_PATH, $"{nameof(MemberFollower)}.sql", COPY_MEMBER_FOLLOWER_PREFIX, memberFollowerSb);
    }
}