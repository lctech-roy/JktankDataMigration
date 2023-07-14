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

var token = new CancellationTokenSource().Token;

Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

// Blog會員
// await CommonHelper.WatchTimeAsync(nameof(memberMigration), async () => await memberMigration.MigrationAsync(token));
await CommonHelper.WatchTimeAsync(nameof(migration.ExecuteMemberAsync), () => migration.ExecuteMemberAsync(token));

// 日誌分類
// CommonHelper.WatchTime(nameof(blogCategoryMigration),  () =>  blogCategoryMigration.Migration());
// await CommonHelper.WatchTimeAsync(nameof(migration.ExecuteMemberBlogCategoryAsync), () => migration.ExecuteMemberBlogCategoryAsync());

Console.WriteLine("Hello, World!");