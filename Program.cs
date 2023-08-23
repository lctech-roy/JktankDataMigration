// See https://aka.ms/new-console-template for more information

using System.Globalization;
using JLookDataMigration;
using JLookDataMigration.Helpers;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Netcorext.Algorithms;

// 1. 建立依賴注入的容器
var serviceCollection = new ServiceCollection();

// 2. 註冊服務
serviceCollection.AddSingleton<ISnowflake>(_ => new SnowflakeJavaScriptSafeInteger((uint)new Random().Next(1, 31)));
serviceCollection.AddSingleton<Migration>();
serviceCollection.AddSingleton<MemberMigration>();
serviceCollection.AddSingleton<MemberBlogCategoryMigration>();
serviceCollection.AddSingleton<BlogMigration>();
serviceCollection.AddSingleton<BlogReactMigration>();
serviceCollection.AddSingleton<CommentMigration>();
serviceCollection.AddSingleton<MemberFavoriteMigration>();
serviceCollection.AddSingleton<MemberFollowerMigration>();
serviceCollection.AddSingleton<MemberStatisticMigration>();

serviceCollection.AddSingleton<FileExtensionContentTypeProvider>(_ =>
                                                                 {
                                                                     var fileExtensionContentTypeProvider = new FileExtensionContentTypeProvider();
                                                                     fileExtensionContentTypeProvider.Mappings.Add(".heif", "image/heif");
                                                                     fileExtensionContentTypeProvider.Mappings.Add(".heic", "image/heic");

                                                                     return fileExtensionContentTypeProvider;
                                                                 });

// 建立依賴服務提供者
var serviceProvider = serviceCollection.BuildServiceProvider();

Directory.CreateDirectory(Setting.INSERT_DATA_PATH);
Directory.CreateDirectory(Setting.INSERT_DATA_PATH + "/Error");

// 3. 執行主服務
var migration = serviceProvider.GetRequiredService<Migration>();

var blogCategoryMigration = serviceProvider.GetRequiredService<MemberBlogCategoryMigration>();
var memberMigration = serviceProvider.GetRequiredService<MemberMigration>();
var blogMigration = serviceProvider.GetRequiredService<BlogMigration>();
var blogReactMigration = serviceProvider.GetRequiredService<BlogReactMigration>();
var commentMigration = serviceProvider.GetRequiredService<CommentMigration>();
var memberFavoriteMigration = serviceProvider.GetRequiredService<MemberFavoriteMigration>();
var memberFollowerMigration = serviceProvider.GetRequiredService<MemberFollowerMigration>();
var memberStatisticMigration =  serviceProvider.GetRequiredService<MemberStatisticMigration>();

var token = new CancellationTokenSource().Token;

Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

// Blog會員
await CommonHelper.WatchTimeAsync(nameof(memberMigration), async () => await memberMigration.MigrationAsync(token));
await CommonHelper.WatchTimeAsync(nameof(migration.ExecuteMemberAsync), () => migration.ExecuteMemberAsync());

// 日誌分類
// CommonHelper.WatchTime(nameof(blogCategoryMigration),  () =>  blogCategoryMigration.Migration());
// await CommonHelper.WatchTimeAsync(nameof(migration.ExecuteMemberBlogCategoryAsync), () => migration.ExecuteMemberBlogCategoryAsync());

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

Console.WriteLine("Hello, World!");