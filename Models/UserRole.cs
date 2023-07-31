using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace JLookDataMigration.Models;

public class UserRole : Entity
{
    public long RoleId { get; set; }
    public DateTimeOffset ExpireDate { get; set; }
}