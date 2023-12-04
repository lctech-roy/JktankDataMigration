ALTER TABLE "BlogReact"
    DROP CONSTRAINT IF EXISTS "FK_BlogReact_Blog_Id";

ALTER TABLE "Comment"
    DROP CONSTRAINT IF EXISTS "FK_Comment_Blog_BlogId";

ALTER TABLE "HotTagHashtag"
    DROP CONSTRAINT IF EXISTS "FK_HotTagHashtag_Hashtag_HashtagsId";

TRUNCATE "Blog" CASCADE;
TRUNCATE "MassageBlog" CASCADE;
TRUNCATE "Hashtag" CASCADE;

ALTER TABLE "MemberJPointsHistory" SET UNLOGGED;
ALTER TABLE "Order" SET UNLOGGED;
ALTER TABLE "MemberViewHistory" SET UNLOGGED;
ALTER TABLE "MemberFavorite" SET UNLOGGED;
ALTER TABLE "MemberTimeline" SET UNLOGGED;
ALTER TABLE "BlogMedia" SET UNLOGGED;
ALTER TABLE "BlogSpecialTag" SET UNLOGGED;
ALTER TABLE "BlogStatistic" SET UNLOGGED;
ALTER TABLE "BlogReact" SET UNLOGGED;
ALTER TABLE "CommentLike" SET UNLOGGED;
ALTER TABLE "Comment" SET UNLOGGED;
ALTER TABLE "Blog" SET UNLOGGED;

