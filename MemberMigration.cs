using System.Text;
using Dapper;
using JLookDataMigration.Extensions;
using JLookDataMigration.Helpers;
using JLookDataMigration.Models;
using Lctech.JLook.Core.Domain.Entities;
using Lctech.JLook.Core.Domain.Enums;
using MySql.Data.MySqlClient;
using Polly;

namespace JLookDataMigration;

public class MemberMigration
{
    private static readonly Dictionary<long, DateTimeOffset> MemberFirstPostDateDic = MemberHelper.GetMemberFirstPostDateDic();

    private const string COPY_MEMBER_PREFIX = $"COPY \"{nameof(Member)}\" " +
                                              $"(\"{nameof(Member.Id)}\",\"{nameof(Member.DisplayName)}\",\"{nameof(Member.NormalizedDisplayName)}\",\"{nameof(Member.RoleId)}\"" +
                                              $",\"{nameof(Member.ParentId)}\",\"{nameof(Member.PrivacyType)}\",\"{nameof(Member.Birthday)}\",\"{nameof(Member.Avatar)}\"" +
                                              $",\"{nameof(Member.Cover)}\",\"{nameof(Member.IsSensitiveCover)}\",\"{nameof(Member.PersonalProfile)}\",\"{nameof(Member.WarningCount)}\"" +
                                              $",\"{nameof(Member.WarningExpirationDate)}\",\"{nameof(Member.FirstPostDate)}\"" + Setting.COPY_ENTITY_SUFFIX;

    private const string COPY_MEMBER_PROFILE_PREFIX = $"COPY \"{nameof(MemberProfile)}\" " +
                                                      $"(\"{nameof(MemberProfile.Id)}\",\"{nameof(MemberProfile.PhoneNumber)}\",\"{nameof(MemberProfile.Email)}\",\"{nameof(MemberProfile.PhoneId)}\"" +
                                                      $",\"{nameof(MemberProfile.ObjectId)}\",\"{nameof(MemberProfile.RegisterIp)}\"" + Setting.COPY_ENTITY_SUFFIX;

    private const string QUERY_BLOG_MEMBER_UID = @"SELECT DISTINCT uid FROM pre_home_blog
                                                   UNION
                                                   SELECT DISTINCT authorid FROM pre_home_comment WHERE idtype = 'blogid'
                                                   UNION
                                                   SELECT DISTINCT uid FROM pre_home_follow
                                                   UNION
                                                   SELECT DISTINCT uid FROM pre_home_favorite
                                                   UNION
                                                   SELECT DISTINCT uid FROM pre_home_class";

    private const string QUERY_MEMBER = @"SELECT pum.uid AS Id, pum.username AS DisplayName,pcm.avatarstatus,pum.email,pum.regdate,pum.regip
                                            FROM pre_ucenter_members pum 
                                            LEFT JOIN pre_common_member pcm ON pcm.uid = pum.uid 
                                            LEFT JOIN pre_common_member_status pcms ON pcms.uid = pum.uid
                                            WHERE pum.uid IN @ids";

    public async Task MigrationAsync(CancellationToken cancellationToken)
    {
        long[] uids;

        await using (var cn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION))
        {
            uids = cn.Query<long>(QUERY_BLOG_MEMBER_UID).ToArray();
        }

        var offset = 0;
        const int limit = 50000;

        var uidGroups = new List<long[]>();

        while (offset < uids.Length)
        {
            var length = offset + limit;

            if (length > uids.Length)
                length = uids.Length;

            uidGroups.Add(uids[offset..length]);

            offset += limit;
        }

        await Parallel.ForEachAsync(uidGroups,
                                    CommonHelper.GetParallelOptions(cancellationToken), async (uidGroup, token) =>
                                                                                        {
                                                                                            await Policy

                                                                                                  // 1. 處理甚麼樣的例外
                                                                                                 .Handle<EndOfStreamException>()
                                                                                                 .Or<ArgumentOutOfRangeException>()

                                                                                                  // 2. 重試策略，包含重試次數
                                                                                                 .RetryAsync(5, (ex, retryCount) =>
                                                                                                                {
                                                                                                                    Console.WriteLine($"發生錯誤：{ex.Message}，第 {retryCount} 次重試");
                                                                                                                    Thread.Sleep(3000);
                                                                                                                })

                                                                                                  // 3. 執行內容
                                                                                                 .ExecuteAsync(async () =>
                                                                                                               {
                                                                                                                   await using var cn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);
                                                                                                                   var command = new CommandDefinition(QUERY_MEMBER, new { ids = uidGroup }, cancellationToken: token);
                                                                                                                   var members = (await cn.QueryAsync<OldMember>(command)).ToArray();

                                                                                                                   if (!members.Any())
                                                                                                                       return;

                                                                                                                   Execute(uidGroup, members);
                                                                                                               });
                                                                                        });
    }

    private void Execute(IReadOnlyList<long> uids, IEnumerable<OldMember> members)
    {
        var memberSb = new StringBuilder();
        var memberProfileSb = new StringBuilder();

        foreach (var oldMember in members)
        {
            var createDate = DateTimeOffset.FromUnixTimeSeconds(oldMember.RegDate);
            var memberId = oldMember.Id;

            var member = new Member
                         {
                             Id = memberId,
                             DisplayName = oldMember.DisplayName,
                             NormalizedDisplayName = oldMember.DisplayName.ToUpper(),
                             RoleId = 1,
                             ParentId = null,
                             PrivacyType = PrivacyType.Public,
                             Birthday = null,
                             Avatar = GetAvatar(memberId, oldMember.AvatarStatus),
                             Cover = null,
                             IsSensitiveCover = false,
                             PersonalProfile = null,
                             WarningCount = 0,
                             WarningExpirationDate = null,
                             FirstPostDate = MemberFirstPostDateDic.TryGetValue(memberId, out var value) ? value : null
                         };

            var memberProfile = new MemberProfile
                                {
                                    Id = memberId,
                                    PhoneNumber = null,
                                    Email = oldMember.Email,
                                    PhoneId = null,
                                    ObjectId = null,
                                    RegisterIp = oldMember.RegIp
                                };

            memberSb.AppendValueLine(member.Id, member.DisplayName.ToCopyText(), member.NormalizedDisplayName.ToCopyText(), member.RoleId,
                                     member.ParentId.ToCopyValue(), (int)member.PrivacyType, member.Birthday.ToCopyValue(), member.Avatar.ToCopyText(),
                                     member.Cover.ToCopyValue(), member.IsSensitiveCover, member.PersonalProfile.ToCopyValue(), member.WarningCount,
                                     member.WarningExpirationDate.ToCopyValue(), member.FirstPostDate.ToCopyValue(),
                                     createDate, 0, createDate, 0, 0);

            memberProfileSb.AppendValueLine(memberProfile.Id, memberProfile.PhoneNumber.ToCopyValue(), memberProfile.Email.ToCopyText(),
                                            memberProfile.PhoneId.ToCopyValue(), memberProfile.ObjectId.ToCopyValue(), memberProfile.RegisterIp.ToCopyValue(),
                                            createDate, 0, createDate, 0, 0);
        }

        FileHelper.WriteToFile($"{Setting.INSERT_DATA_PATH}/{nameof(Member)}", $"{uids[0]}-{uids[^1]}.sql", COPY_MEMBER_PREFIX, memberSb);
        FileHelper.WriteToFile($"{Setting.INSERT_DATA_PATH}/{nameof(MemberProfile)}", $"{uids[0]}-{uids[^1]}.sql", COPY_MEMBER_PROFILE_PREFIX, memberProfileSb);
    }

    private string? GetAvatar(long jkfId, bool? avatarStatus = null)
    {
        if (avatarStatus is false)
        {
            return null;
        }

        var uid = $"{jkfId:000000000}";
        var dir1 = uid.Substring(0, 3);
        var dir2 = uid.Substring(3, 2);
        var dir3 = uid.Substring(5, 2);
        var dir4 = uid.Substring(uid.Length - 2, 2);

        return $"https://www.mymyuc.net/data/avatar/{dir1}/{dir2}/{dir3}/{dir4}_avatar_big.jpg";
    }
}