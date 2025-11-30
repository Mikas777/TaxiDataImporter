IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'TaxiData')
BEGIN
    CREATE DATABASE TaxiData;
END
GO

USE TaxiData;
GO

IF OBJECT_ID('dbo.Trips', 'U') IS NOT NULL
DROP TABLE dbo.Trips;
GO

-- In this case, I prefer to use int for performance reasons. Always I use GUID for identifiers, but here I prioritize speed.
CREATE TABLE dbo.Trips (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    tpep_pickup_datetime DATETIME NOT NULL,
    tpep_dropoff_datetime DATETIME NOT NULL,
    passenger_count INT NULL,
    trip_distance DECIMAL(10, 2) NOT NULL,
    store_and_fwd_flag VARCHAR(3) NULL,
    PULocationID INT NOT NULL,
    DOLocationID INT NOT NULL,
    fare_amount DECIMAL(10, 2) NOT NULL,
    tip_amount DECIMAL(10, 2) NOT NULL
);
GO

CREATE NONCLUSTERED INDEX IX_Trips_PULocation_Tip 
ON dbo.Trips (PULocationID) 
INCLUDE (tip_amount);
GO

CREATE NONCLUSTERED INDEX IX_Trips_Distance 
ON dbo.Trips (trip_distance DESC);
GO

CREATE NONCLUSTERED INDEX IX_Trips_Dates 
ON dbo.Trips (tpep_dropoff_datetime, tpep_pickup_datetime);
GO