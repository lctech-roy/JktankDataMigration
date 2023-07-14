using Lctech.JLook.Core.Domain.Entities;

namespace JLookDataMigration.Models;

public class OldMemberBlogCategory : MemberBlogCategory
{
    public uint Dateline { get; set; }
}