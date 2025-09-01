using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetworkingBot.Migrations
{
    /// <inheritdoc />
    public partial class AddInterest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT into "Topics" ("Name") 
                VALUES
                     ('DotNet'),               
                     ('PostgresSql'),               
                     ('Async');                 
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
