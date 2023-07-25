TRUNCATE "Attachment" CASCADE;
TRUNCATE "AttachmentExtendData" CASCADE;

ALTER TABLE "AttachmentExtendData" SET UNLOGGED;
ALTER TABLE "Attachment" SET UNLOGGED;
