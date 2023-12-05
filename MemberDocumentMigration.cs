using Dapper;
using JKTankDataMigration.Helpers;
using Lctech.JKTank.Core.Domain.Entities;
using Lctech.JKTank.Core.Domain.Enums;
using Nest;
using Npgsql;
using Member = Lctech.JKTank.Core.Domain.Documents.Member;

namespace JKTankDataMigration;

public class MemberDocumentMigration(IElasticClient elasticClient)
{
    private readonly string _elasticIndex = ElasticHelper.GetMemberIndex(Setting.TANK_ES_INDEX);
    private static readonly Dictionary<long, long[]> UserRoleDic = TankHelper.GetAuthUserRoleDic();

    private const string QUERY_TANK_MEMBER_SQL = $"""
                                                   SELECT m."Id",
                                                          m."{nameof(Member.PrivacyType)}",
                                                          ms."{nameof(MemberStatistic.HotScore)}",
                                                          ms."{nameof(MemberStatistic.ViewCount)}",
                                                          ms."{nameof(MemberStatistic.ObtainDonateCount)}",
                                                          ms."{nameof(MemberStatistic.ObtainPurchaseCount)}",
                                                          ms."{nameof(MemberStatistic.ActualObtainDonateJPoints)}",
                                                          ms."{nameof(MemberStatistic.ActualObtainPurchaseJPoints)}",
                                                          ms."{nameof(MemberStatistic.ActualObtainTotalJPoints)}",
                                                          ms."{nameof(MemberStatistic.ObtainDonateJPoints)}",
                                                          ms."{nameof(MemberStatistic.ObtainPurchaseJPoints)}",
                                                          ms."{nameof(MemberStatistic.ObtainTotalJPoints)}",
                                                          ms."{nameof(MemberStatistic.PurchaseCount)}",
                                                          ms."{nameof(MemberStatistic.DonateJPoints)}",
                                                          ms."{nameof(MemberStatistic.PurchaseJPoints)}",
                                                          ms."{nameof(MemberStatistic.ConsumeTotalJPoints)}",
                                                          ms."{nameof(MemberStatistic.CommentCount)}",
                                                          ms."{nameof(MemberStatistic.ReactCount)}",
                                                          ms."{nameof(MemberStatistic.FavoriteCount)}",
                                                          ms."{nameof(MemberStatistic.FollowerCount)}",
                                                          ms."{nameof(MemberStatistic.MassageBlogCount)}",
                                                          ms."{nameof(MemberStatistic.LinkMassageBlogCount)}",
                                                          ms."{nameof(MemberStatistic.PricingBlogCount)}",
                                                          ms."{nameof(MemberStatistic.TotalBlogCount)}",
                                                          m."{nameof(Member.CreationDate)}"
                                                          FROM "Member" m
                                                          LEFT JOIN "MemberStatistic" ms ON ms."Id" = m."Id"
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
        
        Member[] documents;

        await using (var cn = new NpgsqlConnection(Setting.TANK_CONNECTION))
        {
            documents = cn.Query<Member>(QUERY_TANK_MEMBER_SQL).ToArray();
        }

        foreach (var member in documents)
        {
            if (UserRoleDic.TryGetValue(member.Id, out var roleIds))
                member.RoleIds = roleIds;
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

                     var response = await elasticClient.BulkAsync(descriptor => descriptor.IndexMany(documents[offset..length], (des, doc) => des.Index(_elasticIndex)
                                                                                                                                                  .Id(doc.Id)
                                                                                                                                                  .Document(doc)), cancellationToken);

                     if (!response.IsValid && response.OriginalException is not null)
                     {
                         throw new Exception(response.OriginalException.Message);
                     }

                     offset += Setting.TANK_ES_BATCH_SIZE;
                 });
        }
    }
}