ALTER TABLE "Blog"
    DROP CONSTRAINT IF EXISTS "FK_Blog_Member_CreatorId";
ALTER TABLE "Comment"
    DROP CONSTRAINT IF EXISTS "FK_Comment_Member_CreatorId";
ALTER TABLE "MemberBlogCategory"
    DROP CONSTRAINT IF EXISTS "FK_MemberBlogCategory_Member_MemberId";
ALTER TABLE "MemberConsumptionSummary"
    DROP CONSTRAINT IF EXISTS "FK_MemberConsumptionSummary_Member_Id";
ALTER TABLE "MemberFavorite"
    DROP CONSTRAINT IF EXISTS "FK_MemberFavorite_Member_Id";
ALTER TABLE "MemberFollower"
    DROP CONSTRAINT IF EXISTS "FK_MemberFollower_Member_FollowerId",
    DROP CONSTRAINT IF EXISTS "FK_MemberFollower_Member_Id";
ALTER TABLE "MemberJPointsHistory"
    DROP CONSTRAINT IF EXISTS "FK_MemberJPointsHistory_Member_MemberId",
    DROP CONSTRAINT IF EXISTS "FK_MemberJPointsHistory_Member_TradingMemberId";
ALTER TABLE "MemberStatistic"
    DROP CONSTRAINT IF EXISTS "FK_MemberStatistic_Member_Id";
ALTER TABLE "MemberTimeline"
    DROP CONSTRAINT IF EXISTS "FK_MemberTimeline_Member_Id",
    DROP CONSTRAINT IF EXISTS "FK_MemberTimeline_Member_TargetMemberId";
ALTER TABLE "MemberViewHistory"
    DROP CONSTRAINT IF EXISTS "FK_MemberViewHistory_Member_Id";
ALTER TABLE "Order"
    DROP CONSTRAINT IF EXISTS "FK_Order_Member_CreatorId";

TRUNCATE "Member" CASCADE;
TRUNCATE "MemberProfile" CASCADE;

ALTER TABLE "MemberStatistic"
    SET UNLOGGED;
ALTER TABLE "MemberFollower"
    SET UNLOGGED;
ALTER TABLE "MemberBlogCategory"
    SET UNLOGGED;
ALTER TABLE "MemberProfile"
    SET UNLOGGED;
ALTER TABLE "Member"
    SET UNLOGGED;