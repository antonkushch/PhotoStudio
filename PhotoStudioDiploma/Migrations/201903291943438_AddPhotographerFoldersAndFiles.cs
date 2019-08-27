namespace PhotoStudioDiploma.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPhotographerFoldersAndFiles : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PhotographerFiles",
                c => new
                    {
                        PhotographerFileId = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Depth = c.Int(nullable: false),
                        Path = c.String(),
                        ThumbnailImage = c.Binary(),
                        PhotographerFolderId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.PhotographerFileId)
                .ForeignKey("dbo.PhotographerFolders", t => t.PhotographerFolderId, cascadeDelete: true)
                .Index(t => t.PhotographerFolderId);
            
            CreateTable(
                "dbo.PhotographerFolders",
                c => new
                    {
                        PhotographerFolderId = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Depth = c.Int(nullable: false),
                        Path = c.String(),
                        ApplicationUserId = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.PhotographerFolderId)
                .ForeignKey("dbo.AspNetUsers", t => t.ApplicationUserId)
                .Index(t => t.ApplicationUserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PhotographerFiles", "PhotographerFolderId", "dbo.PhotographerFolders");
            DropForeignKey("dbo.PhotographerFolders", "ApplicationUserId", "dbo.AspNetUsers");
            DropIndex("dbo.PhotographerFolders", new[] { "ApplicationUserId" });
            DropIndex("dbo.PhotographerFiles", new[] { "PhotographerFolderId" });
            DropTable("dbo.PhotographerFolders");
            DropTable("dbo.PhotographerFiles");
        }
    }
}
