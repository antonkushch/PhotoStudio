namespace PhotoStudioDiploma.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddClientInfoCascadeDelete : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ClientInfoes", "ApplicationUserId", "dbo.AspNetUsers");
            AddForeignKey("dbo.ClientInfoes", "ApplicationUserId", "dbo.AspNetUsers", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ClientInfoes", "ApplicationUserId", "dbo.AspNetUsers");
            AddForeignKey("dbo.ClientInfoes", "ApplicationUserId", "dbo.AspNetUsers", "Id");
        }
    }
}
