ALTER TABLE "BlogReact"
    ADD CONSTRAINT "PK_BlogReact" PRIMARY KEY ("Id", "CreatorId"),
    ADD CONSTRAINT "FK_BlogReact_Blog_Id" FOREIGN KEY ("Id") REFERENCES "public"."Blog"("Id") ON
DELETE
CASCADE;

ALTER TABLE "BlogReact"
    SET LOGGED;

ANALYZE
"BlogReact";