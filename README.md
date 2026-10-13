# RaceDay Race Event Management System

PROG6212 Portfolio of Evidence, Part 1.

| Project Details   | Information                                      |
| ----------------- | ------------------------------------------------ |
| Module            | PROG6212 (Programming 2B)                        |
| Student           | Milaan Kassie (ST10473583)                       |
| Repository        | `emkndn-prog6212-2026-prog6212-poe-st10473583-milaan-kassie`|
| Video walkthrough | [VIDEO](https://youtu.be/-iElrJIxu6g)             |

## Table of Contents

- [About the project](#about-the-project)
- [Project objectives](#project-objectives)
- [User roles](#user-roles)
- [Assignment scope](#assignment-scope)
- [Technology](#technology)
- [Repository contents](#repository-contents)
- [Database design](#database-design)
- [Entity relationship diagram](#entity-relationship-diagram)
- [API endpoint plan](#api-endpoint-plan)
- [Changes since Part 1](#changes-since-part-1)
- [CI/CD pipeline](#cicd-pipeline)
- [Video walkthrough](#video-walkthrough)
- [Author](#author)

## About the project

RaceDay is a race event management system designed to support the organisation and participation of running, walking, and cycling events. Organisers can create and manage events, define the categories available for entrance in each event, and capture participant results after an event has taken place. Participants can browse available events, enrol in an event under their chosen category, and view their enrolment status and results once the event has ended.

The planned system will use ASP.NET Core Web API for the backend, Entity Framework Core for the data access layer, and ASP.NET Core MVC application for the front end. Microsoft SQL Server is used to store and manage the application's data.

This project is being developed across three parts, with Part 1 covering the planning phase only.

At this stage, this repository contains the planning documents and SQL database script, all found under the `docs` folder. The code aspects of the application have not yet been implemented.

## Project objectives

The main objectives are to:

- Allow organisers to create and manage race events.
- Allow organisers to define and create categories with age restrictions for their events.
- Allow participants to browse available events and enrol in them.
- Maintain the relationship between participants, events, and selected categories.
- Record participant results after an event has taken place.
- Enforce data integrity through database relationships, constraints, and uniqueness rules.
- Provide a clearly defined API structure that can be implemented in the later stages of the project.

## User roles

The system defines two roles, stored in the `Roles` table and assigned to every user at registration.

### Organiser

- Creates and manages events, defining fields such as the event name, event type(run/walk/cycle), date, location, distance, and description.
- Defines the categories available for each of their events, specifying the minimum and maximum age restrictions.
- Captures results for participants once an event has taken place.

### Participant

- Browses events created by organisers.
- Enrols in an event and selects the category they wish to compete in.
- Views their enrolment status and results once these have been captured by the organiser.

## Assignment scope

This submission covers Part 1 of the three-part Portfolio of Evidence (POE).

Part 1 focuses on the planning and database foundation of the RaceDay system. 

The submission consists of:
- ERD: An entity relationship diagram defining and visualising the database structure and relationships.
- API Endpoint Plan: An API endpoint plan defining the routes, HTTP methods, operations, and access and role requirements of the planned API system.
- SQL Script: A SQL database script that creates the required database schema, constraints, relationships, and inserts a few records of sample data.

The working application will be developed in the parts two and three. These parts will make use of the database design and API plan defined in this part one submission as the foundation for the ASP.NET Core Web API and MVC front end.

## Technology

| Part              | Choice                              |
| ----------------- | ----------------------------------- |
| Backend framework | ASP.NET Core Web API                |
| Data access       | Entity Framework Core               |
| Front end         | ASP.NET Core MVC                    |
| Database          | Microsoft SQL Server                |
| Database management tool  | SQL Server Management Studio (SSMS) |
| Source control    | Git / GitHub                        |
| CI/CD             | GitHub Actions                      |

## Repository contents

| File                                                    | Description                                                                                                          |
| ------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| `ST10473583_PROG6212_POE_RACEDAY_ERD.pdf`               | Entity relationship diagram for the RaceDay database.                                                                |
| `ST10473583_PROG6212_POE_RACEDAY_API_ENDPOINT_PLAN.pdf` | Planned API endpoints, covering the routes, HTTP methods, operations, and the role to access each API endpoint.      |
| `ST10473583_PROG6212_POE_RACEDAY_SQL_SCRIPT.sql`        | SQL script that creates the RaceDay database schema, applies constraints and relationships, and inserts sample data. |

## Database design

The RaceDay database consists of seven tables, with relationships designed to represent Users, Events, Categories, Enrolments, and Results.

- **Roles**: lookup table containing the two user roles: Organiser and Participant.
- **Users**: stores registered users, including the date of birth used to check category age restrictions, and assigns each user to a role through `RoleID`.
- **EventType**: lookup table containing the three event types: Run, Walk, and Cycle.
- **Events**: stores race events created by organisers, each event is linked to its organiser and event type.
- **Categories**: stores the categories available within an event, defined by the minimum and maximum age restrictions.
- **Enrolments**: junction table connecting participants to events and their selected categories. Each enrolment also has a status of either Pending or Confirmed.
- **Results**: stores the finishing position and finishing time associated with an enrolment once a participant's result for an event has been captured.

The database uses primary keys and foreign keys to maintain relationships between the tables. Additional constraints are used to protect data integrity and enforce the rules of the system.

Key constraints include:

- A user cannot enrol in the same event more than once.
- An enrolment cannot be assigned more than one result.
- Enrolment status is restricted to either `Pending` or `Confirmed`.
- Foreign key relationships prevent records from referencing users, events, categories, or enrolments that do not exist.
- Event categories are linked to a specific event instead of being shared across all events.

The SQL script also includes insert queries for sample data so that the database structure and relationships can be tested (in SQL Server Management Studio).

## Entity relationship diagram

`ST10473583_PROG6212_POE_RACEDAY_ERD.pdf` contains the entity relationship diagram for the RaceDay database.

The ERD shows the seven tables, their attributes, primary and foreign keys, and the relationships between the entities. It provides a visual representation of the database structure used by the SQL script and serves as the foundational database design reference for the later stages of the project.

## API endpoint plan

`ST10473583_PROG6212_POE_RACEDAY_API_ENDPOINT_PLAN.pdf` documents the planned API endpoints for the RaceDay system.

The endpoint plan identifies the HTTP method, route, purpose, authentication and role requirements, and relevant response codes and messages for each operation. The business rules section at the end of the plan defines the conditions under which events, categories, enrolments, and results can be created, changed, or deleted.

## Changes since Part 1

The Part 1 submission, exactly as it was handed in, is preserved in the [`part-1-submission`](https://github.com/EMKNDN/emkndn-prog6212-2026-prog6212-poe-st10473583-milaan-kassie/releases/tag/part-1-submission) release. Before development of the API began in Part 2, the planning documents were reviewed against the Part 2 requirements, and the amendments below were applied to the SQL script, ERD and API endpoint plan together, so that the three documents remain consistent with one another. The CI workflow was also extended at the start of Part 2, as recorded in the final row of the table.

| Change | Reason |
| ------ | ------ |
| `DateOfBirth` (`DATE`, `NOT NULL`) added to `Users`. | Categories define age restrictions, but the original schema stored no date of birth, so a participant's eligibility for a category could not be verified. |
| `Surname` renamed to `LastName` and `PhoneNum` renamed to `PhoneNumber` in `Users`. | The database and the endpoint plan used different names for the same fields. Each field now has a single name across the database, the API and the request bodies. |
| `EventType` column renamed to `EventTypeName` in the `EventType` table. | C# does not allow a property to share the name of its enclosing class, so the entity could not mirror the original column name. The new name also follows the pattern of `RoleName` in the `Roles` table. |
| Request body fields aligned with the database (`name` to `eventName` and `categoryName`, `phone` to `phoneNumber`, `role` to `roleName`). | Keeps the naming consistent between the endpoint plan, the API and the database. |
| SQL script now creates the database when it does not already exist. | The original script assumed an existing database and would fail on a clean SQL Server instance. |
| Durban Beachfront Park Run seed date moved from 3 October 2026 to 16 January 2027. | The original date passed during Part 2 development, and an upcoming event is needed to demonstrate the enrolment and withdrawal rules. |
| Missing `400 Bad Request`, `401 Unauthorized` and `403 Forbidden` responses added to the endpoint plan. | Request validation and role enforcement return these codes, and the plan must reflect the behaviour of the implemented API. |
| `409 Conflict` responses and a business rules section added to the endpoint plan. | Events that have taken place, and the records linked to them, are protected from changes so that participants' race history is preserved. |
| `GET /api/event-types` endpoint and the query parameters for filtering events and enrolments added to the endpoint plan. | The front end in Part 3 needs the list of event types for its forms and filters, and the filters needed defined parameters. |
| `/api/users/me/profilepicture` renamed to `/api/users/me/profile-picture`. | Multi-word routes now follow the same hyphenated format as `/api/event-types`. |
| `validate-docs-workflow.yml` renamed to `raceday-ci.yml`, with a build and test job added alongside the unchanged Part 1 structure checks. | Part 2 requires the workflow to build the project and run all unit tests on every push. Keeping a single workflow places the structure, build and test results in one run, and allows Part 3 to extend the same workflow to build the MVC project. |

## CI/CD pipeline

The repository uses GitHub Actions to provide automated validation of the project structure. The workflow runs when changes are pushed to the repository or when a pull request is created.

The workflow checks that the expected project documentation is present in the repository and returns a short report, in table format, of which required documents are present and which are missing. This provides an automated method of confirming that all the required files have been committed and that the repository maintains the proper structure.

## GitHub Actions workflow / Latest Successful Actions run

<img width="1758" height="930" alt="image" src="https://github.com/user-attachments/assets/eb0d9ceb-f776-48f7-b9f7-9296e5bcc437" />

## Video walkthrough

An explanatory YouTube video is attached to this submission. It covers the planning documents, the reasoning behind the entity relationship diagram, the choices made in the API endpoint plan, and a live run of the SQL script in SQL Server Management Studio. The video link is provided in the table at the top of this document.

## Author

Milaan Kassie (ST10473583)
