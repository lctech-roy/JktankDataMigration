using System.Text;
using JKTankDataMigration.Extensions;
using JKTankDataMigration.Helpers;
using Lctech.JKTank.Core.Domain.Entities;
using MySql.Data.MySqlClient;

namespace JKTankDataMigration;

public class MemberRelationMigration
{
    private static readonly HashSet<long> ProhibitMemberIdHash = MemberHelper.GetProhibitMemberIdHash();

    private static readonly HashSet<long> BlogCreatorMemberIdHash = MemberHelper.GetBlogCreatorMemberIdHash();

    private static readonly HashSet<long> LookMemberIdHash = LookMemberHelper.GetLookMemberIdHash();

    private const string COPY_MEMBER_RELATION_PREFIX = $"COPY \"{nameof(MemberRelation)}\" " +
                                                       $"(\"{nameof(MemberRelation.Id)}\",\"{nameof(MemberRelation.RelatedMemberId)}\",\"{nameof(MemberRelation.IsFollower)},\"{nameof(MemberRelation.IsFriend)}\""
                                                     + Setting.COPY_ENTITY_SUFFIX;

    private const string QUERY_MEMBER_RELATION_SQL = $@"SELECT followuid AS Id, uid AS {nameof(MemberRelation.RelatedMemberId)}, dateline
                                                        FROM pre_home_follow";

    private const string MEMBER_RELATION_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(MemberRelation)}";

    public async Task MigrationAsync(CancellationToken cancellationToken)
    {
        FileHelper.RemoveFiles(new[] { MEMBER_RELATION_PATH });

        var memberRelationSb = new StringBuilder();

        await using var conn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);

        await conn.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(QUERY_MEMBER_RELATION_SQL, conn);

        var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var memberId = reader.GetInt64(0);

            if (!BlogCreatorMemberIdHash.Contains(memberId) || !LookMemberIdHash.Contains(memberId))
                continue;

            var followerId = reader.GetInt64(1);

            if (!LookMemberIdHash.Contains(followerId))
                continue;

            if (ProhibitMemberIdHash.Contains(memberId) || ProhibitMemberIdHash.Contains(followerId))
                continue;

            var dateLine = reader.GetInt64(2);

            var createDate = DateTimeOffset.FromUnixTimeSeconds(dateLine);

            memberRelationSb.AppendValueLine(memberId, followerId, true, false,
                                             createDate, followerId, createDate, followerId, 0);
        }

        await reader.CloseAsync();

        FileHelper.WriteToFile(MEMBER_RELATION_PATH, $"{nameof(MemberRelation)}.sql", COPY_MEMBER_RELATION_PREFIX, memberRelationSb);
    }
}