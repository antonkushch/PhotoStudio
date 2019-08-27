namespace PhotoStudioDiploma.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ChangedFileSizeTypeToLong : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PhotographerFiles", "Size", c => c.Long(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PhotographerFiles", "Size");
        }
    }
}
