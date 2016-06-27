namespace Draftkings.Ownership.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class klo : DbMigration
    {
        public override void Up()
        {
            
            CreateTable(
                "dbo.LoginInfoes",
                c => new
                    {
                        Id = c.Guid(nullable: false, identity: true),
                        Username = c.String(),
                        Password = c.String(),
                        Created = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            
            
        }
        
        public override void Down()
        {

        }
    }
}
