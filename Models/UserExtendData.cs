using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace JLookDataMigration.Models;

public class UserExtendData : Entity
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
}