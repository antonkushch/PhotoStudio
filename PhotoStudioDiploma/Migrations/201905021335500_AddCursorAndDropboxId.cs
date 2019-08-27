namespace PhotoStudioDiploma.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCursorAndDropboxId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PhotographerFiles", "DropboxFileId", c => c.String());
            AddColumn("dbo.PhotographerFolders", "DropboxCursor", c => c.String());
            AddColumn("dbo.PhotographerFolders", "DropboxFolderId", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.PhotographerFolders", "DropboxFolderId");
            DropColumn("dbo.PhotographerFolders", "DropboxCursor");
            DropColumn("dbo.PhotographerFiles", "DropboxFileId");
        }
    }
}
