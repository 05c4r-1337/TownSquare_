using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TownSquare.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [Users] (
                        [Id] int NOT NULL IDENTITY,
                        [Username] nvarchar(100) NOT NULL,
                        [Password] nvarchar(255) NOT NULL,
                        [Role] nvarchar(50) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
