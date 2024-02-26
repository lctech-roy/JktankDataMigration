using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using JKTankDataMigration.Extensions;
using JKTankDataMigration.Helpers;
using JKTankDataMigration.Models;
using Lctech.Attachment.Core.Domain.Entities;
using Lctech.JKTank.Core.Domain.Entities;
using Lctech.JKTank.Core.Enums;
using MySql.Data.MySqlClient;
using Polly;
using static System.Int32;

namespace JKTankDataMigration;

public class BlogMigration
{
    private static readonly HashSet<long> ProhibitMemberIdHash = MemberHelper.GetProhibitMemberIdHash();
    private static readonly HashSet<long> LifeStyleMemberHash = MemberHelper.GetLifeStyleMemberHash();
    private static readonly HashSet<long> MemberIdHash = TankHelper.GetMemberIdHash();
    private const string FORUM_IMAGE_URL = Setting.FORUM_DOMAIN_URL + "/attachment/";
    private const string FORUM_MASSAGE_URL = Setting.FORUM_DOMAIN_URL + "/board/1128/";

    private HashSet<long>? _massageArticleIdHash;

    private const int LIMIT = 20000;

    private const string COPY_BLOG_PREFIX = $"COPY \"{nameof(Blog)}\" " +
                                            $"(\"{nameof(Blog.Id)}\",\"{nameof(Blog.Subject)}\",\"{nameof(Blog.CategoryId)}\",\"{nameof(Blog.Status)}\"" +
                                            $",\"{nameof(Blog.VisibleType)}\",\"{nameof(Blog.Title)}\",\"{nameof(Blog.Content)}\",\"{nameof(Blog.Cover)}\"" +
                                            $",\"{nameof(Blog.IsPinned)}\",\"{nameof(Blog.Price)}\",\"{nameof(Blog.Conclusion)}\",\"{nameof(Blog.MediaCount)}\"" +
                                            $",\"{nameof(Blog.MassageBlogId)}\",\"{nameof(Blog.Hashtags)}\",\"{nameof(Blog.LastStatusModificationDate)}\",\"{nameof(Blog.Disabled)}\"" +
                                            $",\"{nameof(Blog.ServiceScore)}\",\"{nameof(Blog.AppearanceScore)}\",\"{nameof(Blog.ConversationScore)}\",\"{nameof(Blog.TidinessScore)}\",\"{nameof(Blog.AverageScore)}\",\"{nameof(Blog.LastEditDate)}\"" +
                                            Setting.COPY_ENTITY_SUFFIX;

    private const string COPY_BLOG_MEDIA_PREFIX = $"COPY \"{nameof(BlogMedia)}\" " +
                                                  $"(\"{nameof(BlogMedia.Id)}\",\"{nameof(BlogMedia.AttachmentId)}\",\"{nameof(BlogMedia.Type)}\",\"{nameof(BlogMedia.Status)}\"" +
                                                  $",\"{nameof(BlogMedia.IsCover)}\",\"{nameof(BlogMedia.SortingIndex)}\",\"{nameof(BlogMedia.Width)}\",\"{nameof(BlogMedia.Height)}\"" +
                                                  Setting.COPY_ENTITY_SUFFIX;

    private const string COPY_ATTACHMENT_PREFIX = $"COPY \"{nameof(Attachment)}\" " +
                                                  $"(\"{nameof(Attachment.Id)}\",\"{nameof(Attachment.Size)}\",\"{nameof(Attachment.ExternalLink)}\",\"{nameof(Attachment.Bucket)}\"" +
                                                  $",\"{nameof(Attachment.DownloadCount)}\",\"{nameof(Attachment.ProcessingState)}\",\"{nameof(Attachment.DeleteStatus)}\",\"{nameof(Attachment.IsPublic)}\"" +
                                                  $",\"{nameof(Attachment.StoragePath)}\",\"{nameof(Attachment.Name)}\",\"{nameof(Attachment.ContentType)}\",\"{nameof(Attachment.Extension)}\",\"{nameof(Attachment.ParentId)}\"" +
                                                  Setting.COPY_ENTITY_SUFFIX;

    private const string COPY_ATTACHMENT_EXTEND_DATA_PREFIX = $"COPY \"{nameof(AttachmentExtendData)}\" (\"{nameof(AttachmentExtendData.Id)}\",\"{nameof(AttachmentExtendData.Key)}\",\"{nameof(AttachmentExtendData.Value)}\"" + Setting.COPY_ENTITY_SUFFIX;

    private const string COPY_BLOG_STATISTIC_PREFIX = $"COPY \"{nameof(BlogStatistic)}\" " +
                                                      $"(\"{nameof(BlogStatistic.Id)}\",\"{nameof(BlogStatistic.HotScore)}\",\"{nameof(BlogStatistic.ViewCount)}\",\"{nameof(BlogStatistic.DonateCount)}\",\"{nameof(BlogStatistic.DonorCount)}\"" +
                                                      $",\"{nameof(BlogStatistic.PurchaseCount)}\",\"{nameof(BlogStatistic.ActualDonateJPoints)}\",\"{nameof(BlogStatistic.ActualPurchaseJPoints)}\",\"{nameof(BlogStatistic.ActualObtainTotalJPoints)}\"" +
                                                      $",\"{nameof(BlogStatistic.DonateJPoints)}\",\"{nameof(BlogStatistic.PurchaseJPoints)}\",\"{nameof(BlogStatistic.ObtainTotalJPoints)}\"" +
                                                      $",\"{nameof(BlogStatistic.FavoriteCount)}\",\"{nameof(BlogStatistic.CommentCount)}\",\"{nameof(BlogStatistic.ComeByReactCount)}\",\"{nameof(BlogStatistic.AmazingReactCount)}\"" +
                                                      $",\"{nameof(BlogStatistic.ShakeHandsReactCount)}\",\"{nameof(BlogStatistic.FlowerReactCount)}\",\"{nameof(BlogStatistic.ConfuseReactCount)}\",\"{nameof(BlogStatistic.TotalReactCount)}\"" +
                                                      Setting.COPY_ENTITY_SUFFIX;

    private const string COPY_HASH_TAG_PREFIX = $"COPY \"{nameof(Hashtag)}\" " +
                                                $"(\"{nameof(Hashtag.Id)}\",\"{nameof(Hashtag.Name)}\",\"{nameof(Hashtag.RelationBlogCount)}\"" +
                                                Setting.COPY_ENTITY_SUFFIX;

    private const string COPY_MASSAGE_BLOG_PREFIX = $"COPY \"{nameof(MassageBlog)}\" " +
                                                    $"(\"{nameof(MassageBlog.Id)}\",\"{nameof(MassageBlog.RegionId)}\",\"{nameof(MassageBlog.RelationBlogCount)}\",\"{nameof(MassageBlog.ExpirationDate)}\"" +
                                                    $",\"{nameof(MassageBlog.Title)}\",\"{nameof(MassageBlog.Description)}\",\"{nameof(MassageBlog.Image)}\",\"{nameof(MassageBlog.Url)}\"" +
                                                    Setting.COPY_ENTITY_SUFFIX;

    private static readonly string QueryBlogSql = $"""
                                                   SELECT b.blogid AS {nameof(OldBlog.Id)}, b.subject AS {nameof(OldBlog.Title)} ,b.classid AS {nameof(OldBlog.CategoryId)}, status AS {nameof(OldBlog.IsReview)},
                                                          friend AS {nameof(OldBlog.OldVisibleType)}, bf.message AS {nameof(OldBlog.OldContent)},
                                                          bf.tag as {nameof(OldBlog.OldTags)}, b.viewnum AS {nameof(OldBlog.ViewCount)}, b.favtimes AS {nameof(OldBlog.FavoriteCount)},
                                                          b.replynum AS {nameof(OldBlog.CommentCount)}, b.click1 AS {nameof(OldBlog.ComeByReactCount)}, b.click2 AS {nameof(OldBlog.AmazingReactCount)},
                                                          b.click3 AS {nameof(OldBlog.ShakeHandsReactCount)}, b.click4 AS {nameof(OldBlog.FlowerReactCount)}, b.click5 AS {nameof(OldBlog.ConfuseReactCount)},
                                                          b.uid AS {nameof(OldBlog.Uid)}, b.dateline AS {nameof(OldBlog.DateLine)}
                                                          FROM pre_home_blog b
                                                          LEFT JOIN pre_home_blogfield bf ON b.blogid = bf.blogid
                                                          {(Setting.TestBlogId.HasValue ? $"WHERE b.blogid = {Setting.TestBlogId}" : "")}
                                                          ORDER BY b.blogid
                                                          LIMIT {LIMIT} OFFSET @Offset
                                                   """;

    private static readonly ConcurrentDictionary<string, int> HashTagCountDic = new();
    private static readonly ConcurrentDictionary<long, int> MassageBlogCountDic = new();

    private const string BLOG_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(Blog)}";
    private const string BLOG_STATISTIC_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(BlogStatistic)}";
    private const string BLOG_MEDIA_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(BlogMedia)}";
    private const string ATTACHMENT_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(Attachment)}";
    private const string ATTACHMENT_EXTEND_DATA_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(AttachmentExtendData)}";
    private const string HASH_TAG_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(Hashtag)}.sql";
    private const string MASSAGE_BLOG_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(MassageBlog)}.sql";

    public async Task MigrationAsync(CancellationToken cancellationToken)
    {
        var sql = QueryBlogSql;

        _massageArticleIdHash = !Setting.TestBlogId.HasValue ? NewForumHelper.GetMassageArticleIdHash(null) : null;

        FileHelper.RemoveFiles(new[]
                               {
                                   BLOG_PATH, BLOG_STATISTIC_PATH, BLOG_MEDIA_PATH,
                                   ATTACHMENT_PATH, ATTACHMENT_EXTEND_DATA_PATH, HASH_TAG_PATH, MASSAGE_BLOG_PATH
                               });

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
                               var hasNextRow = true;
                               var offset = 0;

                               while (hasNextRow)
                               {
                                   await using var conn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);

                                   await conn.OpenAsync(cancellationToken);

                                   var oldBlogs = conn.Query<OldBlog>(QueryBlogSql, new { Offset = offset }).ToArray();

                                   if (oldBlogs.Length == 0)
                                   {
                                       hasNextRow = false;

                                       continue;
                                   }

                                   Execute(oldBlogs);

                                   offset += LIMIT;
                               }
                           });

        if (!HashTagCountDic.IsEmpty)
        {
            var dateNow = DateTimeOffset.UtcNow;
            var hashTagSb = new StringBuilder();
            var id = 1;

            foreach (var keyValuePair in HashTagCountDic)
            {
                hashTagSb.AppendValueLine(id++, keyValuePair.Key.ToCopyText(), keyValuePair.Value,
                                          dateNow, 0, dateNow, 0, 0);
            }

            FileHelper.WriteToFile($"{Setting.INSERT_DATA_PATH}", $"{nameof(Hashtag)}.sql", COPY_HASH_TAG_PREFIX, hashTagSb);
        }

        if (!MassageBlogCountDic.IsEmpty)
        {
            var massageBlogs = NewForumHelper.QueryBlogMassages(MassageBlogCountDic.Keys);
            var massageBlogSb = new StringBuilder();

            foreach (var massageBlog in massageBlogs)
            {
                massageBlog.Title = ToDecodeTitle(massageBlog.Title ?? string.Empty);
                massageBlog.RelationBlogCount = MassageBlogCountDic[massageBlog.Id];
                massageBlog.ModificationDate = massageBlog.CreationDate;
                massageBlog.ModifierId = massageBlog.CreatorId;
                massageBlog.Image = FORUM_IMAGE_URL + massageBlog.CoverId;
                massageBlog.Url = FORUM_MASSAGE_URL + massageBlog.Id;

                massageBlogSb.AppendValueLine(massageBlog.Id, massageBlog.RegionId.ToCopyValue(), massageBlog.RelationBlogCount, massageBlog.ExpirationDate.ToCopyValue(),
                                              massageBlog.Title.ToCopyText(), massageBlog.Description.ToCopyText(), massageBlog.Image.ToCopyText(), massageBlog.Url.ToCopyText(),
                                              massageBlog.CreationDate, massageBlog.CreatorId, massageBlog.ModificationDate, massageBlog.ModifierId, 0);
            }

            FileHelper.WriteToFile($"{Setting.INSERT_DATA_PATH}", $"{nameof(MassageBlog)}.sql", COPY_MASSAGE_BLOG_PREFIX, massageBlogSb);
        }
    }

    private void Execute(OldBlog[] oldBlogs)
    {
        var blogSb = new StringBuilder();
        var blogStatisticSb = new StringBuilder();
        var blogMediaSb = new StringBuilder();
        var attachmentSb = new StringBuilder();
        var attachmentExtentDataSb = new StringBuilder();

        foreach (var oldBlog in oldBlogs)
        {
            if (!MemberIdHash.Contains(oldBlog.Uid))
                continue;

            var createDate = DateTimeOffset.FromUnixTimeSeconds(oldBlog.DateLine);
            var memberId = oldBlog.Uid;
            var blogId = oldBlog.Id;
            var subject = LifeStyleMemberHash.Contains(memberId) ? BlogSubject.LifeStyle : BlogSubject.Massage;

            var content = oldBlog.OldContent ?? string.Empty;

            content = RegexHelper.ImgSmileyRegex.Replace(content, innerMatch =>
                                                                  {
                                                                      TryParse(innerMatch.Groups[1].Value, out var emojiId);

                                                                      var emoji = EmojiHelper.EmojiDic.GetValueOrDefault(emojiId, string.Empty);

                                                                      return emoji;
                                                                  });

            var matchCount = 0;
            long? coverId = null;

            content = RegexHelper.ImgSrcRegex.Replace(content, innerMatch =>
                                                               {
                                                                   var matchCollection = RegexHelper.ImgAttrRegex.Matches(innerMatch.Value);

                                                                   var path = string.Empty;
                                                                   int? height = null;
                                                                   int? width = null;

                                                                   foreach (Match match in matchCollection)
                                                                   {
                                                                       var matchPath = match.Groups[RegexHelper.PATH_GROUP].Value;

                                                                       if (!string.IsNullOrEmpty(matchPath))
                                                                       {
                                                                           path = matchPath;

                                                                           continue;
                                                                       }

                                                                       var matchHeight = match.Groups[RegexHelper.HEIGHT_GROUP].Value;

                                                                       if (!string.IsNullOrEmpty(matchHeight))
                                                                       {
                                                                           height = Convert.ToInt32(matchHeight);

                                                                           continue;
                                                                       }

                                                                       var matchWidth = match.Groups[RegexHelper.WIDTH_GROUP].Value;

                                                                       if (!string.IsNullOrEmpty(matchWidth))
                                                                       {
                                                                           width = Convert.ToInt32(matchWidth);
                                                                       }
                                                                   }

                                                                   if (string.IsNullOrEmpty(path) || path.Length > 2048)
                                                                       return string.Empty;

                                                                   var attachmentId = blogId * 1000L + ++matchCount;

                                                                   var isCover = matchCount == 1;

                                                                   if (isCover)
                                                                       coverId = attachmentId;

                                                                   var blogMedia = new BlogMedia
                                                                                   {
                                                                                       Id = blogId,
                                                                                       AttachmentId = attachmentId,
                                                                                       Type = MediaType.Image,
                                                                                       Status = MediaStatus.Normal,
                                                                                       Width = width,
                                                                                       Height = height,
                                                                                       IsCover = isCover,
                                                                                       SortingIndex = matchCount
                                                                                   };

                                                                   var attachment = new Attachment
                                                                                    {
                                                                                        Id = attachmentId,
                                                                                        ExternalLink = path,
                                                                                        IsPublic = true
                                                                                    };

                                                                   blogMediaSb.AppendValueLine(blogMedia.Id, blogMedia.AttachmentId, (int)blogMedia.Type, (int)blogMedia.Status,
                                                                                               blogMedia.IsCover, blogMedia.SortingIndex, blogMedia.Width.ToCopyValue(), blogMedia.Height.ToCopyValue(),
                                                                                               createDate, memberId, createDate, memberId, 0);

                                                                   attachmentSb.AppendValueLine(attachment.Id, attachment.Size.ToCopyValue(), attachment.ExternalLink, attachment.Bucket.ToCopyValue(),
                                                                                                attachment.DownloadCount, (int)attachment.ProcessingState, (int)attachment.DeleteStatus, attachment.IsPublic,
                                                                                                attachment.StoragePath.ToCopyValue(), attachment.Name.ToCopyText(), attachment.ContentType.ToCopyText(),
                                                                                                attachment.Extension.ToCopyText(), attachment.ParentId.ToCopyValue(),
                                                                                                createDate, memberId, createDate, memberId, 0);

                                                                   attachmentExtentDataSb.AppendValueLine(attachment.Id, Setting.BLOG_ID, blogId,
                                                                                                          createDate, memberId, createDate, memberId, 0);

                                                                   return string.Empty;
                                                               });

            content = RegexHelper.EmbedRegex.Replace(content, innerMatch =>
                                                              {
                                                                  var url = innerMatch.Groups[1].Value;

                                                                  return $"[url={url}]{url}[/url]";
                                                              });

            content = RegexHelper.FontSizeRegex.Replace(content, match =>
                                                                 {
                                                                     if (match.Groups[RegexHelper.SIZE_GROUP].Length == 0)
                                                                         return match.Value;

                                                                     TryParse(match.Groups[RegexHelper.SIZE_GROUP].Value, out var fontSize);

                                                                     var newFontSize = RegexHelper.FontSizeDic.GetValueOrDefault(fontSize, "1em");

                                                                     var fontSizeStr = $"font-size:{newFontSize};";

                                                                     if (!RegexHelper.StyleRegex.IsMatch(match.Value))
                                                                         return $"""{match.Groups[RegexHelper.START_GROUP].Value} style="{fontSizeStr}"{match.Groups[RegexHelper.END_GROUP].Value}""";

                                                                     var removeSizeStr = match.Groups[RegexHelper.START_GROUP].Value + match.Groups[RegexHelper.END_GROUP].Value;

                                                                     var finalStr = RegexHelper.StyleRegex.Replace(removeSizeStr, innerMatch =>
                                                                                                                                      $"""{innerMatch.Groups[RegexHelper.START_GROUP].Value}{fontSizeStr}{innerMatch.Groups[RegexHelper.STYLE_GROUP].Value}{innerMatch.Groups[RegexHelper.END_GROUP].Value}""");

                                                                     return finalStr;
                                                                 });

            content = RegexHelper.FontFaceRegex.Replace(content, innerMatch => innerMatch.Groups[1].Value + innerMatch.Groups[2].Value);

            var matchMassageIds = RegexHelper.MassageUrlRegex.Matches(content)
                                             .Select(x => x.Groups[RegexHelper.TID_GROUP].Value)
                                             .Where(x => !string.IsNullOrEmpty(x))
                                             .Distinct().ToArray();

            long? articleId = matchMassageIds.Length == 1 ? Convert.ToInt64(matchMassageIds.First()) : null;

            if (Setting.TestBlogId.HasValue && articleId.HasValue)
                _massageArticleIdHash = NewForumHelper.GetMassageArticleIdHash(articleId.Value);

            var massageArticleId = articleId.HasValue && _massageArticleIdHash != null && _massageArticleIdHash.Contains(articleId.Value)
                                       ? articleId
                                       : null;

            var blog = new Blog
                       {
                           Id = blogId,
                           Subject = subject,
                           CategoryId = oldBlog.CategoryId == 0 ? null : oldBlog.CategoryId,
                           Status = ProhibitMemberIdHash.Contains(memberId) ? new[] { BlogStatus.Block } :
                                    oldBlog.IsReview ? new[] { BlogStatus.PendingReview } :
                                    new[] { BlogStatus.Normal },
                           VisibleType = oldBlog.OldVisibleType switch
                                         {
                                             0 => VisibleType.Public,
                                             1 => VisibleType.Friend,
                                             _ => VisibleType.OnlyMe
                                         },
                           Title = ToDecodeTitle(oldBlog.Title),
                           IsPinned = false,
                           Content = content,
                           Cover = coverId,
                           Price = 0,
                           Conclusion = null,
                           MediaCount = matchCount,
                           MassageBlogId = massageArticleId,
                           Hashtags = GetTags(oldBlog.OldTags),
                           LastStatusModificationDate = null,
                           Disabled = false,
                           ServiceScore = 5,
                           AppearanceScore = 5,
                           ConversationScore = 5,
                           TidinessScore = 5,
                           AverageScore = 5
                       };

            if (blog is { Subject: BlogSubject.Massage, VisibleType: VisibleType.Public or VisibleType.Friend }
             && !blog.Status.Contains(BlogStatus.Block) && !blog.Status.Contains(BlogStatus.PendingReview))
            {
                var lowerHashTags = blog.Hashtags.Select(x => x.ToLower()).ToArray();

                foreach (var tag in lowerHashTags)
                {
                    if (HashTagCountDic.ContainsKey(tag))
                        HashTagCountDic[tag] += 1;
                    else
                        HashTagCountDic.TryAdd(tag, 1);
                }
            }
            else
            {
                var lowerHashTags = blog.Hashtags.Select(x => x.ToLower()).ToArray();

                foreach (var tag in lowerHashTags)
                {
                    if (!HashTagCountDic.ContainsKey(tag))
                        HashTagCountDic.TryAdd(tag, 0);
                }
            }

            if (massageArticleId.HasValue)
            {
                if (blog.VisibleType is VisibleType.Public or VisibleType.Friend &&
                    !blog.Status.Contains(BlogStatus.Block) &&
                    !blog.Status.Contains(BlogStatus.PendingReview) &&
                    blog.Subject != BlogSubject.LifeStyle)
                {
                    if (MassageBlogCountDic.ContainsKey(massageArticleId.Value))
                        MassageBlogCountDic[massageArticleId.Value] += 1;
                    else
                        MassageBlogCountDic.TryAdd(massageArticleId.Value, 1);
                }
                else
                {
                    if (!MassageBlogCountDic.ContainsKey(massageArticleId.Value))
                        MassageBlogCountDic.TryAdd(massageArticleId.Value, 0);
                }
            }

            var copyStatusArrayStr = $"{{{string.Join(",", blog.Status.Select(x => (int)x))}}}";

            blogSb.AppendValueLine(blog.Id, (int)blog.Subject, blog.CategoryId.ToCopyValue(), copyStatusArrayStr,
                                   (int)blog.VisibleType, blog.Title.ToCopyText(), blog.Content.ToCopyText(), blog.Cover.ToCopyValue(),
                                   blog.IsPinned, blog.Price, blog.Conclusion.ToCopyText(), blog.MediaCount, blog.MassageBlogId.ToCopyValue(),
                                   blog.Hashtags.ToCopyArray(), blog.LastStatusModificationDate.ToCopyValue(), blog.Disabled,
                                   blog.ServiceScore, blog.AppearanceScore, blog.ConversationScore, blog.TidinessScore, blog.AverageScore, createDate,
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
                                                    + oldBlog.FlowerReactCount + oldBlog.ConfuseReactCount
                                };

            blogStatistic.HotScore = (int)Math.Round(Convert.ToDecimal(blogStatistic.CommentCount * 0.1 + blogStatistic.TotalReactCount * 0.033), MidpointRounding.AwayFromZero);

            blogStatisticSb.AppendValueLine(blogStatistic.Id, blogStatistic.HotScore, blogStatistic.ViewCount, blogStatistic.DonateCount, blogStatistic.DonorCount,
                                            blogStatistic.PurchaseCount, blogStatistic.ActualDonateJPoints, blogStatistic.ActualPurchaseJPoints, blogStatistic.ActualObtainTotalJPoints,
                                            blogStatistic.DonateJPoints, blogStatistic.PurchaseJPoints, blogStatistic.ObtainTotalJPoints,
                                            blogStatistic.FavoriteCount, blogStatistic.CommentCount, blogStatistic.ComeByReactCount, blogStatistic.AmazingReactCount,
                                            blogStatistic.ShakeHandsReactCount, blogStatistic.FlowerReactCount, blogStatistic.ConfuseReactCount, blogStatistic.TotalReactCount,
                                            createDate, 0, createDate, 0, 0);
        }

        var fileName = $"{oldBlogs.First().Id}.sql";

        FileHelper.WriteToFile(BLOG_PATH, fileName, COPY_BLOG_PREFIX, blogSb);
        FileHelper.WriteToFile(BLOG_STATISTIC_PATH, fileName, COPY_BLOG_STATISTIC_PREFIX, blogStatisticSb);

        if (blogMediaSb.Length > 0)
            FileHelper.WriteToFile(BLOG_MEDIA_PATH, fileName, COPY_BLOG_MEDIA_PREFIX, blogMediaSb);

        if (attachmentSb.Length > 0)
            FileHelper.WriteToFile(ATTACHMENT_PATH, fileName, COPY_ATTACHMENT_PREFIX, attachmentSb);

        if (attachmentExtentDataSb.Length > 0)
            FileHelper.WriteToFile(ATTACHMENT_EXTEND_DATA_PATH, fileName, COPY_ATTACHMENT_EXTEND_DATA_PREFIX, attachmentExtentDataSb);
    }

    private static string[] GetTags(string? tagStr)
    {
        if (string.IsNullOrWhiteSpace(tagStr) || tagStr.Contains('{'))
            return Array.Empty<string>();

        var tags = new HashSet<string>();
        var starPoint = 0;

        for (var i = 0; i < tagStr.Length; i++)
        {
            if (tagStr[i] == ',')
                starPoint = i + 1;

            if (tagStr[i] != '\t') continue;

            var tag = tagStr.Substring(starPoint, i - starPoint);

            tag = RegexHelper.SymbolAndSpaceRegex.Replace(tag, "");

            if (string.IsNullOrWhiteSpace(tag))
                continue;

            tags.Add(tag);
        }

        return tags.ToArray();
    }

    private static string ToDecodeTitle(string title)
    {
        return !string.IsNullOrEmpty(title) ? WebUtility.HtmlDecode(title) : title;
    }
}