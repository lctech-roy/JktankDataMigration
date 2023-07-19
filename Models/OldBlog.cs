using Lctech.JLook.Core.Domain.Entities;

namespace JLookDataMigration.Models;

public class OldBlog : Blog
{
    public bool IsReview { get; set; }
    public int OldVisibleType { get; set; }
    public string OldContent { get; set; } = default!;
    public string OldCover { get; set; } = default!;
    public string OldTags { get; set; } = default!;
    public int ViewCount { get; set; }
    public int FavoriteCount { get; set; }
    public int CommentCount { get; set; }
    public int ComeByReactCount { get; set; }
    public int AmazingReactCount { get; set; }
    public int ShakeHandsReactCount { get; set; }
    public int FlowerReactCount { get; set; }
    public int ConfuseReactCount { get; set; }
    public long Uid { get; set; }
    public uint DateLine { get; set; }
}