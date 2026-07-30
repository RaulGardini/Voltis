using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Voltis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CriarTabelaConfiguracaoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configuracao_usuario",
                columns: table => new
                {
                    configuracao_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dia_fechamento_mes = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    moeda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "BRL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracao_usuario", x => x.configuracao_id);
                    table.ForeignKey(
                        name: "fk_configuracao_usuario",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_configuracao_usuario_usuario_id",
                table: "configuracao_usuario",
                column: "usuario_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuracao_usuario");
        }
    }
}
