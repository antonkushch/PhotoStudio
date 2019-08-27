namespace PhotoStudioDiploma.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class NewMigration : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ClientInfoes",
                c => new
                    {
                        ApplicationUserId = c.String(nullable: false, maxLength: 128),
                        ClientInteger = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ApplicationUserId)
                .ForeignKey("dbo.AspNetUsers", t => t.ApplicationUserId)
                .Index(t => t.ApplicationUserId);
            
            CreateTable(
                "dbo.PhotographerInfoes",
                c => new
                    {
                        ApplicationUserId = c.String(nullable: false, maxLength: 128),
                        DropboxAccessToken = c.String(),
                        ConnectState = c.String(),
                    })
                .PrimaryKey(t => t.ApplicationUserId)
                .ForeignKey("dbo.AspNetUsers", t => t.ApplicationUserId)
                .Index(t => t.ApplicationUserId);
            
            AddColumn("dbo.AspNetUsers", "Name", c => c.String());
            AddColumn("dbo.AspNetUsers", "Surname", c => c.String());
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PhotographerInfoes", "ApplicationUserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.ClientInfoes", "ApplicationUserId", "dbo.AspNetUsers");
            DropIndex("dbo.PhotographerInfoes", new[] { "ApplicationUserId" });
            DropIndex("dbo.ClientInfoes", new[] { "ApplicationUserId" });
            DropColumn("dbo.AspNetUsers", "Surname");
            DropColumn("dbo.AspNetUsers", "Name");
            DropTable("dbo.PhotographerInfoes");
            DropTable("dbo.ClientInfoes");
        }
    }
}
