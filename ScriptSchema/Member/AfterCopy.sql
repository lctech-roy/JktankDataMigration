ALTER TABLE "Member"
    SET LOGGED;
ALTER TABLE "MemberStatistic"
    SET LOGGED;
ALTER TABLE "MemberRelation"
    SET LOGGED;
ALTER TABLE "MemberProfile"
    SET LOGGED;
ALTER TABLE "MemberTopic"
    SET LOGGED;
ALTER TABLE "MemberTimeline"
    SET LOGGED;

ALTER TABLE "MemberProfile"
    ADD CONSTRAINT "FK_MemberProfile_Member_Id" FOREIGN KEY ("Id") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "Blog"
    ADD CONSTRAINT "FK_Blog_Member_CreatorId" FOREIGN KEY ("CreatorId") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "Comment"
    ADD CONSTRAINT "FK_Comment_Member_CreatorId" FOREIGN KEY ("CreatorId") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "MemberBlogCategory"
    ADD CONSTRAINT "FK_MemberBlogCategory_Member_MemberId" FOREIGN KEY ("MemberId") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "MemberCover"
    ADD CONSTRAINT "FK_MemberCover_Member_Id" FOREIGN KEY ("Id") REFERENCES "public"."Member"("Id");
ALTER TABLE "MemberFavorite"
    ADD CONSTRAINT "FK_MemberFavorite_Member_Id" FOREIGN KEY ("Id") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "MemberRelation"
    ADD CONSTRAINT "FK_MemberRelation_Member_RelatedMemberId" FOREIGN KEY ("RelatedMemberId") REFERENCES "public"."Member"("Id") ON DELETE CASCADE,
    ADD CONSTRAINT "FK_MemberRelation_Member_Id" FOREIGN KEY ("Id") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "MemberJPointsHistory"
    ADD CONSTRAINT "FK_MemberJPointsHistory_Member_MemberId" FOREIGN KEY ("MemberId") REFERENCES "public"."Member"("Id") ON DELETE CASCADE,
    ADD CONSTRAINT "FK_MemberJPointsHistory_Member_TradingMemberId" FOREIGN KEY ("TradingMemberId") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "MemberStatistic"
    ADD CONSTRAINT "FK_MemberStatistic_Member_Id" FOREIGN KEY ("Id") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "MemberTimeline"
    ADD CONSTRAINT "FK_MemberTimeline_Member_CreatorId" FOREIGN KEY ("CreatorId") REFERENCES "public"."Member"("Id") ON DELETE CASCADE
ALTER TABLE "MemberTopic"
    ADD CONSTRAINT "FK_MemberTopic_Member_Id" FOREIGN KEY ("Id") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "MemberViewHistory"
    ADD CONSTRAINT "FK_MemberViewHistory_Member_Id" FOREIGN KEY ("Id") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;
ALTER TABLE "Order"
    ADD CONSTRAINT "FK_Order_Member_CreatorId" FOREIGN KEY ("CreatorId") REFERENCES "public"."Member"("Id") ON DELETE CASCADE;

ANALYZE "Member";
ANALYZE "MemberProfile";


