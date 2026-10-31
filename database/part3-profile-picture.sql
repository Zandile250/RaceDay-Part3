-- Part 3: profile pictures.
-- Adds the one new column the Part 3 API needs. Safe to run more than once.
-- Run it against your RaceDay database AFTER raceday-database.sql, e.g. in SSMS/Azure Data Studio,
-- with the RaceDay database selected (or add a USE statement with your database name above this line).

IF COL_LENGTH('dbo.Users', 'ProfilePictureUrl') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD ProfilePictureUrl VARCHAR(500) NULL;
END
GO
