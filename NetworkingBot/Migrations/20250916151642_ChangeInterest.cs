using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetworkingBot.Migrations
{
    /// <inheritdoc />
    public partial class ChangeInterest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "Topics";
                INSERT into "Topics" ("Name") 
                VALUES
                     ('Best practices'),               
                     ('Расширяем горизонты'),               
                     ('Performance + Internals'),               
                     ('Architecture'),               
                     ('Без темы');           
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
