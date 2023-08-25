TRUNCATE "User" CASCADE;
TRUNCATE "UserRole" CASCADE;
TRUNCATE "UserExtendData" CASCADE;
TRUNCATE "UserExternalLogin" CASCADE;

ALTER TABLE "UserRole"
    SET UNLOGGED;
ALTER TABLE "UserPermissionCondition"
    SET UNLOGGED;
ALTER TABLE "UserExtendData"
    SET UNLOGGED;
ALTER TABLE "UserExternalLogin"
    SET UNLOGGED;
ALTER TABLE "User"
    SET UNLOGGED;

INSERT INTO "public"."Role" ("Id", "Name", "Disabled", "CreationDate", "CreatorId", "ModificationDate", "ModifierId", "Version") VALUES
(1, '一般會員', 'f', '2023-07-31 07:59:10.84286+00', 0, '2023-07-31 07:59:10.84286+00', 0, 1),
(98, '禁言', 'f', '2023-07-31 07:59:10.84286+00', 0, '2023-07-31 07:59:10.84286+00', 0, 1),
(99, '封鎖', 'f', '2023-07-31 07:59:10.84286+00', 0, '2023-07-31 07:59:10.84286+00', 0, 1),
(100, '前台管理員', 'f', '2023-07-31 07:59:10.84286+00', 0, '2023-07-31 07:59:10.84286+00', 0, 1);