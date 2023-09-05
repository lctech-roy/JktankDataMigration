using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace JKTankDataMigration.Models;

public class UserRole : Entity
{
    public long RoleId { get; set; }
    public DateTimeOffset ExpireDate { get; set; }
}