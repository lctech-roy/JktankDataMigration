ALTER TABLE "User"
    SET LOGGED;
ALTER TABLE "UserExtendData"
    SET LOGGED;
ALTER TABLE "UserExternalLogin"
    SET LOGGED;
ALTER TABLE "UserRole"
    SET LOGGED;
ALTER TABLE "UserPermissionCondition"
    SET LOGGED;

ANALYZE "User";
ANALYZE "UserExtendData";
ANALYZE "UserExternalLogin";
