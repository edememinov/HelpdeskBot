namespace Helpdesk.Website.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovedLogedInBotinuser : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.AspNetUsers", "LoggedInBot");
        }
        
        public override void Down()
        {
            AddColumn("dbo.AspNetUsers", "LoggedInBot", c => c.Boolean());
        }
    }
}
