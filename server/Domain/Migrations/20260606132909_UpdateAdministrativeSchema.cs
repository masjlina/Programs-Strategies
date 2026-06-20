using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domain.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdministrativeSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Strategies_AdministrativeUnits_AdministrativeUnitId",
                table: "Strategies");

            migrationBuilder.DropTable(
                name: "AdministrativeUnits");

            migrationBuilder.DropIndex(
                name: "IX_Strategies_AdministrativeUnitId",
                table: "Strategies");

            migrationBuilder.DropColumn(
                name: "AdministrativeUnitId",
                table: "Strategies");

            migrationBuilder.AddColumn<Guid>(
                name: "CommunityId",
                table: "Strategies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DistrictId",
                table: "Strategies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RegionId",
                table: "Strategies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrategyUrl",
                table: "Strategies",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Regions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    WebsiteUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    StrategiesUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    KattotgId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NameFull = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ImgUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Districts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    RegionId = table.Column<Guid>(type: "uuid", nullable: false),
                    KattotgId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NameFull = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ImgUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Districts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Districts_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Communities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    RegionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DistrictId = table.Column<Guid>(type: "uuid", nullable: false),
                    WebsiteUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    StrategiesUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    KattotgId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NameFull = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ImgUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Communities_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Communities_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Settlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    KattotgId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RegionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommunityId = table.Column<Guid>(type: "uuid", nullable: true),
                    DistrictId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentSettlementId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NameFull = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ParentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settlements_Communities_CommunityId",
                        column: x => x.CommunityId,
                        principalTable: "Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Settlements_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Settlements_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Settlements_Settlements_ParentSettlementId",
                        column: x => x.ParentSettlementId,
                        principalTable: "Settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Strategies_CommunityId",
                table: "Strategies",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_Strategies_DistrictId",
                table: "Strategies",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Strategies_RegionId",
                table: "Strategies",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Communities_DistrictId",
                table: "Communities",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Communities_KattotgId",
                table: "Communities",
                column: "KattotgId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Communities_RegionId",
                table: "Communities",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Districts_KattotgId",
                table: "Districts",
                column: "KattotgId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Districts_RegionId",
                table: "Districts",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Regions_KattotgId",
                table: "Regions",
                column: "KattotgId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_CommunityId",
                table: "Settlements",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_DistrictId",
                table: "Settlements",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_KattotgId",
                table: "Settlements",
                column: "KattotgId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_ParentSettlementId",
                table: "Settlements",
                column: "ParentSettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_RegionId",
                table: "Settlements",
                column: "RegionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Strategies_Communities_CommunityId",
                table: "Strategies",
                column: "CommunityId",
                principalTable: "Communities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Strategies_Districts_DistrictId",
                table: "Strategies",
                column: "DistrictId",
                principalTable: "Districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Strategies_Regions_RegionId",
                table: "Strategies",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Strategies_Communities_CommunityId",
                table: "Strategies");

            migrationBuilder.DropForeignKey(
                name: "FK_Strategies_Districts_DistrictId",
                table: "Strategies");

            migrationBuilder.DropForeignKey(
                name: "FK_Strategies_Regions_RegionId",
                table: "Strategies");

            migrationBuilder.DropTable(
                name: "Settlements");

            migrationBuilder.DropTable(
                name: "Communities");

            migrationBuilder.DropTable(
                name: "Districts");

            migrationBuilder.DropTable(
                name: "Regions");

            migrationBuilder.DropIndex(
                name: "IX_Strategies_CommunityId",
                table: "Strategies");

            migrationBuilder.DropIndex(
                name: "IX_Strategies_DistrictId",
                table: "Strategies");

            migrationBuilder.DropIndex(
                name: "IX_Strategies_RegionId",
                table: "Strategies");

            migrationBuilder.DropColumn(
                name: "CommunityId",
                table: "Strategies");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "Strategies");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "Strategies");

            migrationBuilder.DropColumn(
                name: "StrategyUrl",
                table: "Strategies");

            migrationBuilder.AddColumn<Guid>(
                name: "AdministrativeUnitId",
                table: "Strategies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "AdministrativeUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdministrativeUnits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Strategies_AdministrativeUnitId",
                table: "Strategies",
                column: "AdministrativeUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Strategies_AdministrativeUnits_AdministrativeUnitId",
                table: "Strategies",
                column: "AdministrativeUnitId",
                principalTable: "AdministrativeUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
