namespace PhotoStudioDiploma.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddGrantedFolders : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.GrantedUserFolders",
                c => new
                    {
                        ApplicationUserRefId = c.String(nullable: false, maxLength: 128),
                        PhotographerFolderRefId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.ApplicationUserRefId, t.PhotographerFolderRefId })
                .ForeignKey("dbo.AspNetUsers", t => t.ApplicationUserRefId, cascadeDelete: true)
                .ForeignKey("dbo.PhotographerFolders", t => t.PhotographerFolderRefId, cascadeDelete: true)
                .Index(t => t.ApplicationUserRefId)
                .Index(t => t.PhotographerFolderRefId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.GrantedUserFolders", "PhotographerFolderRefId", "dbo.PhotographerFolders");
            DropForeignKey("dbo.GrantedUserFolders", "ApplicationUserRefId", "dbo.AspNetUsers");
            DropIndex("dbo.GrantedUserFolders", new[] { "PhotographerFolderRefId" });
            DropIndex("dbo.GrantedUserFolders", new[] { "ApplicationUserRefId" });
            DropTable("dbo.GrantedUserFolders");
        }
    }
}
