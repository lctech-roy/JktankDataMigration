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
                                         SELECT "Id","Name" FROM "HotTag"
                                         """;

    private const string QUERY_HASH_TAG = """
                                          SELECT "Id","Name" FROM "Hashtag" WHERE "Name" IN
                                          """;

    private const string COPY_HASH_TAG_PREFIX = $"COPY \"{nameof(Hashtag)}\" (\"{nameof(Hashtag.Id)}\",\"{nameof(Hashtag.Name)}\",\"{nameof(Hashtag.RelationBlogCount)}\""
                                              + Setting.COPY_ENTITY_SUFFIX;

    private const string COPY_HOT_TAG_PREFIX = $"COPY \"{nameof(HotTag)}\" (\"{nameof(HotTag.Id)}\",\"{nameof(HotTag.Name)}\",\"{nameof(HotTag.SortingIndex)}\",\"{nameof(HotTag.Disabled)}\""
                                             + Setting.COPY_ENTITY_SUFFIX;

    private const string COPY_HOT_HASH_TAG_PREFIX = $"COPY \"{nameof(HotTag)}{nameof(Hashtag)}\" (\"HashtagsId\",\"HotTagsId\""
                                                  + Setting.COPY_SUFFIX;

    private const string HOT_TAG_PATH = $"{Setting.INSERT_DATA_PATH}/{nameof(HotTag)}";


    public void Migration()
    {
        var dateNow = DateTimeOffset.UtcNow;

        using var cn = new NpgsqlConnection(Setting.TANK_CONNECTION);

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

        var hashTagSql = QUERY_HASH_TAG + $" ({string.Join(",", hotHasTagNames.Select(x => $"'{x}'"))})";

        var hashTagDic = cn.Query<(long Id, string Name)>(hashTagSql).ToDictionary(x => x.Name, x => x.Id);

        var hotHashTagSb = new StringBuilder();
        var hashTagSb = new StringBuilder();

        foreach (var defaultHotTag in defaultHotTags)
        {
            var count = 0;

            foreach (var hashtagName in defaultHotTag.HashtagNames)
            {
                var hashTagId = hashTagDic.GetValueOrDefault(hashtagName);

                if (hashTagId != default)
                    hotHashTagSb.AppendValueLine(hashTagId, defaultHotTag.Id);
                else
                {
                    var newHashTagId = defaultHotTag.Id + ++count;

                    hashTagSb.AppendValueLine(newHashTagId, hashtagName, 0, dateNow, 0, dateNow, 0, 0);
                    hotHashTagSb.AppendValueLine(newHashTagId, defaultHotTag.Id);
                }
            }
        }

        if (hotTagSb.Length > 0)
            FileHelper.WriteToFile(HOT_TAG_PATH, $"{nameof(HotTag)}.sql", COPY_HOT_TAG_PREFIX, hotTagSb);

        if (hashTagSb.Length > 0)
            FileHelper.WriteToFile(HOT_TAG_PATH, $"{nameof(Hashtag)}.sql", COPY_HASH_TAG_PREFIX, hashTagSb);

        FileHelper.WriteToFile(HOT_TAG_PATH, $"{nameof(HotTag)}{nameof(Hashtag)}.sql", COPY_HOT_HASH_TAG_PREFIX, hotHashTagSb);
    }

    private class DefaultHotTag
    {
        public long Id { get; set; }
        public string Name { get; set; } = default!;
        public string[] HashtagNames { get; set; } = Array.Empty<string>();
    }

    private static DefaultHotTag[] GetDefaultHotTags()
    {
        DefaultHotTag[] defaultHotTags =
        {
            new()
            {
                Id = 403565045350400,
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
                Id = 403565087293441,
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
                Id = 403565120847874,
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
                Id = 403565150208002,
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
                Id = 403565179568130,
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
                Id = 403565204733955,
                Name = "巨乳",
                HashtagNames = new[]
                               {
                                   "大奶",
                                   "巨乳"
                               }
            },
            new()
            {
                Id = 403565234094083,
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
                Id = 403565263454212,
                Name = "台中",
                HashtagNames = new[]
                               {
                                   "銀都",
                                   "台中"
                               }
            }
        };

        return defaultHotTags;
    }
}