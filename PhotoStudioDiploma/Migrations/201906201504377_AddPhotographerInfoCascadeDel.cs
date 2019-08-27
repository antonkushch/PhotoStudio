namespace PhotoStudioDiploma.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPhotographerInfoCascadeDel : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.PhotographerInfoes", "ApplicationUserId", "dbo.AspNetUsers");
            AddForeignKey("dbo.PhotographerInfoes", "ApplicationUserId", "dbo.AspNetUsers", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PhotographerInfoes", "ApplicationUserId", "dbo.AspNetUsers");
            AddForeignKey("dbo.PhotographerInfoes", "ApplicationUserId", "dbo.AspNetUsers", "Id");
        }
    }
}
