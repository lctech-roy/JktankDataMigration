using System.Text;
using JLookDataMigration.Extensions;
using JLookDataMigration.Helpers;
using Lctech.JLook.Core.Domain.Entities;
using Lctech.JLook.Core.Domain.Enums;
using MySql.Data.MySqlClient;
using static System.Int32;

namespace JLookDataMigration;

public class CommentMigration
{
    private const string COPY_COMMENT_PREFIX = $"COPY \"{nameof(Comment)}\" " +
                                               $"(\"{nameof(Comment.Id)}\",\"{nameof(Comment.BlogId)}\",\"{nameof(Comment.ParentId)}\",\"{nameof(Comment.Type)}\"" +
                                               $",\"{nameof(Comment.DonateJPoints)}\",\"{nameof(Comment.Level)}\",\"{nameof(Comment.Content)}\",\"{nameof(Comment.ReplyCount)}\"" +
                                               $",\"{nameof(Comment.LikeCount)}\",\"{nameof(Comment.TotalLikeCount)}\",\"{nameof(Comment.SortingIndex)}\",\"{nameof(Comment.Hierarchy)}\"" +
                                               $",\"{nameof(Comment.Disabled)}\"" + Setting.COPY_ENTITY_SUFFIX;

    private const string QUERY_COMMENT_SQL = @"SELECT id AS BlogId, cid AS Id, message AS Content, status AS Disabled, dateline, authorid AS MemberId
                                               FROM pre_home_comment WHERE idtype = 'blogid'";

    private const string COMMENT_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(Comment)}";

    public async Task MigrationAsync(CancellationToken cancellationToken)
    {
        FileHelper.RemoveFiles(new[] { COMMENT_PATH });

        var commentSb = new StringBuilder();

        var blogIdHash = BlogHelper.GetBlogIdHash();

        await using var conn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);

        await conn.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(QUERY_COMMENT_SQL, conn);

        var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var blogId = reader.GetInt64(0);

            if (!blogIdHash.Contains(blogId))
                continue;
            
            var commentId = reader.GetInt64(1);
            var content = reader.GetString(2);
            var disabled = reader.GetBoolean(3);
            var dateLine = reader.GetInt64(4);
            var memberId = reader.GetInt64(5);
            
            var createDate = DateTimeOffset.FromUnixTimeSeconds(dateLine);
            
            content = RegexHelper.ImgSmileyRegex.Replace(content, innerMatch =>
                                                                  {
                                                                      TryParse(innerMatch.Groups[1].Value, out var emojiId);

                                                                      var emoji = EmojiHelper.EmojiDic.GetValueOrDefault(emojiId, string.Empty);

                                                                      return emoji;
                                                                  });

            var comment = new Comment
                          {
                              Id = commentId,
                              BlogId = blogId,
                              ParentId = null,
                              Type = CommentType.Comment,
                              DonateJPoints = 0,
                              Level = 1,
                              Content = content,
                              ReplyCount = 0,
                              LikeCount = 0,
                              TotalLikeCount = 0,
                              SortingIndex = 0,
                              Hierarchy = new[] { commentId },
                              Disabled = disabled,
                          };

            commentSb.AppendValueLine(comment.Id, comment.BlogId, comment.ParentId.ToCopyValue(),(int)comment.Type,
                                      comment.DonateJPoints, comment.Level, comment.Content.ToCopyText(),comment.ReplyCount,
                                      comment.LikeCount, comment.TotalLikeCount, comment.SortingIndex,comment.Hierarchy.ToCopyArray(),
                                      comment.Disabled, createDate, memberId, createDate, memberId, 0);
        }

        await reader.CloseAsync();

        FileHelper.WriteToFile(COMMENT_PATH, $"{nameof(Comment)}.sql", COPY_COMMENT_PREFIX, commentSb);
    }
}