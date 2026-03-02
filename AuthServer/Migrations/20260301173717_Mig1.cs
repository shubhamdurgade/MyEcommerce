using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class Mig1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientApps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ClientSecretHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientApps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientAppId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LoginLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastSeenUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    RevokedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_ClientApps_ClientAppId",
                        column: x => x.ClientAppId,
                        principalTable: "ClientApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReplaceByTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ExpiresUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_RefreshTokens_ParentTokenId",
                        column: x => x.ParentTokenId,
                        principalTable: "RefreshTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_RefreshTokens_ReplaceByTokenId",
                        column: x => x.ReplaceByTokenId,
                        principalTable: "RefreshTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_UserSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "UserSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ClientApps",
                columns: new[] { "Id", "ClientId", "ClientSecretHash", "CreatedUtc", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "QaFKlLnRkjht0cnkmVYbw", "$2a$11$ua9Mhe.rPlAGvh7jH5bRguRVuIo8Ewqfofmss17u9jxbZPP34wFXW", new DateTime(2026, 3, 1, 17, 37, 16, 55, DateTimeKind.Utc).AddTicks(5217), true, "E-Commerce Web Client" },
                    { new Guid("11111111-1111-1111-1111-111111111112"), "u5JH6gdhmAB6VBqBprfDHQ", "$2a$11$581VAtEU5CrS/Xt0BDFQ6OkEXAFzDxDoSlme3sY08U85r.OB.Wgjy", new DateTime(2026, 3, 1, 17, 37, 16, 222, DateTimeKind.Utc).AddTicks(6255), true, "E-Commerce Android Client" },
                    { new Guid("11111111-1111-1111-1111-111111111113"), "8dj5ti3zXBUjpgQ_mH-bGA", "$2a$11$1TdaxnUqnYsgIasdZqS5/eTzjY50X2gbftjY9Sm7yODiyeUXEA9ZO", new DateTime(2026, 3, 1, 17, 37, 16, 388, DateTimeKind.Utc).AddTicks(9125), true, "E-Commerce iOS Client" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Description", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222221"), "Full system access. Manage users, roles, catalog, orders and plafroem setting.", true, "Admin" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "End user who browses products, places orders, makes payments, and manages their profile.", true, "Customer" },
                    { new Guid("22222222-2222-2222-2222-222222222223"), "Merchant who lists products, manages inventory/pricing, and fulfills customer order.", true, "Seller" },
                    { new Guid("22222222-2222-2222-2222-222222222224"), "Delivery agaent responsible for pickup, shipment tracking, and last-mile delivery updates.", true, "DeliveryPartner" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedUtc", "Email", "FirstName", "IsActive", "LastName", "PasswordHash", "PhoneNumber" },
                values: new object[,]
                {
                    { new Guid("22222222-bbbb-bbbb-bbbb-222222222222"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "customer@ecommerce.com", "Demo", true, "Customer", "$2a$11$Aq8dWGslPMFYybvv6JgO2OOTfgVBxagIaj/JuhDGf/GtYOltqYvg6", "8888888888" },
                    { new Guid("33333333-cccc-cccc-cccc-333333333333"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seller@ecommerce.com", "Demo", true, "Seller", "$2a$11$x6a3VMzdFImQ3ZKE01dkHesNvVTwWVl75nzrMtssceKhnjnxt2MFa", "7777777777" },
                    { new Guid("44444444-dddd-dddd-dddd-444444444444"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "delivery@ecommerce.com", "Demo", true, "Rider", "$2a$11$8jXB5nLn3qNuV.WKs5480.779PKRHqgg6S7fw8A.oCLy6.EyhZGx2", "6666666666" },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@ecommerce.com", "System", true, "Admin", "$2a$11$lSA2QnkivSQC8R9F1ZeZOes6fh4XwX8W8nZ4Zm/AVikODCY5MzDr2", "999999999" }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "AssignedByUserId", "AssignedUtc", "Notes", "RoleId", "UserId" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seeded admin", new Guid("22222222-2222-2222-2222-222222222221"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
                    { new Guid("10000000-0000-0000-0000-000000000002"), new Guid("22222222-bbbb-bbbb-bbbb-222222222222"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seeded customer", new Guid("22222222-2222-2222-2222-222222222222"), new Guid("22222222-bbbb-bbbb-bbbb-222222222222") },
                    { new Guid("10000000-0000-0000-0000-000000000003"), new Guid("33333333-cccc-cccc-cccc-333333333333"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seeded seller", new Guid("22222222-2222-2222-2222-222222222223"), new Guid("33333333-cccc-cccc-cccc-333333333333") },
                    { new Guid("10000000-0000-0000-0000-000000000004"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seeded admin", new Guid("22222222-2222-2222-2222-222222222224"), new Guid("44444444-dddd-dddd-dddd-444444444444") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientApps_ClientId",
                table: "ClientApps",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ParentTokenId",
                table: "RefreshTokens",
                column: "ParentTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ReplaceByTokenId",
                table: "RefreshTokens",
                column: "ReplaceByTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_SessionId",
                table: "RefreshTokens",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_AssignedByUserId",
                table: "UserRoles",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_ClientAppId",
                table: "UserSessions",
                column: "ClientAppId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId_ClientAppId_DeviceId",
                table: "UserSessions",
                columns: new[] { "UserId", "ClientAppId", "DeviceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "ClientApps");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
