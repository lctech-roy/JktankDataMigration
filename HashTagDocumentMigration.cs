using Dapper;
using JKTankDataMigration.Helpers;
using Lctech.JKTank.Core.Domain.Documents;
using Nest;
using Npgsql;

namespace JKTankDataMigration;

public class HashTagDocumentMigration(IElasticClient elasticClient)
{
    private readonly string _elasticIndex = ElasticHelper.GetHashtagIndex(Setting.TANK_ES_INDEX);

    private const string QUERY_TANK_HASH_TAG_SQL = $"""
                                                    SELECT "{nameof(TempHashtagDocument.Id)}",
                                                           "{nameof(TempHashtagDocument.Name)}",
                                                           "{nameof(TempHashtagDocument.RelationBlogCount)}"
                                                    FROM "Hashtag"
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

        TempHashtagDocument[] documents;

        await using (var cn = new NpgsqlConnection(Setting.TANK_CONNECTION))
        {
            documents = cn.Query<TempHashtagDocument>(QUERY_TANK_HASH_TAG_SQL).ToArray();
        }

        var hashTagDocuments = documents.Select(x => new Hashtag
                                                     {
                                                         Name = x.Name,
                                                         RelationBlogCount = x.RelationBlogCount
                                                     });

        var tempHashtagDic = documents.ToDictionary(x => x.Name, x => x.Id);


        var response = await elasticClient.BulkAsync(descriptor => descriptor.IndexMany(hashTagDocuments, (des, doc) => des.Index(_elasticIndex)
                                                                                                                           .Id(tempHashtagDic[doc.Name])
                                                                                                                           .Document(doc)), cancellationToken);

        if (!response.IsValid && response.OriginalException is not null)
        {
            throw new Exception(response.OriginalException.Message);
        }
    }

    private class TempHashtagDocument : Hashtag
    {
        public long Id { get; set; }
    }
}