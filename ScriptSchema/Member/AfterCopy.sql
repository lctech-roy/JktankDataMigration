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


