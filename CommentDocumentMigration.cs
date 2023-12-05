using Dapper;
using JKTankDataMigration.Helpers;
using Lctech.JKTank.Core.Domain.Documents;
using Nest;
using Npgsql;

namespace JKTankDataMigration;

public class CommentDocumentMigration(IElasticClient elasticClient)
{
    private readonly string _elasticIndex = ElasticHelper.GetCommentIndex(Setting.TANK_ES_INDEX);

    private const string QUERY_TANK_COMMENT_SQL = $"""
                                                  SELECT c."{nameof(Comment.Id)}",
                                                         c."{nameof(Comment.BlogId)}",
                                                         c."{nameof(Comment.Type)}",
                                                         b."Disabled" AS {nameof(Comment.BlogDisabled)},
                                                         c."{nameof(Comment.Content)}",
                                                         c."{nameof(Comment.CreatorId)}",
                                                         m."DisplayName" AS {nameof(Comment.CreatorName)},
                                                         c."{nameof(Comment.CreationDate)}"
                                                         FROM "Comment" c
                                                         INNER JOIN "Blog" b ON b."Id" = c."BlogId"
                                                         LEFT JOIN "Member" m ON c."CreatorId" = m."Id"
                                                         WHERE c."Disabled" = FALSE
                                                  """;

    public async Task MigrationAsync(CancellationToken cancellationToken = new())
    {
        var deleteResponse = await elasticClient.DeleteByQueryAsync(new DeleteByQueryRequest(_elasticIndex)
                                                                    {
                                                                        Query = new MatchAllQuery()
                                                                    }, cancellationToken);
        
        if (!deleteResponse.IsValid && deleteResponse.OriginalException is not null)
        {
            throw new Exception(deleteResponse.OriginalException.Message);
        }
        
        Comment[] documents;

        await using (var cn = new NpgsqlConnection(Setting.TANK_CONNECTION))
        {
            documents = cn.Query<Comment>(QUERY_TANK_COMMENT_SQL).ToArray();
        }

        foreach (var document in documents)
        {
            document.Content = KeywordHelper.ReplaceWords(document.Content);
        }
        
        var offset = 0;

        while (offset < documents.Length)
        {
            await CommonHelper.WatchTimeAsync
                (
                 $"{nameof(elasticClient.BulkAsync)}({Setting.TANK_ES_BATCH_SIZE}) offset:{offset}",
                 async () =>
                 {
                     var length = offset + Setting.TANK_ES_BATCH_SIZE;

                     if (length > documents.Length)
                         length = documents.Length;

                     var response = await elasticClient.BulkAsync(descriptor => descriptor.IndexMany(documents[offset..length],
                                                                                                      (des, doc) => des.Index(_elasticIndex)
                                                                                                                       .Id(doc.Id)
                                                                                                                       .Document(doc)), cancellationToken);

                     if (!response.IsValid && response.OriginalException is not null)
                     {
                         throw response.OriginalException;
                     }

                     offset += Setting.TANK_ES_BATCH_SIZE;
                 });
        }
    }
}