﻿// See https://aka.ms/new-console-template for more information

using System.Globalization;
using Elasticsearch.Net;
using JKTankDataMigration;
using JKTankDataMigration.Helpers;
using Mapster;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nest;
using Netcorext.Algorithms;

// 1. 建立依賴注入的容器
var serviceCollection = new ServiceCollection();

// 2. 註冊服務
serviceCollection.AddSingleton<ISnowflake>(_ => new SnowflakeJavaScriptSafeInteger((uint)new Random().Next(1, 31)));
serviceCollection.AddSingleton<Migration>();
serviceCollection.AddSingleton<MemberMigration>();
serviceCollection.AddSingleton<MemberBlogCategoryMigration>();
serviceCollection.AddSingleton<MassageBlogRegionMigration>();
serviceCollection.AddSingleton<BlogMigration>();
serviceCollection.AddSingleton<BlogReactMigration>();
serviceCollection.AddSingleton<CommentMigration>();
serviceCollection.AddSingleton<MemberFavoriteMigration>();
serviceCollection.AddSingleton<MemberFollowerMigration>();
serviceCollection.AddSingleton<MemberStatisticMigration>();
serviceCollection.AddSingleton<BlogPinMigration>();

serviceCollection.AddSingleton<MemberDocumentMigration>();
serviceCollection.AddSingleton<BlogDocumentMigration>();
serviceCollection.AddSingleton<CommentDocumentMigration>();
serviceCollection.AddSingleton<HashTagDocumentMigration>();

serviceCollection.AddSingleton<FileExtensionContentTypeProvider>(_ =>
                                                                 {
                                                                     var fileExtensionContentTypeProvider = new FileExtensionContentTypeProvider();
                                                                     fileExtensionContentTypeProvider.Mappings.Add(".heif", "image/heif");
                                                                     fileExtensionContentTypeProvider.Mappings.Add(".heic", "image/heic");

                                                                     return fileExtensionContentTypeProvider;
                                                                 });

TypeAdapterConfig.GlobalSettings.LoadProtobufConfig();

serviceCollection.AddSingleton<IElasticClient>(_ => new ElasticClient(new ConnectionSettings(new Uri(Setting.LOOK_ES_CONNECTION))
                                                                     .ApiKeyAuthentication(new ApiKeyAuthenticationCredentials(Setting.LOOK_ES_PASSWORD))
                                                                     .DisableDirectStreaming()));

// 建立依賴服務提供者
var serviceProvider = serviceCollection.BuildServiceProvider();

Directory.CreateDirectory(Setting.INSERT_DATA_PATH);
Directory.CreateDirectory(Setting.INSERT_DATA_PATH + "/Error");

// 3. 執行主服務
var migration = serviceProvider.GetRequiredService<Migration>();

var blogCategoryMigration = serviceProvider.GetRequiredService<MemberBlogCategoryMigration>();
var memberMigration = serviceProvider.GetRequiredService<MemberMigration>();
var massageBlogRegionMigration = serviceProvider.GetRequiredService<MassageBlogRegionMigration>();
var blogMigration = serviceProvider.GetRequiredService<BlogMigration>();
var blogReactMigration = serviceProvider.GetRequiredService<BlogReactMigration>();
var commentMigration = serviceProvider.GetRequiredService<CommentMigration>();
var memberFavoriteMigration = serviceProvider.GetRequiredService<MemberFavoriteMigration>();
var memberFollowerMigration = serviceProvider.GetRequiredService<MemberFollowerMigration>();
var memberStatisticMigration =  serviceProvider.GetRequiredService<MemberStatisticMigration>();
var blogPinMigration =  serviceProvider.GetRequiredService<BlogPinMigration>();

var memberDocumentMigration =  serviceProvider.GetRequiredService<MemberDocumentMigration>();
var blogDocumentMigration =  serviceProvider.GetRequiredService<BlogDocumentMigration>();
var commentDocumentMigration =  serviceProvider.GetRequiredService<CommentDocumentMigration>();
var hashTagDocumentMigration =  serviceProvider.GetRequiredService<HashTagDocumentMigration>();

var token = new CancellationTokenSource().Token;

Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

// Blog會員
// await CommonHelper.WatchTimeAsync(nameof(memberMigration), async () => await memberMigration.MigrationAsync(token));
// await CommonHelper.WatchTimeAsync(nameof(migration.ExecuteMemberAsync), () => migration.ExecuteMemberAsync());

// 日誌分類
// CommonHelper.WatchTime(nameof(blogCategoryMigration),  () =>  blogCategoryMigration.Migration());
// await CommonHelper.WatchTimeAsync(nameof(migration.ExecuteMemberBlogCategoryAsync), () => migration.ExecuteMemberBlogCategoryAsync());

// 日誌-1128地區分類
// CommonHelper.WatchTime(nameof(massageBlogRegionMigration),  () =>  massageBlogRegionMigration.Migration());
// await CommonHelper.WatchTimeAsync(nameof(migration.ExecuteMassageBlogRegionAsync), () => migration.ExecuteMassageBlogRegionAsync());

// 日誌
// await CommonHelper.WatchTimeAsync(nameof(blogMigration), async () => await blogMigration.MigrationAsync(token));
// await CommonHelper.WatchTimeAsync(nameof(migration.ExecuteBlogAsync), () => migration.ExecuteBlogAsync());

// 日誌-會員表態
// await CommonHelper.WatchTimeAsync(nameof(blogReactMigration), async () => await blogReactMigration.MigrationAsync(token));
// await CommonHelper.WatchTimeAsync(nameof(migration.ExecuteBlogReactAsync), () => migration.ExecuteBlogReactAsync());

// 日誌-留言
// await CommonHelper.WatchTimeAsync(nameof(commentMigration), async () => await commentMigration.MigrationAsync(token));
// await CommonHelper.WatchTimeAsync(nameof(migration.ExecuteCommentAsync), () => migration.ExecuteCommentAsync());

// Blog會員-收藏日誌
// await CommonHelper.WatchTimeAsync(nameof(memberFavoriteMigration), async () => await memberFavoriteMigration.MigrationAsync(token));
// await CommonHelper.WatchTimeAsync(nameof(migration.ExecuteMemberFavoriteAsync), () => migration.ExecuteMemberFavoriteAsync());

// Blog會員-追蹤會員
// await CommonHelper.WatchTimeAsync(nameof(memberFollowerMigration), async () => await memberFollowerMigration.MigrationAsync(token));
// await CommonHelper.WatchTimeAsync(nameof(migration.ExecuteMemberFollowerAsync), () => migration.ExecuteMemberFollowerAsync());

// Blog會員-統計資料
// await CommonHelper.WatchTimeAsync(nameof(memberStatisticMigration), async () => await memberStatisticMigration.MigrationAsync(token));
// await CommonHelper.WatchTimeAsync(nameof(migration.ExecuteStatisticAsync), () => migration.ExecuteStatisticAsync());

// Blog-更新置頂
// await CommonHelper.WatchTimeAsync(nameof(blogPinMigration), async () => await blogPinMigration.MigrationAsync(token));

// es-會員
// await CommonHelper.WatchTimeAsync(nameof(memberDocumentMigration), async () => await memberDocumentMigration.MigrationAsync(token));

// es-日誌
//await CommonHelper.WatchTimeAsync(nameof(blogDocumentMigration), async () => await blogDocumentMigration.MigrationAsync(token));

// es-日誌標籤
// await CommonHelper.WatchTimeAsync(nameof(hashTagDocumentMigration), async () => await hashTagDocumentMigration.MigrationAsync(token));

// es-留言
await CommonHelper.WatchTimeAsync(nameof(commentDocumentMigration), async () => await commentDocumentMigration.MigrationAsync(token));

Console.WriteLine("Hello, World!");