using System.Text.RegularExpressions;

namespace JKTankDataMigration.Helpers;

public class RegexHelper
{
    public const string URL_GROUP = "url";
    public const string PATH_GROUP = "path";
    public const string WIDTH_GROUP = "width";
    public const string HEIGHT_GROUP = "height";
    public const string SIZE_GROUP = "size";
    private const string FACE_GROUP = "face";
    public const string TID_GROUP = "tid";

    public const string AUTHOR_GROUP = "author";
    public const string AUTHOR_CONTENT_GROUP = "authorContent";
    public const string REPLIER_CONTENT_GROUP = "replierContent";
    
    private const string IMG_SMILEY_PATTERN = @"<img src=[""'](?:https?://www.(?:jkf\.io|jkforum\.net)/)?(?:static/image/smiley/comcom|image/face)/(\d+)\.gif[""'].*?>";
    private const string IMG_SRC_URL_PATTERN = $@"<img.+?src=""(?<{URL_GROUP}>.*)"">";
    private const string IMG_SRC_PATTERN = @"<img.+?src=[""']https?:.+?[""'].*?>";
    private const string IMG_ATTR_PATTERN = $@"src=[""'](?<{PATH_GROUP}>https?:.*?)[""']|width=[""'](?<{WIDTH_GROUP}>\d*?)[""']|height=[""'](?<{HEIGHT_GROUP}>\d*?)[""']";
    private const string EMBED_SRC_PATTERN = @"<embed.+?src=[""'](https?:.+?)[""'].*?>";
    private const string FONT_SIZE_PATTERN = $@"(<font.+?size=[""'])(?<{SIZE_GROUP}>\d+)([""'].*?>)";
    private const string FONT_FACE_PATTERN = $@"(<font.+?)(?<{FACE_GROUP}>face=[""'].+?[""'])(.*?>)";
    private const string COMMENT_REPLY_PATTERN = $@"<div class=""quote""><span class=""q""><b>(?<{AUTHOR_GROUP}>.*)</b>: (?<{AUTHOR_CONTENT_GROUP}>[\S\s]*)</span></div>(?<{REPLIER_CONTENT_GROUP}>[\S\s]*)";
    
    // include href
    // private const string MASSAGE_URL_PATTERN = $@"(<a\b|(?!^)\G)[^>]*?\bhref=([""']?)(https?://www\.jkforum\.net/(thread-(?<{TID_GROUP}>\d+)-\d+-\d+\.html|forum\.php\?mod=(misc|post|viewthread)\S*&tid=(?<{TID_GROUP}>\d+)|group\/\d+\?action=preview&tid=(?<{TID_GROUP}>\d+)))([""']?)\2";
    private const string MASSAGE_URL_PATTERN = $@"https?://www\.(jkf\.io|jkforum\.net)/(thread-(?<{TID_GROUP}>\d+)-\d+-\d+\.html|forum\.php\?mod=(misc|post|viewthread)\S*&tid=(?<{TID_GROUP}>\d+)|group\/\d+\?action=preview&tid=(?<{TID_GROUP}>\d+))";
    
    public static readonly Regex ImgSmileyRegex = new(IMG_SMILEY_PATTERN, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public static readonly Regex ImgSrcUrlPattern = new(IMG_SRC_URL_PATTERN, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public static readonly Regex ImgSrcRegex = new(IMG_SRC_PATTERN, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public static readonly Regex ImgAttrRegex = new(IMG_ATTR_PATTERN, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public static readonly Regex EmbedRegex = new(EMBED_SRC_PATTERN, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public static readonly Regex FontSizeRegex = new(FONT_SIZE_PATTERN, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public static readonly Regex FontFaceRegex = new(FONT_FACE_PATTERN, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public static readonly Regex MassageUrlRegex = new(MASSAGE_URL_PATTERN, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public static readonly Regex CommentReplyRegex = new(COMMENT_REPLY_PATTERN, RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static readonly Dictionary<int, string> FontSizeDic = new()
                                                                 {
                                                                     { 0, "0.8em" },
                                                                     { 1, "0.8em" },
                                                                     { 2, "0.8em" },
                                                                     { 3, "1em" },
                                                                     { 4, "1.33em" },
                                                                     { 5, "1.87em" },
                                                                 };
}