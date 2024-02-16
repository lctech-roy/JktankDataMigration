using Dapper;
using JKTankDataMigration.Helpers;
using Lctech.JKTank.Core.Domain.Entities;
using Nest;
using Netcorext.Extensions.Commons;
using Npgsql;
using Blog = Lctech.JKTank.Core.Documents.Blog;

namespace JKTankDataMigration;

public class BlogDocumentMigration(IElasticClient elasticClient)
{
    private readonly string _elasticIndex = ElasticHelper.GetBlogIndex(Setting.TANK_ES_INDEX);
    private static readonly Dictionary<long, long[]> UserRoleDic = TankHelper.GetAuthUserRoleDic();
    private static readonly Dictionary<long, long[]> BlogSpecialTagsDic = TankHelper.GetBlogSpecialTagsDic();

    private const string QUERY_TANK_BLOG_SQL = $"""
                                                SELECT b."{nameof(TempBlogDocument.Id)}",
                                                       b."{nameof(TempBlogDocument.Subject)}",
                                                       mbc."{nameof(MemberBlogCategory.Name)}" AS {nameof(TempBlogDocument.Category)},
                                                       b."{nameof(TempBlogDocument.Status)}",
                                                       m."{nameof(TempBlogDocument.PrivacyType)}",
                                                       b."{nameof(TempBlogDocument.VisibleType)}",
                                                       b."{nameof(TempBlogDocument.Title)}",
                                                       b."{nameof(TempBlogDocument.Content)}",
                                                       b."{nameof(TempBlogDocument.Cover)}",
                                                       bs."{nameof(TempBlogDocument.HotScore)}",
                                                       bs."{nameof(TempBlogDocument.ViewCount)}",
                                                       bs."{nameof(TempBlogDocument.DonateCount)}",
                                                       bs."{nameof(TempBlogDocument.DonorCount)}",
                                                       bs."{nameof(TempBlogDocument.PurchaseCount)}",
                                                       bs."{nameof(TempBlogDocument.DonateJPoints)}",
                                                       bs."{nameof(TempBlogDocument.PurchaseJPoints)}",
                                                       bs."{nameof(TempBlogDocument.ActualDonateJPoints)}",
                                                       bs."{nameof(TempBlogDocument.ActualPurchaseJPoints)}",
                                                       bs."{nameof(TempBlogDocument.ActualObtainTotalJPoints)}",
                                                       bs."{nameof(TempBlogDocument.ObtainTotalJPoints)}",
                                                       bs."{nameof(TempBlogDocument.CommentCount)}",
                                                       bs."{nameof(TempBlogDocument.ComeByReactCount)}",
                                                       bs."{nameof(TempBlogDocument.AmazingReactCount)}",
                                                       bs."{nameof(TempBlogDocument.ShakeHandsReactCount)}",
                                                       bs."{nameof(TempBlogDocument.FlowerReactCount)}",
                                                       bs."{nameof(TempBlogDocument.ConfuseReactCount)}",
                                                       bs."{nameof(TempBlogDocument.TotalReactCount)}",
                                                       bs."{nameof(TempBlogDocument.FavoriteCount)}",
                                                       b."{nameof(TempBlogDocument.ServiceScore)}",
                                                       b."{nameof(TempBlogDocument.AppearanceScore)}",
                                                       b."{nameof(TempBlogDocument.ConversationScore)}",
                                                       b."{nameof(TempBlogDocument.TidinessScore)}",
                                                       b."{nameof(TempBlogDocument.AverageScore)}",
                                                       b."{nameof(TempBlogDocument.Hashtags)}",
                                                       b."{nameof(TempBlogDocument.CreatorId)}",
                                                       m."{nameof(Member.DisplayName)}" AS {nameof(TempBlogDocument.CreatorName)},
                                                       b."{nameof(TempBlogDocument.CreationDate)}",
                                                       b."{nameof(TempBlogDocument.LastEditDate)}",
                                                       msb."{nameof(MassageBlog.Image)}" AS {nameof(TempBlogDocument.Image)},
                                                       b."{nameof(TempBlogDocument.MassageBlogId)}",
                                                       msb."{nameof(MassageBlog.RegionId)}" AS {nameof(TempBlogDocument.MassageBlogRegionId)},
                                                       msbg."{nameof(MassageBlogRegion.Name)}" AS {nameof(TempBlogDocument.MassageBlogRegion)},
                                                       msb."{nameof(MassageBlog.ExpirationDate)}" AS {nameof(TempBlogDocument.MassageBlogExpirationDate)},
                                                       b."{nameof(TempBlogDocument.Disabled)}",
                                                       br."{nameof(BlogReview.CommentReviewStatus)}" AS {nameof(TempBlogDocument.CommentReviewStatus)},
                                                       br."{nameof(BlogReview.BlogReviewStatus)}" AS {nameof(TempBlogDocument.BlogReviewStatus)}
                                                       FROM "Blog" b
                                                       LEFT JOIN "BlogStatistic" bs ON b."Id" = bs."Id"
                                                       LEFT JOIN "MassageBlog" msb ON b."MassageBlogId" = msb."Id"
                                                       LEFT JOIN "MassageBlogRegion" msbg ON msb."RegionId" = msbg."Id"
                                                       LEFT JOIN "Member" m ON b."CreatorId" = m."Id"
                                                       LEFT JOIN "MemberBlogCategory" mbc ON b."CategoryId" = mbc."Id"
                                                       LEFT JOIN "BlogReview" br ON b."Id" = br."Id"
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

        TempBlogDocument[] documents;

        await using (var cn = new NpgsqlConnection(Setting.TANK_CONNECTION))
        {
            documents = cn.Query<TempBlogDocument>(QUERY_TANK_BLOG_SQL).ToArray();
        }

        var documentGroup = documents.GroupBy(x => x.Disabled).ToArray();

        var deleteDocuments = documentGroup.FirstOrDefault(x => x.Key)?.Select(x => new Blog
                                                                                    {
                                                                                        Id = x.Id,
                                                                                        CreationDate = x.CreationDate
                                                                                    }).ToArray();

        if (!deleteDocuments.IsEmpty())
        {
            var deleteRep = await elasticClient.BulkAsync(descriptor => descriptor
                                                             .DeleteMany(deleteDocuments, (des, doc) => des.Index(_elasticIndex)
                                                                                                           .Id(doc.Id)), cancellationToken);

            if (!deleteRep.IsValid && deleteRep.OriginalException is not null)
            {
                throw deleteRep.OriginalException;
            }
        }

        var updateDocuments = documentGroup.First(x => !x.Key).Select(tempBlogDoc =>
                                                                      {
                                                                          var hasImage = !string.IsNullOrWhiteSpace(tempBlogDoc.Image);
                                                                          var lastEditDate = tempBlogDoc.LastEditDate ?? DateTimeOffset.MinValue;
                                                                          var massageBlogExpirationDate = tempBlogDoc.MassageBlogExpirationDate ?? DateTimeOffset.MinValue;

                                                                          var blogDoc = (Blog)tempBlogDoc;

                                                                          blogDoc.Title = KeywordHelper.ReplaceEsWords(blogDoc.Title);
                                                                          blogDoc.Content = KeywordHelper.ReplaceEsWords(blogDoc.Content);
                                                                          blogDoc.MassageBlogHasImage = hasImage;
                                                                          blogDoc.LastEditDate = lastEditDate;
                                                                          blogDoc.MassageBlogExpirationDate = massageBlogExpirationDate;

                                                                          if (UserRoleDic.TryGetValue(blogDoc.CreatorId, out var roleIds))
                                                                              blogDoc.CreatorRoleIds = roleIds;

                                                                          if (BlogSpecialTagsDic.TryGetValue(blogDoc.Id, out var specialTags))
                                                                              blogDoc.SpecialTags = specialTags;

                                                                          return blogDoc;
                                                                      }).ToArray();

        var offset = 0;

        while (offset < updateDocuments.Length)
        {
            await CommonHelper.WatchTimeAsync
                (
                 $"{nameof(elasticClient.BulkAsync)}({Setting.TANK_ES_BATCH_SIZE}) offset:{offset}",
                 async () =>
                 {
                     var length = offset + Setting.TANK_ES_BATCH_SIZE;

                     if (length > updateDocuments.Length)
                         length = updateDocuments.Length;

                     var response = await elasticClient.BulkAsync(descriptor => descriptor.IndexMany(updateDocuments[offset..length],
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

    private class TempBlogDocument : Blog
    {
        public bool Disabled { get; set; }
        public string? Image { get; set; }
        public new DateTimeOffset? LastEditDate { get; set; }
        public new DateTimeOffset? MassageBlogExpirationDate { get; set; }
    }
}