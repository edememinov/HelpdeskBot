namespace Helpdesk.Website.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedconversationIDtouser : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LoggedInUsers", "ConversationCode", c => c.String());
            DropColumn("dbo.AspNetUsers", "ConversationCode");
        }
        
        public override void Down()
        {
            AddColumn("dbo.AspNetUsers", "ConversationCode", c => c.String());
            DropColumn("dbo.LoggedInUsers", "ConversationCode");
        }
    }
}
