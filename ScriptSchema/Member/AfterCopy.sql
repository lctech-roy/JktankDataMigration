ALTER TABLE "Blog"
    ADD CONSTRAINT "FK_Blog_Member_CreatorId" FOREIGN KEY ("CreatorId") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "Comment"
    ADD CONSTRAINT "FK_Comment_Member_CreatorId" FOREIGN KEY ("CreatorId") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "MemberBlogCategory"
    ADD CONSTRAINT "FK_MemberBlogCategory_Member_MemberId" FOREIGN KEY ("MemberId") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "MemberConsumptionSummary"
    ADD CONSTRAINT "FK_MemberConsumptionSummary_Member_Id" FOREIGN KEY ("Id") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "MemberFavorite"
    ADD CONSTRAINT "FK_MemberFavorite_Member_Id" FOREIGN KEY ("Id") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "MemberFollower"
    ADD CONSTRAINT "FK_MemberFollower_Member_FollowerId" FOREIGN KEY ("FollowerId") REFERENCES "public"."Member"("Id") ON DELETE CASCADE,
    ADD CONSTRAINT "FK_MemberFollower_Member_Id" FOREIGN KEY ("Id") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "MemberJPointsHistory"
    ADD CONSTRAINT "FK_MemberJPointsHistory_Member_MemberId" FOREIGN KEY ("MemberId") REFERENCES "public"."Member"("Id") ON DELETE CASCADE,
    ADD CONSTRAINT "FK_MemberJPointsHistory_Member_TradingMemberId" FOREIGN KEY ("TradingMemberId") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "MemberStatistic"
    ADD CONSTRAINT "FK_MemberStatistic_Member_Id" FOREIGN KEY ("Id") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "MemberTimeline"
    ADD CONSTRAINT "FK_MemberTimeline_Member_Id" FOREIGN KEY ("Id") REFERENCES "public"."Member"("Id") ON DELETE CASCADE,
    ADD CONSTRAINT "FK_MemberTimeline_Member_TargetMemberId" FOREIGN KEY ("TargetMemberId") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "MemberViewHistory"
    ADD CONSTRAINT "FK_MemberViewHistory_Member_Id" FOREIGN KEY ("Id") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "Order"
    ADD CONSTRAINT "FK_Order_Member_CreatorId" FOREIGN KEY ("CreatorId") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;

ALTER TABLE "Member"
    SET LOGGED;
ALTER TABLE "MemberStatistic"
    SET LOGGED;
ALTER TABLE "MemberFollower"
    SET LOGGED;
ALTER TABLE "MemberBlogCategory"
    SET LOGGED;
ALTER TABLE "MemberProfile"
    SET LOGGED;

ANALYZE "Member";
ANALYZE "MemberProfile";


