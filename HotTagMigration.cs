using System.Text;
using Dapper;
using JKTankDataMigration.Extensions;
using JKTankDataMigration.Helpers;
using Lctech.JKTank.Core.Domain.Entities;
using Npgsql;

namespace JKTankDataMigration;

public class HotTagMigration
{
    private const string QUERY_HOT_TAG = """
                                         SELECT "Id","Name" FROM "HotTag""
                                         """;

    private const string QUERY_HASH_TAG = """
                                          SELECT "Id","Name" FROM "Hashtag" WHERE "Name" IN (@Names)
                                          """;

    private const string COPY_HOT_TAG_PREFIX = $"COPY \"{nameof(HotTag)}\" (\"{nameof(HotTag.Id)}\",\"{nameof(HotTag.Name)}\",\"{nameof(HotTag.SortingIndex)}\",\"{nameof(HotTag.Disabled)}\""
                                             + Setting.COPY_ENTITY_SUFFIX;

    private const string COPY_HOT_HASH_TAG_PREFIX = $"COPY \"{nameof(HotTag)}{nameof(Hashtag)}\" (\"HashtagsId\",\"HotTagsId\""
                                                  + Setting.COPY_ENTITY_SUFFIX;

    private const string HOT_TAG_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(HotTag)}";


    public void Migration()
    {
        var dateNow = DateTimeOffset.UtcNow;

        using var cn = new NpgsqlConnection(Setting.NEW_LOOK_CONNECTION);

        var existHotTags = cn.Query<(long Id, string Name)>(QUERY_HOT_TAG).ToArray();

        var defaultHotTags = GetDefaultHotTags();

        var hotTagSb = new StringBuilder();

        foreach (var defaultHotTag in defaultHotTags)
        {
            var existHotTag = existHotTags.FirstOrDefault(x => x.Name == defaultHotTag.Name);

            if (existHotTag == default)
            {
                hotTagSb.AppendValueLine(defaultHotTag.Id, defaultHotTag.Name, 2147483647, false, dateNow, 0, dateNow, 0, 0);

                continue;
            }

            defaultHotTag.Id = existHotTag.Id;
        }

        var hotHasTagNames = defaultHotTags.SelectMany(x => x.HashtagNames).Distinct().ToArray();

        var hashTagDic = cn.Query<(long Id, string Name)>(QUERY_HASH_TAG, new { Names = hotHasTagNames }).ToDictionary(x => x.Name, x => x.Id);

        var hotHashTagSb = new StringBuilder();
        
        foreach (var defaultHotTag in defaultHotTags)
        {
            var count = 0;

            foreach (var hashtagName in defaultHotTag.HashtagNames)
            {
                var hashTagId = hashTagDic.GetValueOrDefault(hashtagName);

                if (hashTagId != default)
                    hotHashTagSb.AppendValueLine(hashTagId, defaultHotTag.Id, dateNow, 0, dateNow, 0, 0);
                else
                {
                    hotHashTagSb.AppendValueLine(defaultHotTag.Id + ++ count, defaultHotTag.Id, dateNow, 0, dateNow, 0, 0);
                }
                
            }
        }

        if (hotTagSb.Length > 0)
            FileHelper.WriteToFile(HOT_TAG_PATH, $"{nameof(HotTag)}.sql", COPY_HOT_TAG_PREFIX, hotTagSb);
        
        FileHelper.WriteToFile(HOT_TAG_PATH, $"{nameof(HotTag)}{nameof(Hashtag)}.sql", COPY_HOT_HASH_TAG_PREFIX, hotHashTagSb);
    }

    private class DefautHotTag
    {
        public long Id { get; set; }
        public string Name { get; set; } = default!;
        public string[] HashtagNames { get; set; } = Array.Empty<string>();
    }

    private static DefautHotTag[] GetDefaultHotTags()
    {
        DefautHotTag[] defaultHotTags =
        {
            new()
            {
                Id = 10,
                Name = "我愛個工",
                HashtagNames = new[]
                               {
                                   "工作室",
                                   "個工",
                                   "按摩個工",
                                   "外約",
                                   "我愛個工"
                               }
            },
            new()
            {
                Id = 20,
                Name = "北部大推",
                HashtagNames = new[]
                               {
                                   "桃園",
                                   "中山區",
                                   "林森北路",
                                   "中壢",
                                   "台北",
                                   "五木",
                                   "北部大推"
                               }
            },
            new()
            {
                Id = 30,
                Name = "高顏值",
                HashtagNames = new[]
                               {
                                   "漂亮",
                                   "美女",
                                   "可愛",
                                   "高顏值"
                               }
            },
            new()
            {
                Id = 40,
                Name = "海外",
                HashtagNames = new[]
                               {
                                   "越南",
                                   "印尼",
                                   "對岸",
                                   "日本",
                                   "月亮",
                                   "海外"
                               }
            },
            new()
            {
                Id = 50,
                Name = "重口味",
                HashtagNames = new[]
                               {
                                   "美腿",
                                   "人妻",
                                   "絲襪",
                                   "淫蕩",
                                   "性感",
                                   "騷女",
                                   "重口味"
                               }
            },
            new()
            {
                Id = 60,
                Name = "巨乳",
                HashtagNames = new[]
                               {
                                   "大奶",
                                   "巨乳"
                               }
            },
            new()
            {
                Id = 70,
                Name = "技術一流",
                HashtagNames = new[]
                               {
                                   "舒壓",
                                   "按摩",
                                   "按摩舒壓",
                                   "技術一流"
                               }
            },
            new()
            {
                Id = 80,
                Name = "台中",
                HashtagNames = new[]
                               {
                                   "銀都",
                                   "台中"
                               }
            },
        };

        return defaultHotTags;
    }
}