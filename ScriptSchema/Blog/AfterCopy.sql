ALTER TABLE "Blog"
    SET LOGGED;
ALTER TABLE "Comment"
    SET LOGGED;
ALTER TABLE "CommentLike"
    SET UNLOGGED;
ALTER TABLE "BlogReact"
    SET UNLOGGED;
ALTER TABLE "BlogStatistic"
    SET LOGGED;
ALTER TABLE "BlogSpecialTag"
    SET LOGGED;
ALTER TABLE "BlogMedia"
    SET LOGGED;
ALTER TABLE "MemberTimeline"
    SET LOGGED;
ALTER TABLE "MemberFavorite"
    SET LOGGED;
ALTER TABLE "MemberViewHistory"
    SET LOGGED;
ALTER TABLE "Order"
    SET LOGGED;
ALTER TABLE "MemberJPointsHistory"
    SET LOGGED;

ALTER TABLE "BlogReact"
    ADD CONSTRAINT "FK_BlogReact_Blog_Id" FOREIGN KEY ("Id") REFERENCES "public"."Blog" ("Id") ON DELETE CASCADE;

ALTER TABLE "Comment"
    ADD CONSTRAINT "FK_Comment_Blog_BlogId" FOREIGN KEY ("BlogId") REFERENCES "public"."Blog" ("Id") ON DELETE CASCADE;

ALTER TABLE "HotTagHashtag"
    ADD CONSTRAINT "FK_HotTagHashtag_Hashtag_HashtagsId" FOREIGN KEY ("HashtagsId") REFERENCES "public"."Hashtag" ("Id") ON DELETE CASCADE;