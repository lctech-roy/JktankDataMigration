using System.Text;
using System.Web;
using Dapper;
using JKTankDataMigration.Extensions;
using JKTankDataMigration.Helpers;
using JKTankDataMigration.Models;
using Lctech.JKTank.Core.Domain.Entities;
using MySql.Data.MySqlClient;

namespace JKTankDataMigration;

public class MemberBlogCategoryMigration
{
    private const string MEMBER_BLOG_CATEGORY_SQL = $"COPY \"{nameof(MemberBlogCategory)}\" " +
                                                    $"(\"{nameof(MemberBlogCategory.Id)}\",\"{nameof(MemberBlogCategory.MemberId)}\",\"{nameof(MemberBlogCategory.Name)}\",\"{nameof(MemberBlogCategory.SortingIndex)}\""
                                                  + Setting.COPY_ENTITY_SUFFIX;

    private const string QUERY_BLOG_CLASS_SQL = $"SELECT classid as {nameof(MemberBlogCategory.Id)}, classname AS '{nameof(MemberBlogCategory.Name)}', uid as {nameof(MemberBlogCategory.MemberId)},dateline FROM pre_home_class";

    private static readonly HashSet<long> MemberIdHash = TankHelper.GetMemberIdHash();

    public void Migration()
    {
        OldMemberBlogCategory[] oldMemberBlogCategories;

        using (var cn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION))
        {
            oldMemberBlogCategories = cn.Query<OldMemberBlogCategory>(QUERY_BLOG_CLASS_SQL).ToArray();
        }

        var sb = new StringBuilder(MEMBER_BLOG_CATEGORY_SQL);
        var memberCategoryDic = new Dictionary<long, (int count, List<(string name, int count)> names)>();

        foreach (var memberBlogCategory in oldMemberBlogCategories)
        {
            var memberId = memberBlogCategory.MemberId;

            if (!MemberIdHash.Contains(memberId))
                continue;

            if (!memberCategoryDic.ContainsKey(memberBlogCategory.MemberId))
            {
                memberCategoryDic.Add(memberBlogCategory.MemberId, (1, new List<(string name, int count)> { (memberBlogCategory.Name, 1) }));
            }
            else
            {
                var nameCount = 1;

                while (memberCategoryDic[memberId].names.Contains((memberBlogCategory.Name, nameCount)))
                {
                    nameCount++;
                }

                memberCategoryDic[memberId].names.Add((memberBlogCategory.Name, nameCount));

                memberCategoryDic[memberId] = (memberCategoryDic[memberId].count + 1, memberCategoryDic[memberId].names);

                if (nameCount > 1)
                    memberBlogCategory.Name = memberBlogCategory.Name + "_" + nameCount;
            }

            var createDate = DateTimeOffset.FromUnixTimeSeconds(memberBlogCategory.Dateline);

            sb.AppendValueLine(memberBlogCategory.Id, memberBlogCategory.MemberId, HttpUtility.HtmlDecode(memberBlogCategory.Name).ToCopyText(), memberCategoryDic[memberBlogCategory.MemberId].count,
                               createDate, memberBlogCategory.MemberId, createDate, memberBlogCategory.MemberId, memberBlogCategory.Version);
        }

        File.WriteAllText($"{Setting.INSERT_DATA_PATH}/{nameof(MemberBlogCategory)}.sql", sb.ToString());
    }
}