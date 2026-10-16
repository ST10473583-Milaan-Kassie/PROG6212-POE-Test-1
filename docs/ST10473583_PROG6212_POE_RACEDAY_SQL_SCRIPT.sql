-- creates the database first if it doesnt exist yet, so the script runs on a clean instance
IF DB_ID('ST10473583_PROG6212_POE_RACEDAY_SQL') IS NULL
    CREATE DATABASE ST10473583_PROG6212_POE_RACEDAY_SQL;
GO

USE ST10473583_PROG6212_POE_RACEDAY_SQL;
GO

-- TABLE CREATION

-- Roles table
-- either organiser or participant
CREATE TABLE Roles
(
    -- (1,1) means the IDs start at 1 and increase by 1 each time
    -- so it starts at 1 and goes up by 1 each time
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);


-- Users table
-- a user is either an organiser or participant
-- the role, coming from the Roles table, dictates what they can do(which API endpoints they can use)
CREATE TABLE Users 
(
    UserID INT IDENTITY(1,1) PRIMARY KEY,

    -- FK, which role this user has(participant or organiser)
    RoleID INT NOT NULL, 

    -- both required
    -- renamed from Surname to LastName, matches the API
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,

    -- required for all users, used to check the category age restrictions when enrolling
    DateOfBirth DATE NOT NULL,

    -- emails are used to log in, must be unique
    Email NVARCHAR(100) NOT NULL UNIQUE,

    -- null bcs its not required at registration
    -- renamed from PhoneNum to PhoneNumber, clarity sake
    PhoneNumber NVARCHAR(20) NULL,
    PasswordHash NVARCHAR(255) NOT NULL,

    -- filled in once they upload a pic
    ProfileImageURL NVARCHAR(255) NULL, 

    -- FK reference constraint
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
);

-- EventType table
-- lookup table for event type (run/walk/cycle)
CREATE TABLE EventType 
(
    EventTypeID INT IDENTITY(1,1) PRIMARY KEY,

    -- renamed from Type to EventTypeName, clarity sake and it matches RoleName in the Roles table
    -- also cant be called EventType in C# since the class is already called EventType
    EventTypeName NVARCHAR(20) NOT NULL UNIQUE
);


-- Events table
-- each event is created by one organiser
CREATE TABLE Events 
(
    EventID INT IDENTITY(1,1) PRIMARY KEY,

    -- required
    EventName NVARCHAR(100) NOT NULL,

    -- FK for the EventType table, (run/walk/cycle)
    EventTypeID INT NOT NULL, 

    -- FK for the Users table, the organiser who owns this event
    UserID INT NOT NULL, 

    Description NVARCHAR(500) NULL,

    -- renamed from Date to EventDate, clarity sake
    EventDate DATE NOT NULL,

    Location NVARCHAR(150) NOT NULL,

    -- (6,2) means 6 total digits, with 2 digits after the decimal point, so 9999.99 is the max
    Distance DECIMAL(6,2) NOT NULL, -- in km

    -- 0 until the organiser publishes the results, participants only see results once its 1
    ResultsPublished BIT NOT NULL DEFAULT 0,

    --null for now cause its empty until they upload one
    BannerImageURL NVARCHAR(255) NULL,

    -- FK constraints
    CONSTRAINT FK_Events_EventType FOREIGN KEY (EventTypeID) REFERENCES EventType(EventTypeID),
    CONSTRAINT FK_Events_Users FOREIGN KEY (UserID) REFERENCES Users(UserID)
);


-- categories table
-- categories belong to one event
-- a category is defined by an age range, a distance, or both
CREATE TABLE Categories 
(
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,

    -- FK for the events table, which event this category belongs to
    EventID INT NOT NULL, 

    CategoryName NVARCHAR(50) NOT NULL,

    -- null for a distance only category, which is open to every age
    MinAge INT NULL,

    -- null as there may be no max age
    MaxAge INT NULL, 

    -- in km, null for an age only category
    -- same (6,2) size as the event distance
    Distance DECIMAL(6,2) NULL,

    -- FK constraints
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID) REFERENCES Events(EventID),

    -- allows EventID and CategoryID to be referenced together by Enrolments
    CONSTRAINT UQ_Categories_EventCategory UNIQUE (EventID, CategoryID),

    -- ensures a maximum age has a minimum age to go with it, and is not lower than it
    CONSTRAINT CK_Categories_AgeRange CHECK (MaxAge IS NULL OR (MinAge IS NOT NULL AND MaxAge >= MinAge)),

    -- ensures every category has an age range, a distance, or both
    CONSTRAINT CK_Categories_AgeOrDistance CHECK (MinAge IS NOT NULL OR Distance IS NOT NULL)
);


-- Enrolments table
-- enrolments link a participant to an event and their chosen category
-- this is the junction table for the whole entry process
CREATE TABLE Enrolments 
(
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,

    -- FK, for the Users table, the participant enrolling
    UserID INT NOT NULL, 

    -- FK for the Events table, the event they're entering
    EventID INT NOT NULL, 

    -- FK for the category table, the category they picked
    CategoryID INT NOT NULL,

    -- either pending or confirmed. pending by default
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending', 

    -- FK constraints
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT FK_Enrolments_Events FOREIGN KEY (EventID) REFERENCES Events(EventID),
    CONSTRAINT FK_Enrolments_EventCategory FOREIGN KEY (EventID, CategoryID) REFERENCES Categories(EventID, CategoryID),

    -- seperation constraint, stops users from entering the same event twice
    CONSTRAINT UQ_Enrolments_UserEvent UNIQUE (UserID, EventID),

    -- only allows Pending or Confirmed enrolment statuses
    CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Pending', 'Confirmed'))
);


-- Results table
-- results, attached to an enrolment, are captured by the organiser once the event is done
CREATE TABLE Results 
(
    ResultID INT IDENTITY(1,1) PRIMARY KEY,

    -- FK for the Enrolments table, which enrolment the results belong to
    EnrolmentID INT NOT NULL,

    FinishTime TIME NOT NULL,
    FinishingPosition INT NOT NULL,

    -- FK constraints
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID) REFERENCES Enrolments(EnrolmentID),

    -- Uniqueness constraint, prevents an enrolment from having multiple results
    CONSTRAINT UQ_Results_Enrolment UNIQUE (EnrolmentID)
);

-- INSERT Queries

-- both roles
INSERT INTO Roles (RoleName) VALUES
('Organiser'),
('Participant');

-- the three event types
INSERT INTO EventType (EventTypeName) VALUES
('Run'),
('Walk'), 
('Cycle');

-- users, 2 organisers (roleID 1) and 2 participants (roleID 2)
-- participant DOBs fit the age ranges of the categories they enrol in below
INSERT INTO Users (RoleID, FirstName, LastName, DateOfBirth, Email, PhoneNumber, PasswordHash, ProfileImageURL) VALUES
(1, 'Trevor', 'Belmont', '1985-03-21', 'thelastbelmont@raceday.co.za', '0821234567', '$2b$11$BkpZnaPwOkrZxW.Nlk2y9e8mCo3fhDDhhi9QGnwNUw3dlUzfyWXpa', NULL),
(1, 'Sypha',  'Belnades', '1990-08-09', 'speakersypha@raceday.co.za', '0837654321', '$2b$11$U4/77zOepUUoaMQRBiyQI.OvbezrbqS8GkyKW5i6XTqyquPaZr8Ia', NULL),
(2, 'Alucard', 'Tepes', '1998-05-02', 'floatingsword@gmail.com', '0731122334', '$2b$11$lbEz7U7rWaa0WlM4RN.ZquQgFi0af44pl//TgJnSqr9LnISv40U6i', NULL),
(2, 'Carmilla','Karnstein', '1982-11-17', 'queenofstyria@gmail.com', '0845566778', '$2b$11$JVzq35RGkGJtCSS3vRTVGec3Nb9mDrrkKL81ggI7.P.sT2Z1ib3zC', NULL);

-- events, 3 inserts
-- comrades already happened this year, the other two are still upcoming
-- ResultsPublished is left at its default of 0, comrades results are captured but not published yet
INSERT INTO Events (EventName, EventTypeID, UserID, Description, EventDate, Location, Distance, BannerImageURL) VALUES
('Comrades Marathon', 1, 1, 'Iconic ultra marathon between Pietermaritzburg and Durban.', '2026-06-14', 'Pietermaritzburg to Durban', 87.70, NULL),
('Cape Town Cycle Tour', 3, 2, 'Scenic cycle race around the Cape Peninsula.', '2027-03-08', 'Cape Town', 109.00, NULL),
('Durban Beachfront Park Run', 2, 1, 'Casual weekly walk along the Durban beachfront.', '2027-01-16', 'Durban', 5.00,  NULL);


-- categories, 8 inserts, a couple per event, mix of age based, distance based and open
-- Fun Ride is distance only, Junior 2.5km has both, the rest are age only
INSERT INTO Categories (EventID, CategoryName, MinAge, MaxAge, Distance) VALUES
(1, 'Senior',       20,   39,   NULL),
(1, 'Veteran',      40,   59,   NULL),
(2, 'Open',         18,   49,   NULL),
(2, 'Masters',      50,   65,   NULL),
(3, 'Junior',       10,   17,   NULL),
(3, 'Open',         18,   NULL, NULL),
(2, 'Fun Ride',     NULL, NULL, 47.00),
(3, 'Junior 2.5km', 10,   17,   2.50);


-- enrolments, 4 inserts, these are participants entering the events above
-- uniqueness constraint on UserID and EventID prevents double booking
INSERT INTO Enrolments (UserID, EventID, CategoryID, Status) VALUES
(3, 1, 1, 'Confirmed'),
(4, 1, 2, 'Confirmed'),
(3, 2, 3, 'Pending'),
(4, 3, 6, 'Confirmed');


-- results, 2 inserts, only for comrades since that's the only event that's actually happened
INSERT INTO Results (EnrolmentID, FinishTime, FinishingPosition) VALUES
(1, '07:45:12', 1),
(2, '08:02:45', 2);

-- checking each table
SELECT * FROM Roles;
SELECT * FROM Users;
SELECT * FROM EventType;
SELECT * FROM Events;
SELECT * FROM Categories;
SELECT * FROM Enrolments;
SELECT * FROM Results;

-- for testing and redo
--DROP TABLE Results;
--DROP TABLE Enrolments;
--DROP TABLE Categories;
--DROP TABLE Events;
--DROP TABLE EventType;
--DROP TABLE Users;
--DROP TABLE Roles;
