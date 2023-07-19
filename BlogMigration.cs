using System.Collections.Concurrent;
using System.Text;
using Dapper;
using JLookDataMigration.Extensions;
using JLookDataMigration.Helpers;
using JLookDataMigration.Models;
using Lctech.JLook.Core.Domain.Entities;
using Lctech.JLook.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Netcorext.Algorithms;
using Polly;

namespace JLookDataMigration;

public class BlogMigration
{
    private static readonly HashSet<long> LifeStyleMemberHash = MemberHelper.GetLifeStyleMemberHash();

    private const int LIMIT = 20000;

    private const string COPY_BLOG_PREFIX = $"COPY \"{nameof(Blog)}\" " +
                                            $"(\"{nameof(Blog.Id)}\",\"{nameof(Blog.Subject)}\",\"{nameof(Blog.CategoryId)}\",\"{nameof(Blog.Status)}\"" +
                                            $",\"{nameof(Blog.VisibleType)}\",\"{nameof(Blog.Title)}\",\"{nameof(Blog.Content)}\",\"{nameof(Blog.Cover)}\"" +
                                            $",\"{nameof(Blog.IsSensitiveCover)}\",\"{nameof(Blog.IsPinned)}\",\"{nameof(Blog.Price)}\",\"{nameof(Blog.Conclusion)}\"" +
                                            $",\"{nameof(Blog.MassageBlogId)}\",\"{nameof(Blog.Hashtags)}\",\"{nameof(Blog.LastStatusModificationDate)}\",\"{nameof(Blog.Disabled)}\"" +
                                            Setting.COPY_ENTITY_SUFFIX;

    private const string COPY_BLOG_MEDIA_PREFIX = $"COPY \"{nameof(BlogMedia)}\" " +
                                                  $"(\"{nameof(BlogMedia.Id)}\",\"{nameof(BlogMedia.AttachmentId)}\",\"{nameof(BlogMedia.Type)}\",\"{nameof(BlogMedia.Status)}\"" +
                                                  $",\"{nameof(BlogMedia.IsCover)}\",\"{nameof(BlogMedia.SortingIndex)}\",\"{nameof(BlogMedia.Width)}\",\"{nameof(BlogMedia.Height)}\"" +
                                                  Setting.COPY_ENTITY_SUFFIX;

    private const string COPY_BLOG_STATISTIC_PREFIX = $"COPY \"{nameof(BlogStatistic)}\" " +
                                                      $"(\"{nameof(BlogStatistic.Id)}\",\"{nameof(BlogStatistic.HotScore)}\",\"{nameof(BlogStatistic.ViewCount)}\",\"{nameof(BlogStatistic.DonateCount)}\",\"{nameof(BlogStatistic.DonorCount)}\"" +
                                                      $",\"{nameof(BlogStatistic.PurchaseCount)}\",\"{nameof(BlogStatistic.DonateJPoints)}\",\"{nameof(BlogStatistic.PurchaseJPoints)}\",\"{nameof(BlogStatistic.ObtainTotalJPoints)}\"" +
                                                      $",\"{nameof(BlogStatistic.FavoriteCount)}\",\"{nameof(BlogStatistic.CommentCount)}\",\"{nameof(BlogStatistic.ComeByReactCount)}\",\"{nameof(BlogStatistic.AmazingReactCount)}\"" +
                                                      $",\"{nameof(BlogStatistic.ShakeHandsReactCount)}\",\"{nameof(BlogStatistic.FlowerReactCount)}\",\"{nameof(BlogStatistic.ConfuseReactCount)}\",\"{nameof(BlogStatistic.TotalReactCount)}\"" +
                                                      $",\"{nameof(BlogStatistic.ServiceScore)}\",\"{nameof(BlogStatistic.AppearanceScore)}\",\"{nameof(BlogStatistic.ConversationScore)}\",\"{nameof(BlogStatistic.TidinessScore)}\",\"{nameof(BlogStatistic.AverageScore)}\"" +
                                                      Setting.COPY_ENTITY_SUFFIX;

    private const string COPY_HASH_TAG_PREFIX = $"COPY \"{nameof(Hashtag)}\" " +
                                                $"(\"{nameof(Hashtag.Id)}\",\"{nameof(Hashtag.Name)}\",\"{nameof(Hashtag.RelationBlogCount)}\"" +
                                                Setting.COPY_ENTITY_SUFFIX;

    private static readonly string QueryBlogSql = @$"SELECT 
                                            b.blogid AS {nameof(OldBlog.Id)}, b.subject AS {nameof(OldBlog.Title)} ,b.classid AS {nameof(OldBlog.CategoryId)}, status AS {nameof(OldBlog.IsReview)}, 
                                            friend AS {nameof(OldBlog.OldVisibleType)}, bf.message AS {nameof(OldBlog.OldContent)}, bf.pic AS {nameof(OldBlog.OldCover)}, 
                                            bf.tag as {nameof(OldBlog.OldTags)}, b.viewnum AS {nameof(OldBlog.ViewCount)}, b.favtimes AS {nameof(OldBlog.FavoriteCount)}, 
                                            b.replynum AS {nameof(OldBlog.CommentCount)}, b.click1 AS {nameof(OldBlog.ComeByReactCount)}, b.click2 AS {nameof(OldBlog.AmazingReactCount)}, 
                                            b.click3 AS {nameof(OldBlog.ShakeHandsReactCount)}, b.click4 AS {nameof(OldBlog.FlowerReactCount)}, b.click5 AS {nameof(OldBlog.ConfuseReactCount)},
                                            b.uid AS {nameof(OldBlog.Uid)}, b.dateline AS {nameof(OldBlog.DateLine)}
                                            FROM pre_home_blog b
                                            INNER JOIN pre_home_blogfield bf ON b.blogid = bf.blogid
                                            WHERE b.blogid >= @Id and b.blogid = 1538990
                                            LIMIT {LIMIT}";

    private static readonly ConcurrentDictionary<string, int> HashTagCountDic = new();

    private static readonly ISnowflake HashTagSnowflake = new SnowflakeJavaScriptSafeInteger(1);

    public async Task MigrationAsync(CancellationToken cancellationToken)
    {
        var firstIds = PagingHelper.GetPagingFirstIds("pre_home_blog", "blogid", LIMIT);

        await Parallel.ForEachAsync(firstIds,
                                    CommonHelper.GetParallelOptions(cancellationToken), async (id, token) =>
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
                                                                                                                   var options = new DbContextOptionsBuilder<DbContext>().UseMySql(Setting.OLD_FORUM_CONNECTION, ServerVersion.AutoDetect(Setting.OLD_FORUM_CONNECTION)).Options;

                                                                                                                   await using var ctx = new DbContext(options);

                                                                                                                   var command = new CommandDefinition(QueryBlogSql, new { id }, cancellationToken: token);
                                                                                                                   var oldBlogs = (await ctx.Database.GetDbConnection().QueryAsync<OldBlog>(command)).ToArray();

                                                                                                                   if (!oldBlogs.Any())
                                                                                                                       return;

                                                                                                                   Execute(oldBlogs);
                                                                                                               });
                                                                                        });

        var dateNow = DateTimeOffset.UtcNow;
        var hashTagSb = new StringBuilder();

        foreach (var keyValuePair in HashTagCountDic)
        {
            hashTagSb.AppendValueLine(HashTagSnowflake.Generate(), keyValuePair.Key.ToCopyText(), keyValuePair.Value,
                                      dateNow, 0, dateNow, 0, 0);
        }

        if (HashTagCountDic.Any())
            FileHelper.WriteToFile($"{Setting.INSERT_DATA_PATH}", $"{nameof(Hashtag)}.sql", COPY_HASH_TAG_PREFIX, hashTagSb);
    }

    private void Execute(OldBlog[] oldBlogs)
    {
        var blogSb = new StringBuilder();
        var blogStatisticSb = new StringBuilder();

        foreach (var oldBlog in oldBlogs)
        {
            var createDate = DateTimeOffset.FromUnixTimeSeconds(oldBlog.DateLine);
            var memberId = oldBlog.Uid;
            var blogId = oldBlog.Id;
            var subject = LifeStyleMemberHash.Contains(memberId) ? BlogSubject.LifeStyle : BlogSubject.Massage;

            var blog = new Blog
                       {
                           Id = blogId,
                           Subject = subject,
                           CategoryId = oldBlog.CategoryId == 0 ? null : oldBlog.CategoryId,
                           Status = oldBlog.IsReview ? new[] { BlogStatus.PendingReview } : new[] { BlogStatus.Normal },
                           VisibleType = oldBlog.OldVisibleType switch
                                         {
                                             0 => VisibleType.Public,
                                             1 => VisibleType.Friend,
                                             _ => VisibleType.OnlyMe
                                         },
                           Title = oldBlog.Title,
                           IsPinned = false,
                           Content = oldBlog.OldContent,
                           Cover = null,
                           IsSensitiveCover = false,
                           Price = 0,
                           Conclusion = null,
                           MassageBlogId = null,
                           Hashtags = oldBlog.OldTags.Split(' '),
                           LastStatusModificationDate = null,
                           Disabled = false
                       };

            var copyStatusArrayStr = $"{{{string.Join(",", blog.Status.Select(x => (int)x))}}}";

            foreach (var blogHashtag in blog.Hashtags)
            {
                if (HashTagCountDic.ContainsKey(blogHashtag))
                    HashTagCountDic[blogHashtag] += 1;
                else
                    HashTagCountDic.TryAdd(blogHashtag, 1);
            }


            blogSb.AppendValueLine(blog.Id, (int)blog.Subject, blog.CategoryId.ToCopyValue(), copyStatusArrayStr,
                                   (int)blog.VisibleType, blog.Title.ToCopyText(), blog.Content.ToCopyText(), blog.Cover.ToCopyValue(),
                                   blog.IsSensitiveCover, blog.IsPinned, blog.Price, blog.Conclusion.ToCopyText(),
                                   blog.MassageBlogId.ToCopyValue(), blog.Hashtags.ToCopyArray(), blog.LastStatusModificationDate.ToCopyValue(), blog.Disabled,
                                   createDate, memberId, createDate, memberId, 0);

            var blogStatistic = new BlogStatistic
                                {
                                    Id = blogId,
                                    ViewCount = oldBlog.ViewCount,
                                    FavoriteCount = oldBlog.FavoriteCount,
                                    CommentCount = oldBlog.CommentCount,
                                    ComeByReactCount = oldBlog.ComeByReactCount,
                                    AmazingReactCount = oldBlog.AmazingReactCount,
                                    ShakeHandsReactCount = oldBlog.ShakeHandsReactCount,
                                    FlowerReactCount = oldBlog.FlowerReactCount,
                                    ConfuseReactCount = oldBlog.ConfuseReactCount,
                                    TotalReactCount = oldBlog.ComeByReactCount + oldBlog.AmazingReactCount + oldBlog.ShakeHandsReactCount
                                                    + oldBlog.FlowerReactCount + oldBlog.ConfuseReactCount,
                                    ServiceScore = 5,
                                    AppearanceScore = 5,
                                    ConversationScore = 5,
                                    TidinessScore = 5,
                                    AverageScore = 5
                                };

            blogStatistic.HotScore = Convert.ToDecimal(blogStatistic.CommentCount * 0.1 + blogStatistic.TotalReactCount * 0.033);

            blogStatisticSb.AppendValueLine(blogStatistic.Id, blogStatistic.HotScore, blogStatistic.ViewCount, blogStatistic.DonateCount, blogStatistic.DonorCount,
                                            blogStatistic.PurchaseCount, blogStatistic.DonateJPoints, blogStatistic.PurchaseJPoints, blogStatistic.ObtainTotalJPoints,
                                            blogStatistic.FavoriteCount, blogStatistic.CommentCount, blogStatistic.ComeByReactCount, blogStatistic.AmazingReactCount,
                                            blogStatistic.ShakeHandsReactCount, blogStatistic.FlowerReactCount, blogStatistic.ConfuseReactCount, blogStatistic.TotalReactCount,
                                            blogStatistic.ServiceScore, blogStatistic.AppearanceScore, blogStatistic.ConversationScore, blogStatistic.TidinessScore, blogStatistic.AverageScore,
                                            createDate, 0, createDate, 0, 0);
        }

        FileHelper.WriteToFile($"{Setting.INSERT_DATA_PATH}/{nameof(Blog)}", $"{oldBlogs.First().Id}.sql", COPY_BLOG_PREFIX, blogSb);
        FileHelper.WriteToFile($"{Setting.INSERT_DATA_PATH}/{nameof(BlogStatistic)}", $"{oldBlogs.First().Id}.sql", COPY_BLOG_STATISTIC_PREFIX, blogStatisticSb);
    }
}