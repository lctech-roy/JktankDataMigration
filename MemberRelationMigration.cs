using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HashidsNet;
using JKTankDataMigration.Extensions;
using JKTankDataMigration.Helpers;
using Lctech.JKTank.Core.Domain.Entities;
using MySql.Data.MySqlClient;

namespace JKTankDataMigration;

public class MemberRelationMigration
{
    private static readonly Hashids Hashids = new("Our users are awesome", 8, "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");

    private static readonly HashSet<long> ProhibitMemberIdHash = MemberHelper.GetProhibitMemberIdHash();

    private static readonly HashSet<long> BlogCreatorMemberIdHash = MemberHelper.GetBlogCreatorMemberIdHash();

    private static readonly HashSet<long> LookMemberIdHash = LookMemberHelper.GetLookMemberIdHash();

    private const string COPY_MEMBER_RELATION_PREFIX = $"COPY \"{nameof(MemberRelation)}\" " +
                                                       $"(\"{nameof(MemberRelation.Id)}\",\"{nameof(MemberRelation.RelatedMemberId)}\",\"{nameof(MemberRelation.IsFollower)}\",\"{nameof(MemberRelation.IsFriend)}\""
                                                     + Setting.COPY_ENTITY_SUFFIX;

    private const string QUERY_MEMBER_RELATION_SQL = $@"SELECT followuid AS Id, uid AS {nameof(MemberRelation.RelatedMemberId)}, dateline
                                                        FROM pre_home_follow";

    private const string MEMBER_RELATION_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(MemberRelation)}";
    private const string MEMBER_RELATION_FRIEND_SEED_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(MemberRelation)}FriendSeed";

    public async Task MigrationAsync(CancellationToken cancellationToken)
    {
        FileHelper.RemoveFiles(new[] { MEMBER_RELATION_PATH });

        await using var conn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);

        await conn.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(QUERY_MEMBER_RELATION_SQL, conn);

        var reader = await command.ExecuteReaderAsync(cancellationToken);

        var memberRelationDic = new Dictionary<(long, long), MemberRelation?>();

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

            var memberRelation = new MemberRelation
                                 {
                                     Id = memberId,
                                     RelatedMemberId = followerId,
                                     CreationDate = createDate,
                                     CreatorId = followerId,
                                     ModificationDate = createDate,
                                     ModifierId = followerId,
                                     IsFollower = true
                                 };

            memberRelationDic.Add((memberId, followerId), memberRelation);
        }

        await reader.CloseAsync();

        if (!Directory.Exists(MEMBER_RELATION_FRIEND_SEED_PATH))
        {
            Directory.CreateDirectory(MEMBER_RELATION_FRIEND_SEED_PATH);
        }

        var jsonFilePaths = Directory.GetFiles(MEMBER_RELATION_FRIEND_SEED_PATH, "*.json").OrderBy(x => x).ToArray();

        if (jsonFilePaths.Length == 0)
            return;

        var dateNow = DateTimeOffset.UtcNow;

        foreach (var jsonFilePath in jsonFilePaths)
        {
            await using var stream = File.OpenRead(jsonFilePath);

            var friendRelations = await JsonSerializer.DeserializeAsync<IEnumerable<FriendRelation>>(stream, cancellationToken: cancellationToken);

            foreach (var friendRelation in friendRelations!)
            {
                var meUid = Convert.ToInt64(Hashids.Decode(friendRelation.MeHashUid)[0]);
                var youUid = Convert.ToInt64(Hashids.Decode(friendRelation.YouHashUid)[0]);

                if (!LookMemberIdHash.Contains(meUid) || !LookMemberIdHash.Contains(youUid))
                    continue;

                var meYou = memberRelationDic.GetValueOrDefault((meUid, youUid));

                if (meYou != null)
                    meYou.IsFriend = true;
                else
                {
                    var memberRelation = new MemberRelation
                                         {
                                             Id = meUid,
                                             RelatedMemberId = youUid,
                                             CreationDate = dateNow,
                                             CreatorId = youUid,
                                             ModificationDate = dateNow,
                                             ModifierId = youUid,
                                             IsFriend = true
                                         };

                    memberRelationDic.Add((meUid, youUid), memberRelation);
                }

                var youMe = memberRelationDic.GetValueOrDefault((youUid, meUid));

                if (youMe != null)
                    youMe.IsFriend = true;
                else
                {
                    var memberRelation = new MemberRelation
                                         {
                                             Id = youUid,
                                             RelatedMemberId = meUid,
                                             CreationDate = dateNow,
                                             CreatorId = meUid,
                                             ModificationDate = dateNow,
                                             ModifierId = meUid,
                                             IsFriend = true
                                         };

                    memberRelationDic.Add((youUid, meUid), memberRelation);
                }
            }
        }

        var memberRelationSb = new StringBuilder();

        foreach (var memberRelation in memberRelationDic.Select(keyValuePair => keyValuePair.Value!))
        {
            memberRelationSb.AppendValueLine(memberRelation.Id, memberRelation.RelatedMemberId, memberRelation.IsFollower, memberRelation.IsFriend,
                                             memberRelation.CreationDate, memberRelation.CreatorId, memberRelation.ModificationDate, memberRelation.ModifierId, 0);
        }

        FileHelper.WriteToFile(MEMBER_RELATION_PATH, $"{nameof(MemberRelation)}.sql", COPY_MEMBER_RELATION_PREFIX, memberRelationSb);
    }

    private class FriendRelation
    {
        [JsonPropertyName("me.huid")]
        public string MeHashUid { get; set; } = default!;

        [JsonPropertyName("you.huid")]
        public string YouHashUid { get; set; } = default!;
    }
}