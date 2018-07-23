namespace Helpdesk.Website.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Addedloggedinuser : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.LoggedInUsers",
                c => new
                    {
                        LoggedInUserID = c.Int(nullable: false, identity: true),
                        LoggedInUserEmail = c.String(),
                    })
                .PrimaryKey(t => t.LoggedInUserID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.LoggedInUsers");
        }
    }
}
