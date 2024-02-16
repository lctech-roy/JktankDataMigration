ALTER TABLE "Comment"
    SET LOGGED;
ALTER TABLE "MemberTimeline"
    SET LOGGED;

ANALYZE "Comment";

UPDATE "BlogStatistic" AS bs
SET "CommentCount" = cc.count
FROM (SELECT "BlogId", COUNT("BlogId") FROM "Comment" GROUP BY "BlogId") AS cc
WHERE bs."Id" = cc."BlogId";

INSERT INTO "BlogReview" ("Id","BlogReviewStatus","LastBlogReviewDate","TextReviewStatus","CoverReviewStatus",
                          "ImageReviewStatus","CommentReviewStatus", "LastCommentReviewDate",
                          "CreationDate","CreatorId","ModificationDate","ModifierId","Version")
SELECT
    b."Id",
    CASE WHEN b."Status"[1] = 3 THEN 3 ELSE 2 END,
    b."CreationDate",
    CASE WHEN b."Status"[1] = 3 THEN 3 ELSE 2 END,
    CASE WHEN b."Status"[1] = 3 THEN 3 ELSE 2 END,
    CASE WHEN b."Status"[1] = 3 THEN 3 ELSE 2 END,
    CASE WHEN bs."CommentCount" = 0 THEN 0 ELSE 2 END,
    CASE WHEN bs."CommentCount" = 0 THEN NULL ELSE now() END,
    b."CreationDate",
    b."CreatorId",
    now(),
    b."CreatorId",
    0
FROM "Blog" b
INNER JOIN "BlogStatistic" bs ON b."Id" = bs."Id"
