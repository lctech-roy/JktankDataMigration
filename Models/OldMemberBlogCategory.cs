using Lctech.JKTank.Core.Domain.Entities;

namespace JKTankDataMigration.Models;

public class OldMemberBlogCategory : MemberBlogCategory
{
    public uint Dateline { get; set; }
}