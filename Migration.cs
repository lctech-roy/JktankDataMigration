using JLookDataMigration.Extensions;
using Lctech.Attachment.Core.Domain.Entities;
using Lctech.JLook.Core.Domain.Entities;
using Npgsql;

namespace JLookDataMigration;

public class Migration
{
    private const string SCHEMA_PATH = Setting.SCHEMA_PATH;
    private const string BEFORE_FILE_NAME = Setting.BEFORE_FILE_NAME;
    private const string AFTER_FILE_NAME = Setting.AFTER_FILE_NAME;

    public async Task ExecuteMemberAsync(CancellationToken token)
    {
        const string schemaPath = $"{SCHEMA_PATH}/{nameof(Member)}";
        const string memberPath = $"{Setting.INSERT_DATA_PATH}/{nameof(Member)}";
        const string memberProfilePath = $"{Setting.INSERT_DATA_PATH}/{nameof(MemberProfile)}";

        await using (var cn = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION))
            await cn.ExecuteCommandByPathAsync($"{schemaPath}/{BEFORE_FILE_NAME}", token);

        await using var cn1 = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION);
        cn1.ExecuteAllCopyFiles(memberPath);

        await using var cn2 = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION);
        cn2.ExecuteAllCopyFiles(memberProfilePath);

        await using (var cn = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION))
            await cn.ExecuteCommandByPathAsync($"{schemaPath}/{AFTER_FILE_NAME}", token);
    }

    public async Task ExecuteMemberBlogCategoryAsync()
    {
        await using var connection = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION);

        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(MemberBlogCategory)}/{BEFORE_FILE_NAME}");

        connection.ExecuteAllTexts($"{Setting.INSERT_DATA_PATH}/{nameof(MemberBlogCategory)}.sql");

        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(MemberBlogCategory)}/{AFTER_FILE_NAME}");
    }

    public async Task ExecuteBlogAsync()
    {
        const string blogPath = $"{Setting.INSERT_DATA_PATH}/{nameof(Blog)}";
        const string blogStatisticPath = $"{Setting.INSERT_DATA_PATH}/{nameof(BlogStatistic)}";
        const string blogMediaPath = $"{Setting.INSERT_DATA_PATH}/{nameof(BlogMedia)}";
        const string attachmentPath = $"{Setting.INSERT_DATA_PATH}/{nameof(Attachment)}";
        const string attachmentExtendDataPath = $"{Setting.INSERT_DATA_PATH}/{nameof(AttachmentExtendData)}";
        
        await using var connection = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION);
        connection.ExecuteAllTexts($"{Setting.INSERT_DATA_PATH}/{nameof(MassageBlog)}.sql");
        connection.ExecuteAllCopyFiles(blogPath);
        connection.ExecuteAllCopyFiles(blogStatisticPath);
        connection.ExecuteAllCopyFiles(blogMediaPath);
        
        connection.ExecuteAllTexts($"{Setting.INSERT_DATA_PATH}/{nameof(Hashtag)}.sql");
        
        await using var connection2 = new NpgsqlConnection(Setting.NEW_ATTACHMENT_CONNECTION);
        connection2.ExecuteAllCopyFiles(attachmentPath);
        connection2.ExecuteAllCopyFiles(attachmentExtendDataPath);
    }
    
    public async Task ExecuteBlogReactAsync()
    {
        const string blogReactPath = $"{Setting.INSERT_DATA_PATH}/{nameof(BlogReact)}";
        
        await using var connection = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION);
        connection.ExecuteAllCopyFiles(blogReactPath);
    }
    
    public async Task ExecuteCommentAsync()
    {
        const string commentPath = $"{Setting.INSERT_DATA_PATH}/{nameof(Comment)}";
        
        await using var connection = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION);
        connection.ExecuteAllCopyFiles(commentPath);
    }
    
    public async Task ExecuteMemberFavoriteAsync()
    {
        const string memberFavoritePath = $"{Setting.INSERT_DATA_PATH}/{nameof(MemberFavorite)}";
        
        await using var connection = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION);
        connection.ExecuteAllCopyFiles(memberFavoritePath);
    }
    
    public async Task ExecuteMemberFollowerAsync()
    {
        const string memberFollowerPath = $"{Setting.INSERT_DATA_PATH}/{nameof(MemberFollower)}";
        
        await using var connection = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION);
        connection.ExecuteAllCopyFiles(memberFollowerPath);
    }
}