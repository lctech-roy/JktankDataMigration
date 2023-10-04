using Dapper;
using JKTankDataMigration.Helpers;
using Lctech.JKTank.Core.Domain.Documents;
using Nest;
using Npgsql;

namespace JKTankDataMigration;

public class MemberDocumentMigration
{
    private readonly IElasticClient _elasticClient;
    private readonly string _elasticIndex;
    private static readonly Dictionary<long, long[]> UserRoleDic = LookAuthHelper.GetLookAuthUserRoleDic();

    private const string QUERY_LOOK_MEMBER_SQL = $@"SELECT m.""Id"",m.""PrivacyType"",
                                                    ms.""HotScore"",
                                                    ms.""ViewCount"",
                                                    ms.""ObtainDonateCount"",
                                                    ms.""ObtainPurchaseCount"",
                                                    ms.""ObtainDonateJPoints"",
                                                    ms.""ObtainPurchaseJPoints"",
                                                    ms.""ObtainTotalJPoints"",
                                                    ms.""ActualObtainDonateJPoints"",
                                                    ms.""ActualObtainPurchaseJPoints"",
                                                    ms.""ActualObtainTotalJPoints"",
                                                    ms.""CommentCount"",
                                                    ms.""ReactCount"",
                                                    ms.""FavoriteCount"",
                                                    ms.""FollowerCount"",
                                                    ms.""MassageBlogCount"",
                                                    ms.""LinkMassageBlogCount"",
                                                    ms.""PricingBlogCount"",
                                                    ms.""TotalBlogCount""
                                                    FROM ""Member"" m
                                                    LEFT JOIN ""MemberStatistic"" ms ON ms.""Id"" = m.""Id""";
    
    public MemberDocumentMigration(IElasticClient elasticClient)
    {
        _elasticClient = elasticClient;
        _elasticIndex = ElasticHelper.GetMemberIndex(Setting.LOOK_ES_INDEX);
    }

    public async Task MigrationAsync(CancellationToken cancellationToken = new())
    {
        Member[] documents;

        await using (var cn = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION))
        {
            documents = cn.Query<Member>(QUERY_LOOK_MEMBER_SQL).ToArray();
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
                 $"{nameof(_elasticClient.BulkAsync)}({Setting.LOOK_ES_BATCH_SIZE}) offset:{offset}",
                 async () =>
                 {
                     var length = offset + Setting.LOOK_ES_BATCH_SIZE;

                     if (length > documents.Length)
                         length = documents.Length;

                     var response = await _elasticClient.BulkAsync(descriptor => descriptor.IndexMany(documents[offset..length], (des, doc) => des.Index(_elasticIndex)
                                                                                                                                                  .Id(doc.Id)
                                                                                                                                                  .Document(doc)), cancellationToken);

                     if (!response.IsValid && response.OriginalException is not null)
                     {
                         throw new Exception(response.OriginalException.Message);
                     }

                     offset += Setting.LOOK_ES_BATCH_SIZE;
                 });
        }
    }
}