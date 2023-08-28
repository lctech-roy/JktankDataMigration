using Dapper;
using JLookDataMigration.Helpers;
using Lctech.JLook.Core.Domain.Entities;
using Lctech.JLook.Core.Domain.Enums;
using Npgsql;
using System.Text;
using JLookDataMigration.Extensions;

namespace JLookDataMigration;

public class MemberStatisticMigration
{
    private static readonly IEnumerable<long> LookMemberIds = LookMemberHelper.GetLookMemberId();
    private static readonly Dictionary<long, int> LookMemberFollowerCountDic = LookMemberHelper.GetLookMemberFollowerCountDic();

    private const string COPY_MEMBER_STATISTIC_PREFIX = $"COPY \"{nameof(MemberStatistic)}\" " +
                                                        $"(\"{nameof(MemberStatistic.Id)}\",\"{nameof(MemberStatistic.HotScore)}\",\"{nameof(MemberStatistic.ViewCount)}\",\"{nameof(MemberStatistic.ObtainDonateCount)}\"" +
                                                        $",\"{nameof(MemberStatistic.ObtainPurchaseCount)}\",\"{nameof(MemberStatistic.ObtainDonateJPoints)}\",\"{nameof(MemberStatistic.ObtainPurchaseJPoints)}\",\"{nameof(MemberStatistic.ObtainTotalJPoints)}\"" +
                                                        $",\"{nameof(MemberStatistic.CommentCount)}\",\"{nameof(MemberStatistic.ReactCount)}\",\"{nameof(MemberStatistic.FavoriteCount)}\",\"{nameof(MemberStatistic.FollowerCount)}\"" +
                                                        $",\"{nameof(MemberStatistic.MassageBlogCount)}\",\"{nameof(MemberStatistic.PricingBlogCount)}\",\"{nameof(MemberStatistic.TotalBlogCount)}\"" +
                                                        Setting.COPY_ENTITY_SUFFIX;
    
    private const string MEMBER_STATISTIC_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(MemberStatistic)}";

    public async Task MigrationAsync(CancellationToken cancellationToken)
    {
        const string queryBlogStatisticSql = $@"SELECT b.""{nameof(Blog.Id)}"",b.""{nameof(Blog.Price)}"",b.""{nameof(Blog.MassageBlogId)}""
                                       ,b.""{nameof(Blog.VisibleType)}"",b.""{nameof(Blog.Disabled)}"",b.""{nameof(Blog.CreatorId)}""
                                       ,bs.""{nameof(BlogStatistic.Id)}"",bs.""{nameof(BlogStatistic.ViewCount)}"",bs.""{nameof(BlogStatistic.DonateCount)}"",bs.""{nameof(BlogStatistic.PurchaseCount)}""
                                       ,bs.""{nameof(BlogStatistic.DonateJPoints)}"",bs.""{nameof(BlogStatistic.PurchaseJPoints)}"",bs.""{nameof(BlogStatistic.ObtainTotalJPoints)}""
                                       ,bs.""{nameof(BlogStatistic.CommentCount)}"",bs.""{nameof(BlogStatistic.TotalReactCount)}"",bs.""{nameof(BlogStatistic.FavoriteCount)}""
                                       FROM ""Blog"" b 
                                       INNER JOIN ""BlogStatistic"" bs ON bs.""Id"" = b.""Id""";

        await using var conn = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION);

        var memberBlogDic = conn.Query<Blog, BlogStatistic, Blog>(queryBlogStatisticSql, (blog, blogStatistic) =>
                                                                                         {
                                                                                             blog.Statistic = blogStatistic;

                                                                                             return blog;
                                                                                         }, splitOn: "Id,Id")
                                .GroupBy(x => x.CreatorId).ToDictionary(x => x.Key, x => x.ToArray());

        var memberStatisticSb = new StringBuilder();
        var dateNow = DateTimeOffset.UtcNow;

        foreach (var lookMemberId in LookMemberIds)
        {
            var totalBlogStatistics = memberBlogDic.GetValueOrDefault(lookMemberId);
            var filterBlogStatistics = totalBlogStatistics?.Where(x => x.VisibleType != VisibleType.OnlyMe && !x.Disabled).ToArray();
            var followerCount = LookMemberFollowerCountDic.GetValueOrDefault(lookMemberId);
            
            if(totalBlogStatistics  == null  && followerCount == default)
                continue;
            
            var memberStatistic = new MemberStatistic
                                  {
                                      Id = lookMemberId,
                                      ViewCount = filterBlogStatistics?.Sum(x => x.Statistic.ViewCount) ?? 0,
                                      ObtainDonateCount = filterBlogStatistics?.Sum(x => x.Statistic.DonateCount) ?? 0,
                                      ObtainPurchaseCount = totalBlogStatistics?.Sum(x => x.Statistic.PurchaseCount) ?? 0,
                                      ObtainDonateJPoints = totalBlogStatistics?.Sum(x => x.Statistic.DonateJPoints) ?? 0,
                                      ObtainPurchaseJPoints = totalBlogStatistics?.Sum(x => x.Statistic.PurchaseJPoints) ?? 0,
                                      ObtainTotalJPoints = totalBlogStatistics?.Sum(x => x.Statistic.ObtainTotalJPoints) ?? 0,
                                      CommentCount = filterBlogStatistics?.Sum(x => x.Statistic.CommentCount) ?? 0,
                                      ReactCount = filterBlogStatistics?.Sum(x => x.Statistic.TotalReactCount) ?? 0,
                                      FavoriteCount = totalBlogStatistics?.Sum(x => x.Statistic.FavoriteCount) ?? 0,
                                      FollowerCount = LookMemberFollowerCountDic.GetValueOrDefault(lookMemberId),
                                      MassageBlogCount = filterBlogStatistics?.Count(x => x.MassageBlogId.HasValue) ?? 0,
                                      PricingBlogCount = filterBlogStatistics?.Count(x => x.Price > 0) ?? 0,
                                      TotalBlogCount = filterBlogStatistics?.Length ?? 0,
                                  };

            memberStatistic.HotScore = Convert.ToDecimal(memberStatistic.CommentCount * 0.1 + memberStatistic.ReactCount * 0.033);
            
            memberStatisticSb.AppendValueLine(memberStatistic.Id, memberStatistic.HotScore, memberStatistic.ViewCount, memberStatistic.ObtainDonateCount
                                            , memberStatistic.ObtainPurchaseCount, memberStatistic.ObtainDonateJPoints, memberStatistic.ObtainPurchaseJPoints, memberStatistic.ObtainTotalJPoints
                                            , memberStatistic.CommentCount, memberStatistic.ReactCount, memberStatistic.FavoriteCount, memberStatistic.FollowerCount
                                            , memberStatistic.MassageBlogCount, memberStatistic.PricingBlogCount, memberStatistic.TotalBlogCount
                                            , dateNow, 0, dateNow, 0, 0);
        }
        
        FileHelper.WriteToFile(MEMBER_STATISTIC_PATH, $"{nameof(MemberStatistic)}.sql", COPY_MEMBER_STATISTIC_PREFIX, memberStatisticSb);
    }
}