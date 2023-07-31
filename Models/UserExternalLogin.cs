using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace JLookDataMigration.Models;

public class UserExternalLogin : Entity
{
    public string Provider { get; set; } = null!;
    public string UniqueId { get; set; } = null!;
}