using System.Text;
using Dapper;
using JLookDataMigration.Extensions;
using JLookDataMigration.Helpers;
using JLookDataMigration.Models;
using Lctech.JLook.Core.Domain.Entities;
using Lctech.JLook.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Polly;

namespace JLookDataMigration;

public class BlogReactMigration
{
    private const string COPY_BLOG_REACT_PREFIX = $"COPY \"{nameof(BlogReact)}\" " +
                                                  $"(\"{nameof(BlogReact.Id)}\",\"{nameof(BlogReact.Type)}\"" +
                                                  Setting.COPY_ENTITY_SUFFIX;

    private static readonly string QueryBlogReactSql = @"SELECT uid, id , clickid ,dateline FROM pre_home_clickuser 
                                                          WHERE idtype = 'blogid' ORDER BY dateline DESC";

    private const string BLOG_REACT_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(BlogReact)}";

    public async Task MigrationAsync(CancellationToken cancellationToken)
    {
        FileHelper.RemoveFiles(new[] { BLOG_REACT_PATH });

        var blogReactSb = new StringBuilder();

        await using var conn = new MySqlConnection(Setting.OLD_FORUM_CONNECTION);

        await conn.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(QueryBlogReactSql, conn);

        var reader = await command.ExecuteReaderAsync(cancellationToken);

        var distinctHash = new HashSet<(long, long)>();
        
        while (await reader.ReadAsync(cancellationToken))
        {
            var memberId = reader.GetInt64(0);
            var id = reader.GetInt64(1);
            
            if(distinctHash.Contains((id,memberId)))
                continue;

            distinctHash.Add((id, memberId));
            
            var clickId = reader.GetInt32(2);
            var dateLine =  reader.GetInt64(3);
            var createDate = DateTimeOffset.FromUnixTimeSeconds(dateLine);
            
            var type = clickId switch
                       {
                           1 => ReactType.ComeBy,
                           2 => ReactType.Amazing,
                           3 => ReactType.ShakeHands,
                           4 => ReactType.Flower,
                           5 => ReactType.Confuse,
                           _ => ReactType.None
                       };
                    
            blogReactSb.AppendValueLine(id, (int)type,
                                        createDate, memberId, createDate, memberId, 0);
        }

        await reader.CloseAsync();
        
        FileHelper.WriteToFile(BLOG_REACT_PATH, $"{nameof(BlogReact)}.sql", COPY_BLOG_REACT_PREFIX, blogReactSb);
    }
}