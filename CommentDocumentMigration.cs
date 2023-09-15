using Dapper;
using JKTankDataMigration.Helpers;
using Lctech.JKTank.Core.Domain.Documents;
using Nest;
using Npgsql;

namespace JKTankDataMigration;

public class CommentDocumentMigration
{
    private readonly IElasticClient _elasticClient;
    private readonly string _elasticIndex;

    private const string QUERY_LOOK_COMMENT_SQL = @"SELECT 
                                                    c.""Id"",
                                                    c.""BlogId"",
                                                    c.""Type"",
                                                    c.""Disabled"",
                                                    c.""Content"",
                                                    c.""CreatorId"",
                                                    m.""DisplayName"" AS CreatorName,
                                                    c.""CreationDate""
                                                    FROM ""Comment"" c 
                                                    LEFT JOIN ""Member"" m ON c.""CreatorId"" = m.""Id""";

    public CommentDocumentMigration(IElasticClient elasticClient)
    {
        _elasticClient = elasticClient;
        _elasticIndex = ElasticHelper.GetCommentIndex(Setting.LOOK_ES_INDEX);
    }

    public async Task MigrationAsync(CancellationToken cancellationToken = new())
    {
        Comment[] documents;

        await using (var cn = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION))
        {
            documents = cn.Query<Comment>(QUERY_LOOK_COMMENT_SQL).ToArray();
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
                 $"{nameof(_elasticClient.BulkAsync)}({Setting.LOOK_ES_BATCH_SIZE}) offset:{offset}",
                 async () =>
                 {
                     var length = offset + Setting.LOOK_ES_BATCH_SIZE;

                     if (length > documents.Length)
                         length = documents.Length;

                     var response = await _elasticClient.BulkAsync(descriptor => descriptor.IndexMany(documents[offset..length],
                                                                                                      (des, doc) => des.Index(_elasticIndex)
                                                                                                                       .Id(doc.Id)
                                                                                                                       .Document(doc)), cancellationToken);

                     if (!response.IsValid && response.OriginalException is not null)
                     {
                         throw response.OriginalException;
                     }

                     offset += Setting.LOOK_ES_BATCH_SIZE;
                 });
        }
    }
}