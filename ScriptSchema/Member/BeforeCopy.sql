ALTER TABLE "MemberProfile"
DROP
CONSTRAINT IF EXISTS "FK_MemberProfile_Member_Id";
ALTER TABLE "Blog"
DROP
CONSTRAINT IF EXISTS "FK_Blog_Member_CreatorId";
ALTER TABLE "Comment"
DROP
CONSTRAINT IF EXISTS "FK_Comment_Member_CreatorId";
ALTER TABLE "MemberBlogCategory"
DROP
CONSTRAINT IF EXISTS "FK_MemberBlogCategory_Member_MemberId";
ALTER TABLE "MemberCover"
DROP
CONSTRAINT IF EXISTS "FK_MemberCover_Member_Id";
ALTER TABLE "MemberFavorite"
DROP
CONSTRAINT IF EXISTS "FK_MemberFavorite_Member_Id";
ALTER TABLE "MemberRelation"
DROP
CONSTRAINT IF EXISTS "FK_MemberRelation_Member_RelatedMemberId",
    DROP
CONSTRAINT IF EXISTS "FK_MemberRelation_Member_Id";
ALTER TABLE "MemberJPointsHistory"
DROP
CONSTRAINT IF EXISTS "FK_MemberJPointsHistory_Member_MemberId",
    DROP
CONSTRAINT IF EXISTS "FK_MemberJPointsHistory_Member_TradingMemberId";
ALTER TABLE "MemberStatistic"
DROP
CONSTRAINT IF EXISTS "FK_MemberStatistic_Member_Id";
ALTER TABLE "MemberTimeline"
DROP
CONSTRAINT IF EXISTS "FK_MemberTimeline_Member_CreatorId";
ALTER TABLE "MemberTopic"
DROP
CONSTRAINT IF EXISTS "FK_MemberTopic_Member_Id";
ALTER TABLE "MemberViewHistory"
DROP
CONSTRAINT IF EXISTS "FK_MemberViewHistory_Member_Id";
ALTER TABLE "Order"
DROP
CONSTRAINT IF EXISTS "FK_Order_Member_CreatorId";

TRUNCATE "Member";
TRUNCATE "MemberProfile";

ALTER TABLE "MemberStatistic"
    SET UNLOGGED;
ALTER TABLE "MemberRelation"
    SET UNLOGGED;
ALTER TABLE "MemberProfile"
    SET UNLOGGED;
ALTER TABLE "MemberTopic"
    SET UNLOGGED;
ALTER TABLE "MemberTimeline"
    SET UNLOGGED;
ALTER TABLE "Member"
    SET UNLOGGED;