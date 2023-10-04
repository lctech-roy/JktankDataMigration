using Dapper;
using JKTankDataMigration.Helpers;
using Lctech.JKTank.Core.Domain.Documents;
using Nest;
using Netcorext.Extensions.Commons;
using Npgsql;

namespace JKTankDataMigration;

public class BlogDocumentMigration
{
    private readonly IElasticClient _elasticClient;
    private readonly string _elasticIndex;
    private static readonly Dictionary<long, long[]> UserRoleDic = LookAuthHelper.GetLookAuthUserRoleDic();
    private static readonly Dictionary<long, long[]> BlogSpecialTagsDic = LookSpecialTagHelper.GetBlogSpecialTagsDic();

    private const string QUERY_LOOK_BLOG_SQL = @"SELECT 
                                                    b.""Id"",
                                                    b.""Subject"",
                                                    mbc.""Name"" AS Category,
                                                    b.""Status"",
                                                    m.""PrivacyType"",
                                                    b.""VisibleType"",
                                                    b.""Title"",
                                                    b.""Content"",
                                                    b.""Cover"",
                                                    bs.""ObtainTotalJPoints"",
                                                    bs.""CommentCount"",
                                                    bs.""HotScore"",
                                                    bs.""ViewCount"",
                                                    bs.""DonateCount"",
                                                    bs.""DonorCount"",
                                                    bs.""PurchaseCount"",
                                                    bs.""DonateJPoints"",
                                                    bs.""PurchaseJPoints"",
                                                    bs.""ActualDonateJPoints"",
                                                    bs.""ActualPurchaseJPoints"",
                                                    bs.""ActualObtainTotalJPoints"",
                                                    bs.""ObtainTotalJPoints"",
                                                    bs.""CommentCount"",
                                                    bs.""ComeByReactCount"",
                                                    bs.""AmazingReactCount"",
                                                    bs.""ShakeHandsReactCount"",
                                                    bs.""FlowerReactCount"",
                                                    bs.""ConfuseReactCount"",
                                                    bs.""TotalReactCount"",
                                                    bs.""FavoriteCount"",
                                                    bs.""ServiceScore"",
                                                    bs.""AppearanceScore"",
                                                    bs.""ConversationScore"",
                                                    bs.""TidinessScore"",
                                                    bs.""AverageScore"",
                                                    b.""Hashtags"",
                                                    b.""CreatorId"",
                                                    m.""DisplayName"" AS CreatorName,
                                                    b.""CreationDate"",
                                                    b.""LastEditDate"",
                                                    b.""MassageBlogId"",
                                                    msb.""RegionId"" AS MassageBlogRegionId,
                                                    msb.""RelationBlogCount"" AS MassageBlogRelationBlogCount,
                                                    msb.""ExpirationDate"" AS MassageBlogExpirationDate,
                                                    b.""Disabled""
                                                    FROM ""Blog"" b
                                                    LEFT JOIN ""BlogStatistic"" bs ON b.""Id"" = bs.""Id""
                                                    LEFT JOIN ""MassageBlog"" msb ON b.""MassageBlogId"" = msb.""Id""
                                                    LEFT JOIN ""Member"" m ON b.""CreatorId"" = m.""Id""
                                                    LEFT JOIN ""MemberBlogCategory"" mbc ON b.""CategoryId"" = mbc.""Id""";

    public BlogDocumentMigration(IElasticClient elasticClient)
    {
        _elasticClient = elasticClient;
        _elasticIndex = ElasticHelper.GetBlogIndex(Setting.LOOK_ES_INDEX);
    }

    public async Task MigrationAsync(CancellationToken cancellationToken = new())
    {
        TempBlogDocument[] documents;

        await using (var cn = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION))
        {
            documents = cn.Query<TempBlogDocument>(QUERY_LOOK_BLOG_SQL).ToArray();
        }

        var documentGroup = documents.GroupBy(x => x.Disabled).ToArray();

        var deleteDocuments = documentGroup.FirstOrDefault(x => x.Key)?.Select(x => new Blog
                                                                                    {
                                                                                        Id = x.Id,
                                                                                        CreationDate = x.CreationDate
                                                                                    }).ToArray();

        if (!deleteDocuments.IsEmpty())
        {
            var deleteRep = await _elasticClient.BulkAsync(descriptor => descriptor
                                                              .DeleteMany(deleteDocuments, (des, doc) => des.Index(_elasticIndex)
                                                                                                            .Id(doc.Id)), cancellationToken);

            if (!deleteRep.IsValid && deleteRep.OriginalException is not null)
            {
                throw deleteRep.OriginalException;
            }
        }

        var updateDocuments = documentGroup.First(x => !x.Key).Select(x =>
                                                                      {
                                                                          var blogDocument = (Blog)x;

                                                                          blogDocument.Title = KeywordHelper.ReplaceWords(blogDocument.Title);
                                                                          blogDocument.Content = KeywordHelper.ReplaceWords(blogDocument.Content);

                                                                          if (UserRoleDic.TryGetValue(blogDocument.CreatorId, out var roleIds))
                                                                              blogDocument.CreatorRoleIds = roleIds;

                                                                          if (BlogSpecialTagsDic.TryGetValue(blogDocument.Id, out var specialTags))
                                                                              blogDocument.SpecialTags = specialTags;

                                                                          return blogDocument;
                                                                      }).ToArray();

        var offset = 0;

        while (offset < updateDocuments.Length)
        {
            await CommonHelper.WatchTimeAsync
                (
                 $"{nameof(_elasticClient.BulkAsync)}({Setting.LOOK_ES_BATCH_SIZE}) offset:{offset}",
                 async () =>
                 {
                     var length = offset + Setting.LOOK_ES_BATCH_SIZE;

                     if (length > updateDocuments.Length)
                         length = updateDocuments.Length;

                     var response = await _elasticClient.BulkAsync(descriptor => descriptor.IndexMany(updateDocuments[offset..length],
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

    private class TempBlogDocument : Blog
    {
        public bool Disabled { get; set; }
    }
}