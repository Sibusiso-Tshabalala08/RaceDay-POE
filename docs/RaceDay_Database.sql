/* ============================================================
   RaceDay Database Schema
   Part 1, Section C — SQL Database Script
   Target: SQL Server (SSMS)
   ============================================================
   Run this script on a clean database. It will:
     1. Create all 6 tables matching the approved ERD
     2. Define all primary keys, foreign keys, and constraints
     3. Seed realistic sample data
   ============================================================ */

-- ------------------------------------------------------------
-- Create the RaceDay database if it doesn't already exist,
-- then switch into it. This ensures the tables below are NOT
-- accidentally created inside 'master' or another database.
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'RaceDay')
BEGIN
    CREATE DATABASE RaceDay;
END
GO

USE RaceDay;
GO

-- ------------------------------------------------------------
-- Clean slate: drop tables if they already exist (in FK-safe order)
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

-- ============================================================
-- 1. Roles  (lookup table: Organiser / Participant)
-- ============================================================
CREATE TABLE dbo.Roles (
    RoleId      INT IDENTITY(1,1) PRIMARY KEY,
    RoleName    VARCHAR(20) NOT NULL UNIQUE
        CHECK (RoleName IN ('Organiser', 'Participant'))
);
GO

-- ============================================================
-- 2. Users
-- ============================================================
CREATE TABLE dbo.Users (
    UserId          INT IDENTITY(1,1) PRIMARY KEY,
    FullName        VARCHAR(100) NOT NULL,
    Email           VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255) NOT NULL,
    RoleId          INT NOT NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId)
        REFERENCES dbo.Roles(RoleId)
);
GO

-- ============================================================
-- 3. Events
-- ============================================================
CREATE TABLE dbo.Events (
    EventId         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId     INT NOT NULL,
    EventName       VARCHAR(150) NOT NULL,
    EventDate       DATETIME NOT NULL,
    Location        VARCHAR(200) NOT NULL,
    Latitude        DECIMAL(9,6) NULL,
    Longitude       DECIMAL(9,6) NULL,
    Distance        DECIMAL(6,2) NULL,
    EventType       VARCHAR(10) NOT NULL
        CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    RouteMapUrl     VARCHAR(500) NULL,
    Description     VARCHAR(1000) NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserId)
        REFERENCES dbo.Users(UserId)
);
GO

-- ============================================================
-- 4. Categories
--    CategoryType drives which of (Distance) or (MinAge/MaxAge)
--    is populated. Enforced with a CHECK constraint below.
-- ============================================================
CREATE TABLE dbo.Categories (
    CategoryId      INT IDENTITY(1,1) PRIMARY KEY,
    EventId         INT NOT NULL,
    CategoryName    VARCHAR(50) NOT NULL,
    CategoryType    VARCHAR(10) NOT NULL
        CHECK (CategoryType IN ('Age', 'Distance')),
    Distance        DECIMAL(6,2) NULL,
    MinAge          INT NULL,
    MaxAge          INT NULL,
    MaxParticipants INT NOT NULL DEFAULT 100,
    EntryFee        DECIMAL(8,2) NOT NULL DEFAULT 0,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events(EventId) ON DELETE CASCADE,
    CONSTRAINT CHK_Category_TypeFields CHECK (
        (CategoryType = 'Distance' AND Distance IS NOT NULL AND MinAge IS NULL AND MaxAge IS NULL)
        OR
        (CategoryType = 'Age' AND Distance IS NULL AND MinAge IS NOT NULL AND MaxAge IS NOT NULL)
    )
);
GO

-- ============================================================
-- 5. Enrolments
--    Junction entity resolving the many-to-many between
--    Participants (Users) and Categories.
-- ============================================================
CREATE TABLE dbo.Enrolments (
    EnrolmentId     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId   INT NOT NULL,
    CategoryId      INT NOT NULL,
    EnrolmentDate   DATETIME NOT NULL DEFAULT GETDATE(),
    Status          VARCHAR(20) NOT NULL DEFAULT 'Confirmed'
        CHECK (Status IN ('Confirmed', 'Cancelled')),
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantId)
        REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories(CategoryId),
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantId, CategoryId)
);
GO

-- ============================================================
-- 6. Results
--    One-to-one with Enrolments (EnrolmentId is UNIQUE).
--    FinishTime/Position nullable to support DNF/DNS/DQ.
-- ============================================================
CREATE TABLE dbo.Results (
    ResultId        INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId     INT NOT NULL UNIQUE,
    FinishTime      TIME NULL,
    Position        INT NULL,
    Status          VARCHAR(10) NOT NULL DEFAULT 'Finished'
        CHECK (Status IN ('Finished', 'DNF', 'DNS', 'DQ')),
    RecordedAt      DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId)
        REFERENCES dbo.Enrolments(EnrolmentId)
);
GO

/* ============================================================
   SEED DATA
   ============================================================ */

-- ------------------------------------------------------------
-- Roles
-- ------------------------------------------------------------
INSERT INTO dbo.Roles (RoleName) VALUES
('Organiser'),
('Participant');
GO

-- ------------------------------------------------------------
-- Users: 2 Organisers, 2 Participants
-- (PasswordHash values below are placeholders representing
--  already-hashed passwords, not plaintext)
-- ------------------------------------------------------------
INSERT INTO dbo.Users (FullName, Email, PasswordHash, RoleId) VALUES
('Thabo Nkosi',     'thabo.nkosi@raceday.co.za',    'HASH_PLACEHOLDER_1', 1), -- Organiser
('Lindiwe Dlamini',  'lindiwe.dlamini@raceday.co.za','HASH_PLACEHOLDER_2', 1), -- Organiser
('Sarah van der Merwe','sarah.vdm@example.com',      'HASH_PLACEHOLDER_3', 2), -- Participant
('Ahmed Patel',      'ahmed.patel@example.com',      'HASH_PLACEHOLDER_4', 2); -- Participant
GO

-- ------------------------------------------------------------
-- Events: 3 events, owned by the 2 organisers
-- ------------------------------------------------------------
INSERT INTO dbo.Events (OrganiserId, EventName, EventDate, Location, Latitude, Longitude, Distance, EventType, RouteMapUrl, Description) VALUES
(1, 'Soweto Marathon',       '2026-11-08 06:00:00', 'Soweto, Johannesburg', -26.2485, 27.8540, 42.20, 'Run',   NULL, 'Annual road marathon through the streets of Soweto.'),
(1, 'Cape Town Cycle Tour',  '2026-03-08 06:30:00', 'Cape Town City Centre', -33.9249, 18.4241, 109.00,'Cycle', NULL, 'Iconic cycling race around the Cape Peninsula.'),
(2, 'Park Walk & Wellness Day','2026-09-20 08:00:00','Zoo Lake, Johannesburg', -26.1636, 28.0384, 5.00,  'Walk',  NULL, 'Community fun walk promoting health and wellness.');
GO

-- ------------------------------------------------------------
-- Categories: mix of Distance-based and Age-based per event
-- ------------------------------------------------------------
INSERT INTO dbo.Categories (EventId, CategoryName, CategoryType, Distance, MinAge, MaxAge, MaxParticipants, EntryFee) VALUES
-- Soweto Marathon (EventId 1)
(1, '10km',      'Distance', 10.00, NULL, NULL, 500, 150.00),
(1, '21km',      'Distance', 21.10, NULL, NULL, 500, 250.00),
(1, 'Under 20',  'Age',      NULL,  15,   19,   100, 100.00),
-- Cape Town Cycle Tour (EventId 2)
(2, '109km',     'Distance', 109.00,NULL, NULL, 1000,350.00),
(2, 'Senior',    'Age',      NULL,  50,   99,   200, 300.00),
-- Park Walk & Wellness Day (EventId 3)
(3, '5km',       'Distance', 5.00,  NULL, NULL, 300, 50.00);
GO

-- ------------------------------------------------------------
-- Enrolments: participants entering categories
-- ------------------------------------------------------------
INSERT INTO dbo.Enrolments (ParticipantId, CategoryId, Status) VALUES
(3, 1, 'Confirmed'),  -- Sarah -> Soweto Marathon 10km
(3, 4, 'Confirmed'),  -- Sarah -> Cape Town Cycle Tour 109km
(4, 2, 'Confirmed'),  -- Ahmed -> Soweto Marathon 21km
(4, 6, 'Confirmed');  -- Ahmed -> Park Walk 5km
GO

-- ------------------------------------------------------------
-- Results: sample outcomes, including one DNF to show the
-- Status field is actually used
-- ------------------------------------------------------------
INSERT INTO dbo.Results (EnrolmentId, FinishTime, Position, Status) VALUES
(1, '00:52:14', 12, 'Finished'),  -- Sarah's 10km result
(3, NULL,       NULL, 'DNF'),     -- Ahmed's 21km: did not finish
(4, '00:28:03', 5,  'Finished'); -- Ahmed's 5km walk result
-- Note: EnrolmentId 2 (Sarah's cycle tour) has no result yet — event is in the future.
GO
