ALTER TABLE "Comment"
    SET LOGGED;
ALTER TABLE "MemberTimeline"
    SET LOGGED;

ANALYZE "Comment";

UPDATE "BlogStatistic" AS bs
SET "CommentCount" = cc.count
FROM (SELECT "BlogId", COUNT("BlogId") FROM "Comment" GROUP BY "BlogId") AS cc
WHERE bs."Id" = cc."BlogId";


