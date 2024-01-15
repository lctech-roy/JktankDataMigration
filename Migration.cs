using JKTankDataMigration.Extensions;
using JKTankDataMigration.Models;
using Lctech.Attachment.Core.Domain.Entities;
using Lctech.JKTank.Core.Domain.Entities;
using Npgsql;

namespace JKTankDataMigration;

public class Migration
{
    private const string SCHEMA_PATH = Setting.SCHEMA_PATH;
    private const string BEFORE_FILE_NAME = Setting.BEFORE_FILE_NAME;
    private const string AFTER_FILE_NAME = Setting.AFTER_FILE_NAME;

    public async Task ExecuteMemberAsync()
    {
        const string memberSchemaPath = $"{SCHEMA_PATH}/{nameof(Member)}";
        const string memberPath = $"{Setting.INSERT_DATA_PATH}/{nameof(Member)}";
        const string memberIpAddressPath = $"{Setting.INSERT_DATA_PATH}/{nameof(MemberIpAddress)}";
        const string memberProfilePath = $"{Setting.INSERT_DATA_PATH}/{nameof(MemberProfile)}";

        const string userSchemaPath = $"{SCHEMA_PATH}/{nameof(User)}";
        const string userPath = $"{Setting.INSERT_DATA_PATH}/{nameof(User)}";
        const string userRolePath = $"{Setting.INSERT_DATA_PATH}/{nameof(UserRole)}";
        const string userExtendDataPath = $"{Setting.INSERT_DATA_PATH}/{nameof(UserExtendData)}";
        const string userExternalLoginPath = $"{Setting.INSERT_DATA_PATH}/{nameof(UserExternalLogin)}";

        var memberTask = new Task(() =>
                                  {
                                      using var cn = new NpgsqlConnection(Setting.TANK_CONNECTION);

                                      cn.ExecuteCommandByPath($"{memberSchemaPath}/{BEFORE_FILE_NAME}");
                                      cn.ExecuteAllCopyFiles(memberPath);
                                      cn.ExecuteAllCopyFiles(memberIpAddressPath);
                                      cn.ExecuteAllCopyFiles(memberProfilePath);
                                      cn.ExecuteCommandByPath($"{memberSchemaPath}/{AFTER_FILE_NAME}");
                                  });

        var userTask = new Task(() =>
                                {
                                    using var cn1 = new NpgsqlConnection(Setting.TANK_AUTH_CONNECTION);
                                    cn1.ExecuteCommandByPath($"{userSchemaPath}/{BEFORE_FILE_NAME}");
                                    cn1.ExecuteAllCopyFiles(userPath);
                                    cn1.ExecuteAllCopyFiles(userRolePath);
                                    cn1.ExecuteAllCopyFiles(userExtendDataPath);
                                    cn1.ExecuteAllCopyFiles(userExternalLoginPath);
                                    cn1.ExecuteCommandByPath($"{userSchemaPath}/{AFTER_FILE_NAME}");
                                });

        userTask.Start();
        memberTask.Start();

        await Task.WhenAll(memberTask, userTask);
    }

    public async Task ExecuteMemberBlogCategoryAsync()
    {
        await using var connection = new NpgsqlConnection(Setting.TANK_CONNECTION);

        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(MemberBlogCategory)}/{BEFORE_FILE_NAME}");
        connection.ExecuteAllTexts($"{Setting.INSERT_DATA_PATH}/{nameof(MemberBlogCategory)}.sql");
        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(MemberBlogCategory)}/{AFTER_FILE_NAME}");
    }

    public async Task ExecuteMassageBlogRegionAsync()
    {
        const string massageBlogRegionPath = $"{Setting.INSERT_DATA_PATH}/{nameof(MassageBlogRegion)}";

        await using var connection = new NpgsqlConnection(Setting.TANK_CONNECTION);
        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(MassageBlogRegion)}/{BEFORE_FILE_NAME}");
        connection.ExecuteAllCopyFiles(massageBlogRegionPath);
        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(MassageBlogRegion)}/{AFTER_FILE_NAME}");
    }

    public async Task ExecuteBlogAsync()
    {
        const string blogPath = $"{Setting.INSERT_DATA_PATH}/{nameof(Blog)}";
        const string blogStatisticPath = $"{Setting.INSERT_DATA_PATH}/{nameof(BlogStatistic)}";
        const string blogMediaPath = $"{Setting.INSERT_DATA_PATH}/{nameof(BlogMedia)}";
        const string attachmentPath = $"{Setting.INSERT_DATA_PATH}/{nameof(Attachment)}";
        const string attachmentExtendDataPath = $"{Setting.INSERT_DATA_PATH}/{nameof(AttachmentExtendData)}";

        await using var connection = new NpgsqlConnection(Setting.TANK_CONNECTION);
        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(Blog)}/{BEFORE_FILE_NAME}");

        connection.ExecuteAllTexts($"{Setting.INSERT_DATA_PATH}/{nameof(MassageBlog)}.sql");
        connection.ExecuteAllCopyFiles(blogPath);
        connection.ExecuteAllCopyFiles(blogStatisticPath);
        connection.ExecuteAllCopyFiles(blogMediaPath);
        connection.ExecuteAllTexts($"{Setting.INSERT_DATA_PATH}/{nameof(Hashtag)}.sql");

        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(Blog)}/{AFTER_FILE_NAME}");

        await using var connection2 = new NpgsqlConnection(Setting.TANK_ATTACHMENT_CONNECTION);

        connection2.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(Attachment)}/{BEFORE_FILE_NAME}");
        connection2.ExecuteAllCopyFiles(attachmentPath);
        connection2.ExecuteAllCopyFiles(attachmentExtendDataPath);
        connection2.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(Attachment)}/{AFTER_FILE_NAME}");
    }

    public async Task ExecuteHotTagAsync()
    {
        const string hotTagPath = $"{Setting.INSERT_DATA_PATH}/{nameof(HotTag)}";

        await using var connection = new NpgsqlConnection(Setting.TANK_CONNECTION);
        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(HotTag)}/{BEFORE_FILE_NAME}");

        connection.ExecuteAllTexts($"{hotTagPath}/{nameof(HotTag)}.sql");
        connection.ExecuteAllTexts($"{hotTagPath}/{nameof(Hashtag)}.sql");
        connection.ExecuteAllTexts($"{hotTagPath}/{nameof(HotTag)}{nameof(Hashtag)}.sql");
    }

    public async Task ExecuteBlogReactAsync()
    {
        const string blogReactPath = $"{Setting.INSERT_DATA_PATH}/{nameof(BlogReact)}";

        await using var connection = new NpgsqlConnection(Setting.TANK_CONNECTION);
        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(BlogReact)}/{BEFORE_FILE_NAME}");
        connection.ExecuteAllCopyFiles(blogReactPath);
        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(BlogReact)}/{AFTER_FILE_NAME}");
    }

    public async Task ExecuteCommentAsync()
    {
        const string commentPath = $"{Setting.INSERT_DATA_PATH}/{nameof(Comment)}";

        await using var connection = new NpgsqlConnection(Setting.TANK_CONNECTION);
        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(Comment)}/{BEFORE_FILE_NAME}");
        connection.ExecuteAllCopyFiles(commentPath);
        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(Comment)}/{AFTER_FILE_NAME}");
    }

    public async Task ExecuteMemberFavoriteAsync()
    {
        const string memberFavoritePath = $"{Setting.INSERT_DATA_PATH}/{nameof(MemberFavorite)}";


        await using var connection = new NpgsqlConnection(Setting.TANK_CONNECTION);
        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(MemberFavorite)}/{BEFORE_FILE_NAME}");
        connection.ExecuteAllCopyFiles(memberFavoritePath);
        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(MemberFavorite)}/{AFTER_FILE_NAME}");
    }

    public async Task ExecuteMemberRelationAsync()
    {
        const string memberRelationPath = $"{Setting.INSERT_DATA_PATH}/{nameof(MemberRelation)}";

        await using var connection = new NpgsqlConnection(Setting.TANK_CONNECTION);
        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(MemberRelation)}/{BEFORE_FILE_NAME}");
        connection.ExecuteAllCopyFiles(memberRelationPath);
        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(MemberRelation)}/{AFTER_FILE_NAME}");
    }

    public async Task ExecuteStatisticAsync()
    {
        const string memberStatisticPath = $"{Setting.INSERT_DATA_PATH}/{nameof(MemberStatistic)}";

        await using var connection = new NpgsqlConnection(Setting.TANK_CONNECTION);
        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(MemberStatistic)}/{BEFORE_FILE_NAME}");
        connection.ExecuteAllCopyFiles(memberStatisticPath);
        connection.ExecuteCommandByPath($"{SCHEMA_PATH}/{nameof(MemberStatistic)}/{AFTER_FILE_NAME}");
    }
}