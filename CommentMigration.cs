using System.Text;
using JKTankDataMigration.Extensions;
using JKTankDataMigration.Helpers;
using Lctech.JKTank.Core.Domain.Entities;
using Lctech.JKTank.Core.Domain.Enums;
using MySql.Data.MySqlClient;
using static System.Int32;

namespace JKTankDataMigration;

public class CommentMigration
{
    private const string COPY_COMMENT_PREFIX = $"COPY \"{nameof(Comment)}\" " +
                                               $"(\"{nameof(Comment.Id)}\",\"{nameof(Comment.BlogId)}\",\"{nameof(Comment.ParentId)}\",\"{nameof(Comment.Type)}\"" +
                                               $",\"{nameof(Comment.DonateJPoints)}\",\"{nameof(Comment.Level)}\",\"{nameof(Comment.Content)}\",\"{nameof(Comment.ReplyCount)}\"" +
                                               $",\"{nameof(Comment.LikeCount)}\",\"{nameof(Comment.TotalLikeCount)}\",\"{nameof(Comment.SortingIndex)}\",\"{nameof(Comment.Hierarchy)}\"" +
                                               $",\"{nameof(Comment.Disabled)}\",\"{nameof(Comment.LastEditDate)}\"" + Setting.COPY_ENTITY_SUFFIX;

    private const string QUERY_COMMENT_SQL = @"SELECT id AS BlogId, cid AS Id, message AS Content, status AS Disabled, dateline, authorid AS MemberId, author AS Author
                                               FROM pre_home_comment WHERE idtype = 'blogid' ORDER BY blogid,dateline";

    //test Id => 9522,279968
    private const string COMMENT_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(Comment)}";

    public async Task MigrationAsync(CancellationToken cancellationToken)
    {
        FileHelper.RemoveFiles(new[] { COMMENT_PATH });

        var commentSb = new StringBuilder();

        var blogIdHash = LookHelper.GetLookBlogIdHash();
        var memberIdHash = LookMemberHelper.GetLookMemberIdHash();

        await using var conn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);

        await conn.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(QUERY_COMMENT_SQL, conn);

        var reader = await command.ExecuteReaderAsync(cancellationToken);

        var blogComments = new List<Comment>();
        var previousBlogId = 0L;

        var hasNextRow = await reader.ReadAsync(cancellationToken);

        var parentNotFoundCount = 0;
        var isLastRow = false;

        while (hasNextRow || isLastRow)
        {
            var blogId = reader.GetInt64(0);
            var commentId = reader.GetInt64(1);
            var content = reader.GetString(2);
            var disabled = reader.GetBoolean(3);
            var dateLine = reader.GetInt64(4);
            var memberId = reader.GetInt64(5);
            var authorName = reader.GetString(6);

            if (!blogIdHash.Contains(blogId) || !memberIdHash.Contains(memberId))
            {
                hasNextRow = await reader.ReadAsync(cancellationToken);
                isLastRow = false;

                continue;
            }

            var createDate = DateTimeOffset.FromUnixTimeSeconds(dateLine);

            var comment = new Comment
                          {
                              Id = commentId,
                              BlogId = blogId,
                              Type = CommentType.Comment,
                              Disabled = disabled,
                              LastEditDate = createDate,
                              Author = new Member { DisplayName = authorName.Trim() },
                              CreatorId = memberId,
                              ModifierId = memberId,
                              CreationDate = createDate,
                              ModificationDate = createDate
                          };
            
            var matches = RegexHelper.CommentReplyRegex.Matches(content);

            var matchReply = matches.FirstOrDefault();
            
            if (matchReply != null)
            {
                var author = matchReply.Groups[RegexHelper.AUTHOR_GROUP].Value.Trim();
                var authorContent = matchReply.Groups[RegexHelper.AUTHOR_CONTENT_GROUP].Value.Replace("\n", "\r\n").Trim();
                var replierContent = matchReply.Groups[RegexHelper.REPLIER_CONTENT_GROUP].Value.Trim();

                var parentComment = blogComments.FirstOrDefault(x => x.Author.DisplayName == author && x.Content == authorContent);

                if (parentComment == null)
                {
                    Console.WriteLine($"Count:{++parentNotFoundCount} CommentId:{commentId} ParentComment is null");

                    comment.Level = 1;
                    comment.Content = replierContent.Trim();
                    comment.Hierarchy = new[] { commentId };
                }
                else
                {
                    parentComment.ReplyCount++;

                    comment.ParentId = parentComment.Id;
                    comment.Level = parentComment.Level + 1 > 2 ? 2 : parentComment.Level + 1;
                    comment.Content = replierContent.Trim();
                    comment.Hierarchy = parentComment.Hierarchy.Append(commentId).ToArray();
                }
            }
            else
            {
                comment.Level = 1;
                comment.Content = content.Trim();
                comment.Hierarchy = new[] { commentId };
            }

            var blogIdChanged = blogId != previousBlogId;

            if ((blogIdChanged || !hasNextRow) && blogComments.Count > 0)
            {
                foreach (var blogComment in blogComments)
                {
                    blogComment.Content = ReplaceImageSmiley(blogComment.Content);
                    blogComment.Content = ReplaceImageSrcToUrl(blogComment.Content);

                    commentSb.AppendValueLine(blogComment.Id, blogComment.BlogId, blogComment.ParentId.ToCopyValue(), (int)blogComment.Type,
                                              blogComment.DonateJPoints, blogComment.Level, blogComment.Content.ToCopyText(), blogComment.ReplyCount,
                                              blogComment.LikeCount, blogComment.TotalLikeCount, blogComment.SortingIndex, blogComment.Hierarchy.ToCopyArray(),
                                              blogComment.Disabled,blogComment.LastEditDate.ToCopyValue(), blogComment.CreationDate, blogComment.CreatorId, blogComment.ModificationDate, blogComment.ModifierId, 0);
                }
            }

            if (blogIdChanged)
            {
                previousBlogId = blogId;
                blogComments = new List<Comment>();
            }

            blogComments.Add(comment);

            if (isLastRow)
                break;

            hasNextRow = await reader.ReadAsync(cancellationToken);

            if (!hasNextRow)
                isLastRow = true;
        }

        await reader.CloseAsync();

        FileHelper.WriteToFile(COMMENT_PATH, $"{nameof(Comment)}.sql", COPY_COMMENT_PREFIX, commentSb);

        return;

        string ReplaceImageSmiley(string content)
        {
            return RegexHelper.ImgSmileyRegex.Replace(content, innerMatch =>
                                                               {
                                                                   TryParse(innerMatch.Groups[1].Value, out var emojiId);

                                                                   var emoji = EmojiHelper.EmojiDic.GetValueOrDefault(emojiId, string.Empty);

                                                                   return emoji;
                                                               });
        }

        string ReplaceImageSrcToUrl(string content)
        {
            return RegexHelper.ImgSrcUrlPattern.Replace(content, innerMatch =>
                                                                 {
                                                                     var result = innerMatch.Groups[RegexHelper.URL_GROUP].Value;

                                                                     return result;
                                                                 });
        }
    }
}