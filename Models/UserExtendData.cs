using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace JKTankDataMigration.Models;

public class UserExtendData : Entity
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
}