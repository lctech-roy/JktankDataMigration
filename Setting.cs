using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace JKTankDataMigration;

public static class Setting
{
    // v2 localhost
    // public const string NEW_FORUM_CONNECTION_LOCAL = "Host=127.0.0.1;Port=5432;Username=postgres;Password=P@ssw0rd;Database=lctech_jkf_forum_tttt;Timeout=1024;CommandTimeout=1800;Maximum Pool Size=80;";

    // v1 dev
    // public const string OLD_FORUM_CONNECTION = "Host=35.194.153.253;Port=3306;Username=testuser;Password=b5GbvjRKXhrXcuW;Database=newjk;Pooling=True;maximumpoolsize=80;default command timeout=300;TreatTinyAsBoolean=false;sslmode=none;";

    // v1 stage
    public const string OLD_FORUM_CONNECTION = "Host=34.80.4.149;Port=3306;Username=migrationUser;Password=A|5~9R}Olfs}@)/M;Database=newjk;Pooling=True;maximumpoolsize=80;default command timeout=300;TreatTinyAsBoolean=false;sslmode=none;";

    //v2 dev
    private const string HOST = "35.189.163.100";
    private const string USER_NAME = "postgres";
    private const string PASSWORD = "+^s_yRQc|JuYN4hr";
    private const string PORT = "30201";

    //v2 stage
    // private const string HOST = "34.81.88.250";
    // private const string USER_NAME = "jkforum";
    // private const string PASSWORD = "5vvJumLnhFnu";
    //private const string PORT = "5432";
    
    public const string NEW_LOOK_CONNECTION = $"Host={HOST};Port={PORT};Username={USER_NAME};Password={PASSWORD};Database=lctech_jktank;Timeout=1024;CommandTimeout=1800;Maximum Pool Size=80;SSL Mode=Require;Trust Server Certificate=true;Include Error Detail=true";
    public const string NEW_ATTACHMENT_CONNECTION = $"Host={HOST};Port={PORT};Username={USER_NAME};Password={PASSWORD};Database=lctech_attachment;Timeout=1024;CommandTimeout=1800;Maximum Pool Size=80;SSL Mode=Require;Trust Server Certificate=true;Include Error Detail=true";
    public const string NEW_AUTH_CONNECTION = $"Host={HOST};Port={PORT};Username={USER_NAME};Password={PASSWORD};Database=lctech_auth;Timeout=1024;CommandTimeout=1800;Maximum Pool Size=80;SSL Mode=Require;Trust Server Certificate=true;Include Error Detail=true";

    //查詢new forum 1128
    private const string F_HOST = "35.189.163.100";
    private const string F_USER_NAME = "postgres";
    private const string F_PASSWORD = "Vc&v_|3x@7Z>l2J1";
    private const string F_PORT = "30151";
    
    public const string NEW_FORUM_CONNECTION = $"Host={F_HOST};Port={F_PORT};Username={F_USER_NAME};Password={F_PASSWORD};Database=lctech_jkf_forum;Timeout=1024;CommandTimeout=1800;Maximum Pool Size=80;SSL Mode=Require;Trust Server Certificate=true;Include Error Detail=true";
    
    public const string D = "";
    public const string INSERT_DATA_PATH = "../../../ScriptInsert";

    public const string COPY_ENTITY_SUFFIX = $",\"{nameof(Entity.CreationDate)}\",\"{nameof(Entity.CreatorId)}\",\"{nameof(Entity.ModificationDate)}\",\"{nameof(Entity.ModifierId)}\",\"{nameof(Entity.Version)}\") " +
                                             $"FROM STDIN (DELIMITER '{D}')\n";

    public const string COPY_SUFFIX = $") FROM STDIN (DELIMITER '{D}')\n";

    public const string SCHEMA_PATH = "../../../ScriptSchema";
    public const string BEFORE_FILE_NAME = "BeforeCopy.sql";
    public const string AFTER_FILE_NAME = "AfterCopy.sql";
    
    public const string BLOG_ID = "BlogId";

    public const string LOOK_ES_INDEX = "lctech-jktank-dev";
    public const string LOOK_ES_CONNECTION = "https://es-jkforum-dev.es.asia-east1.gcp.elastic-cloud.com";
    public const string LOOK_ES_PASSWORD = "NXRvZDVvY0JDd284cFNTREM2U2Y6WHJpU3VIWnhTRUdHdmE2YkVqc0xkdw==";
    public const int LOOK_ES_BATCH_SIZE = 5000;
    
    public static readonly long? TestBlogId = null;
}