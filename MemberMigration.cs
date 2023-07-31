using System.Security.AccessControl;
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
    private const string USER_EXTEND_DATA_KEY = "SOURCE";
    private const string USER_EXTEND_DATA_VALUE = "LCTECH.JKF.MEMBER";
    private const string EXTERNAL_LOGIN_PROVIDER = "Pan";
    private const string PASSWORD = "37248859E0EA71CF30B6BC4ACCD1F113F8F02439E55A473EAA7BF0ABB4D6242B";
    
    private static readonly Dictionary<long, DateTimeOffset> MemberFirstPostDateDic = MemberHelper.GetMemberFirstPostDateDic();

    private const string COPY_MEMBER_PREFIX = $"COPY \"{nameof(Member)}\" " +
                                              $"(\"{nameof(Member.Id)}\",\"{nameof(Member.DisplayName)}\",\"{nameof(Member.NormalizedDisplayName)}\",\"{nameof(Member.RoleId)}\"" +
                                              $",\"{nameof(Member.ParentId)}\",\"{nameof(Member.PrivacyType)}\",\"{nameof(Member.Birthday)}\",\"{nameof(Member.Avatar)}\"" +
                                              $",\"{nameof(Member.Cover)}\",\"{nameof(Member.IsSensitiveCover)}\",\"{nameof(Member.PersonalProfile)}\",\"{nameof(Member.WarningCount)}\"" +
                                              $",\"{nameof(Member.WarningExpirationDate)}\",\"{nameof(Member.FirstPostDate)}\"" + Setting.COPY_ENTITY_SUFFIX;

    private const string COPY_MEMBER_PROFILE_PREFIX = $"COPY \"{nameof(MemberProfile)}\" " +
                                                      $"(\"{nameof(MemberProfile.Id)}\",\"{nameof(MemberProfile.PhoneNumber)}\",\"{nameof(MemberProfile.Email)}\",\"{nameof(MemberProfile.PhoneId)}\"" +
                                                      $",\"{nameof(MemberProfile.ObjectId)}\",\"{nameof(MemberProfile.RegisterIp)}\"" + Setting.COPY_ENTITY_SUFFIX;

    private const string COPY_USER_PREFIX = $"COPY \"{nameof(User)}\" " +
                                              $"(\"{nameof(User.Id)}\",\"{nameof(User.Username)}\",\"{nameof(User.NormalizedUsername)}\"" +
                                              $",\"{nameof(User.DisplayName)}\",\"{nameof(User.NormalizedDisplayName)}\",\"{nameof(User.Password)}\"" +
                                              $",\"{nameof(User.Email)}\",\"{nameof(User.NormalizedEmail)}\",\"{nameof(User.EmailConfirmed)}\",\"{nameof(User.PhoneNumber)}\"" +
                                              $",\"{nameof(User.PhoneNumberConfirmed)}\",\"{nameof(User.Otp)}\",\"{nameof(User.OtpBound)}\",\"{nameof(User.TwoFactorEnabled)}\"" +
                                              $",\"{nameof(User.RequiredChangePassword)}\",\"{nameof(User.AllowedRefreshToken)}\",\"{nameof(User.TokenExpireSeconds)}\"" +
                                              $",\"{nameof(User.RefreshTokenExpireSeconds)}\",\"{nameof(User.CodeExpireSeconds)}\",\"{nameof(User.AccessFailedCount)}\"" +
                                              $",\"{nameof(User.LastSignInDate)}\",\"{nameof(User.LastSignInIp)}\",\"{nameof(User.Disabled)}\"" +
                                              Setting.COPY_ENTITY_SUFFIX;
    
    private const string COPY_USER_EXTEND_DATA_PREFIX = $"COPY \"{nameof(UserExtendData)}\"" +
                                                        $"(\"{nameof(UserExtendData.Id)}\",\"{nameof(UserExtendData.Key)}\",\"{nameof(UserExtendData.Value)}\"" 
                                                      + Setting.COPY_ENTITY_SUFFIX;
    
    private const string COPY_USER_EXTERNAL_LOGIN_PREFIX = $"COPY \"{nameof(UserExternalLogin)}\"" +
                                                        $"(\"{nameof(UserExternalLogin.Id)}\",\"{nameof(UserExternalLogin.Provider)}\",\"{nameof(UserExternalLogin.UniqueId)}\"" 
                                                      + Setting.COPY_ENTITY_SUFFIX;

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
                                          WHERE pum.uid IN @ids";

    private const string QUERY_ADDITIONAL_MEMBER = @"SELECT pcm.uid AS Id, pcm.username AS DisplayName,pcm.avatarstatus,pcm.email,pcm.regdate,pcms.regip
                                                     FROM pre_common_member pcm 
                                                     LEFT JOIN pre_common_member_status pcms ON pcms.uid = pcm.uid
                                                     WHERE pcm.uid IN @ids";

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

        FileHelper.RemoveFiles(new[]
                               {
                                   $"{Setting.INSERT_DATA_PATH}/{nameof(Member)}",
                                   $"{Setting.INSERT_DATA_PATH}/{nameof(MemberProfile)}",
                                   $"{Setting.INSERT_DATA_PATH}/{nameof(User)}",
                                   $"{Setting.INSERT_DATA_PATH}/{nameof(UserExtendData)}",
                                   $"{Setting.INSERT_DATA_PATH}/{nameof(UserExternalLogin)}"
                               });
        
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

        var notInCenterMemberIds = MemberHelper.GetNotInCenterMemberIds();
        var uidHash = uids.ToHashSet();

        var additionMemberIds = notInCenterMemberIds.Where(uidHash.Contains).ToArray();

        await using var cnn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);
        var command = new CommandDefinition(QUERY_ADDITIONAL_MEMBER, new { ids = additionMemberIds }, cancellationToken: cancellationToken);
        var members = (await cnn.QueryAsync<OldMember>(command)).ToArray();

        if (!members.Any())
            return;

        Execute(members.Select(x => x.Id).ToArray(), members);
    }

    private void Execute(IReadOnlyList<long> uids, IEnumerable<OldMember> members)
    {
        var memberSb = new StringBuilder();
        var memberProfileSb = new StringBuilder();
        var userSb = new StringBuilder();
        var userExtendDataSb = new StringBuilder();
        var userExternalLoginSb = new StringBuilder();
        
        foreach (var oldMember in members)
        {
            var createDate = DateTimeOffset.FromUnixTimeSeconds(oldMember.RegDate);
            var memberId = oldMember.Id;

            var member = new Member
                         {
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
                                    PhoneNumber = null,
                                    Email = oldMember.Email,
                                    PhoneId = null,
                                    ObjectId = null,
                                    RegisterIp = oldMember.RegIp
                                };

            var userName = oldMember.DisplayName + "-" + memberId;
            
            var user = new User
                       {
                           Username = userName,
                           NormalizedUsername = userName.ToUpper(),
                           DisplayName = oldMember.DisplayName,
                           NormalizedDisplayName = oldMember.DisplayName.ToUpper(),
                           Password = PASSWORD,
                           Email = oldMember.Email,
                           NormalizedEmail = oldMember.Email.ToUpper(),
                           EmailConfirmed = false,
                           PhoneNumber = null,
                           PhoneNumberConfirmed = false,
                           Otp = null,
                           OtpBound = false,
                           TwoFactorEnabled = false,
                           RequiredChangePassword = false,
                           AllowedRefreshToken = true,
                           TokenExpireSeconds = 3600,
                           RefreshTokenExpireSeconds = 21600,
                           CodeExpireSeconds = 600,
                           AccessFailedCount = 0,
                           LastSignInDate = null,
                           LastSignInIp = null,
                           Disabled = false
                       };

            memberSb.AppendValueLine(memberId, member.DisplayName.ToCopyText(), member.NormalizedDisplayName.ToCopyText(), member.RoleId,
                                     member.ParentId.ToCopyValue(), (int)member.PrivacyType, member.Birthday.ToCopyValue(), member.Avatar.ToCopyText(),
                                     member.Cover.ToCopyValue(), member.IsSensitiveCover, member.PersonalProfile.ToCopyValue(), member.WarningCount,
                                     member.WarningExpirationDate.ToCopyValue(), member.FirstPostDate.ToCopyValue(),
                                     createDate, 0, createDate, 0, 0);

            memberProfileSb.AppendValueLine(memberId, memberProfile.PhoneNumber.ToCopyValue(), memberProfile.Email.ToCopyText(),
                                            memberProfile.PhoneId.ToCopyValue(), memberProfile.ObjectId.ToCopyValue(), memberProfile.RegisterIp.ToCopyValue(),
                                            createDate, 0, createDate, 0, 0);

            userSb.AppendValueLine(memberId,user.Username.ToCopyText(),user.NormalizedUsername.ToCopyText(),
                                   user.DisplayName.ToCopyText(),user.NormalizedDisplayName.ToCopyText(),user.Password,
                                   user.Email.ToCopyText(),user.NormalizedEmail.ToCopyText(),user.EmailConfirmed,user.PhoneNumber.ToCopyText(),
                                   user.PhoneNumberConfirmed,user.Otp.ToCopyText(), user.OtpBound, user.TwoFactorEnabled,
                                   user.RequiredChangePassword,user.AllowedRefreshToken,user.TokenExpireSeconds,
                                   user.RefreshTokenExpireSeconds,user.CodeExpireSeconds,user.AccessFailedCount,
                                   user.LastSignInDate.ToCopyValue(),user.LastSignInIp.ToCopyText(),user.Disabled,
                                   createDate, 0, createDate, 0, 0);
            
            userExtendDataSb.AppendValueLine(memberId, USER_EXTEND_DATA_KEY, USER_EXTEND_DATA_VALUE,
                                                   createDate, 0, createDate, 0, 0);
            
            userExternalLoginSb.AppendValueLine(memberId,EXTERNAL_LOGIN_PROVIDER,memberId.ToString(),
                                                createDate, 0, createDate, 0, 0);
        }

        FileHelper.WriteToFile($"{Setting.INSERT_DATA_PATH}/{nameof(Member)}", $"{uids[0]}-{uids[^1]}.sql", COPY_MEMBER_PREFIX, memberSb);
        FileHelper.WriteToFile($"{Setting.INSERT_DATA_PATH}/{nameof(MemberProfile)}", $"{uids[0]}-{uids[^1]}.sql", COPY_MEMBER_PROFILE_PREFIX, memberProfileSb);
        FileHelper.WriteToFile($"{Setting.INSERT_DATA_PATH}/{nameof(User)}", $"{uids[0]}-{uids[^1]}.sql", COPY_USER_PREFIX, userSb);
        FileHelper.WriteToFile($"{Setting.INSERT_DATA_PATH}/{nameof(UserExtendData)}", $"{uids[0]}-{uids[^1]}.sql", COPY_USER_EXTEND_DATA_PREFIX, userExtendDataSb);
        FileHelper.WriteToFile($"{Setting.INSERT_DATA_PATH}/{nameof(UserExternalLogin)}", $"{uids[0]}-{uids[^1]}.sql", COPY_USER_EXTERNAL_LOGIN_PREFIX, userExternalLoginSb);
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