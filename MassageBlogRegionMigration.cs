using System.Text;
using Dapper;
using JLookDataMigration.Extensions;
using JLookDataMigration.Helpers;
using Lctech.JLook.Core.Domain.Entities;
using Npgsql;

namespace JLookDataMigration;

public class MassageBlogRegionMigration
{
    private const string QUERY_MASSAGE_CATEGORY = $@"SELECT a.""Id"" AS {nameof(MassageBlogRegion.Id)}, 
                                     a.""Name"" AS {nameof(MassageBlogRegion.Name)}
                                     FROM ""ArticleCategory"" a
                                     WHERE a.""BoardId"" = 1128";
    
    private const string COPY_MASSAGE_BLOG_REGION_PREFIX = $"COPY \"{nameof(MassageBlogRegion)}\" (\"{nameof(MassageBlogRegion.Id)}\",\"{nameof(MassageBlogRegion.Name)}\"" + Setting.COPY_ENTITY_SUFFIX;
    
    private const string MASSAGE_BLOG_REGION_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(MassageBlogRegion)}";
    
    public void Migration()
    {
        using var cn = new NpgsqlConnection(Setting.NEW_FORUM_CONNECTION);

        var massageBlogRegions = cn.Query<MassageBlogRegion>(QUERY_MASSAGE_CATEGORY).ToArray();

        var dateNow = DateTimeOffset.UtcNow;

        var massageBlogRegionSb = new StringBuilder();
        
        foreach (var massageBlogRegion in massageBlogRegions)
        {
            massageBlogRegionSb.AppendValueLine(massageBlogRegion.Id, massageBlogRegion.Name, dateNow, 0, dateNow, 0, 0);
        }
        
        FileHelper.WriteToFile(MASSAGE_BLOG_REGION_PATH, $"{nameof(MassageBlogRegion)}.sql", COPY_MASSAGE_BLOG_REGION_PREFIX, massageBlogRegionSb);
    }
}