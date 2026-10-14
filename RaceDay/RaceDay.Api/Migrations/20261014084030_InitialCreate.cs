using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RaceDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventType",
                columns: table => new
                {
                    EventTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventTypeName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventType", x => x.EventTypeID);
                    table.UniqueConstraint("UQ_EventType_EventTypeName", x => x.EventTypeName);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleID);
                    table.UniqueConstraint("UQ_Roles_RoleName", x => x.RoleName);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProfileImageURL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                    table.UniqueConstraint("UQ_Users_Email", x => x.Email);
                    table.ForeignKey(
                        name: "FK_Users_Roles",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID");
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventTypeID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Distance = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    BannerImageURL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventID);
                    table.ForeignKey(
                        name: "FK_Events_EventType",
                        column: x => x.EventTypeID,
                        principalTable: "EventType",
                        principalColumn: "EventTypeID");
                    table.ForeignKey(
                        name: "FK_Events_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventID = table.Column<int>(type: "int", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinAge = table.Column<int>(type: "int", nullable: false),
                    MaxAge = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryID);
                    table.UniqueConstraint("UQ_Categories_EventCategory", x => new { x.EventID, x.CategoryID });
                    table.CheckConstraint("CK_Categories_AgeRange", "MaxAge IS NULL OR MaxAge >= MinAge");
                    table.ForeignKey(
                        name: "FK_Categories_Events",
                        column: x => x.EventID,
                        principalTable: "Events",
                        principalColumn: "EventID");
                });

            migrationBuilder.CreateTable(
                name: "Enrolments",
                columns: table => new
                {
                    EnrolmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    EventID = table.Column<int>(type: "int", nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrolments", x => x.EnrolmentID);
                    table.UniqueConstraint("UQ_Enrolments_UserEvent", x => new { x.UserID, x.EventID });
                    table.CheckConstraint("CK_Enrolments_Status", "Status IN ('Pending', 'Confirmed')");
                    table.ForeignKey(
                        name: "FK_Enrolments_EventCategory",
                        columns: x => new { x.EventID, x.CategoryID },
                        principalTable: "Categories",
                        principalColumns: new[] { "EventID", "CategoryID" });
                    table.ForeignKey(
                        name: "FK_Enrolments_Events",
                        column: x => x.EventID,
                        principalTable: "Events",
                        principalColumn: "EventID");
                    table.ForeignKey(
                        name: "FK_Enrolments_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    ResultID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrolmentID = table.Column<int>(type: "int", nullable: false),
                    FinishTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    FinishingPosition = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.ResultID);
                    table.UniqueConstraint("UQ_Results_Enrolment", x => x.EnrolmentID);
                    table.ForeignKey(
                        name: "FK_Results_Enrolments",
                        column: x => x.EnrolmentID,
                        principalTable: "Enrolments",
                        principalColumn: "EnrolmentID");
                });

            migrationBuilder.InsertData(
                table: "EventType",
                columns: new[] { "EventTypeID", "EventTypeName" },
                values: new object[,]
                {
                    { 1, "Run" },
                    { 2, "Walk" },
                    { 3, "Cycle" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleID", "RoleName" },
                values: new object[,]
                {
                    { 1, "Organiser" },
                    { 2, "Participant" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserID", "DateOfBirth", "Email", "FirstName", "LastName", "PasswordHash", "PhoneNumber", "ProfileImageURL", "RoleID" },
                values: new object[,]
                {
                    { 1, new DateOnly(1985, 3, 21), "thelastbelmont@raceday.co.za", "Trevor", "Belmont", "$2b$11$BkpZnaPwOkrZxW.Nlk2y9e8mCo3fhDDhhi9QGnwNUw3dlUzfyWXpa", "0821234567", null, 1 },
                    { 2, new DateOnly(1990, 8, 9), "speakersypha@raceday.co.za", "Sypha", "Belnades", "$2b$11$U4/77zOepUUoaMQRBiyQI.OvbezrbqS8GkyKW5i6XTqyquPaZr8Ia", "0837654321", null, 1 },
                    { 3, new DateOnly(1998, 5, 2), "floatingsword@gmail.com", "Alucard", "Tepes", "$2b$11$lbEz7U7rWaa0WlM4RN.ZquQgFi0af44pl//TgJnSqr9LnISv40U6i", "0731122334", null, 2 },
                    { 4, new DateOnly(1982, 11, 17), "queenofstyria@gmail.com", "Carmilla", "Karnstein", "$2b$11$JVzq35RGkGJtCSS3vRTVGec3Nb9mDrrkKL81ggI7.P.sT2Z1ib3zC", "0845566778", null, 2 }
                });

            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "EventID", "BannerImageURL", "Description", "Distance", "EventDate", "EventName", "EventTypeID", "Location", "UserID" },
                values: new object[,]
                {
                    { 1, null, "Iconic ultra marathon between Pietermaritzburg and Durban.", 87.70m, new DateOnly(2026, 6, 14), "Comrades Marathon", 1, "Pietermaritzburg to Durban", 1 },
                    { 2, null, "Scenic cycle race around the Cape Peninsula.", 109.00m, new DateOnly(2027, 3, 8), "Cape Town Cycle Tour", 3, "Cape Town", 2 },
                    { 3, null, "Casual weekly walk along the Durban beachfront.", 5.00m, new DateOnly(2027, 1, 16), "Durban Beachfront Park Run", 2, "Durban", 1 }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryID", "CategoryName", "EventID", "MaxAge", "MinAge" },
                values: new object[,]
                {
                    { 1, "Senior", 1, 39, 20 },
                    { 2, "Veteran", 1, 59, 40 },
                    { 3, "Open", 2, 49, 18 },
                    { 4, "Masters", 2, 65, 50 },
                    { 5, "Junior", 3, 17, 10 },
                    { 6, "Open", 3, null, 18 }
                });

            migrationBuilder.InsertData(
                table: "Enrolments",
                columns: new[] { "EnrolmentID", "CategoryID", "EventID", "Status", "UserID" },
                values: new object[,]
                {
                    { 1, 1, 1, "Confirmed", 3 },
                    { 2, 2, 1, "Confirmed", 4 },
                    { 3, 3, 2, "Pending", 3 },
                    { 4, 6, 3, "Confirmed", 4 }
                });

            migrationBuilder.InsertData(
                table: "Results",
                columns: new[] { "ResultID", "EnrolmentID", "FinishTime", "FinishingPosition" },
                values: new object[,]
                {
                    { 1, 1, new TimeOnly(7, 45, 12), 4520 },
                    { 2, 2, new TimeOnly(8, 2, 45), 6789 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "Enrolments");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "EventType");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
