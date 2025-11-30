# NYC Taxi ETL Importer

Console app to import NYC Taxi data (CSV) into MS SQL Server. Built with .NET 9 using Clean Architecture.

## Key Features

* **Architecture:** Onion/Clean Architecture (Domain -> Infrastructure -> App).
* **Performance:** Uses `SqlBulkCopy` with batching (10k rows) for speed.
* **Memory:** Streams CSV via `CsvHelper` to handle large files without loading everything into RAM.
* **Logic:**
    * Converts EST time to **UTC**.
    * Normalizes flags (`Y`/`N` -> `Yes`/`No`) and trims whitespace.
    * Fixes SQL date overflows (defaults invalid dates to `1900-01-01`).
* **Deduplication:** Checks duplicates by `Pickup` + `Dropoff` + `PassengerCount` using a HashSet. Duplicates are saved to `duplicates.csv`.

## Setup & Run

1.  **Database:** Run the script `TaxiDataImporter/SqlScripts/schema.sql` to create DB and indexes.
2.  **Config:** Update `appsettings.json` with your file paths and connection string:
    ```json
    {
      "ConnectionStrings": {
        "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=TaxiData;Trusted_Connection=True;"
      },
      "FileSettings": {
        "InputCsvPath": "C:\\data\\taxi_data.csv",
        "DuplicatesCsvPath": "C:\\data\\duplicates.csv",
        "BatchSize": 10000
      }
    }
    ```
3.  **Run:** `dotnet run --project TaxiDataImporter`

## Benchmark (Local Machine)

Processed **30,000 rows** in **~1.01 seconds**.
* Inserted: 29,846
* Duplicates: 154 (befor I implement date convertor, it was 111 records, but now we have duplicate dates)

## Assumptions

* Input time is **EST**, stored as **UTC**.
* Dates older than 1753 are treated as invalid (set to `1900-01-01`).
* A record is a duplicate if Pickup, Dropoff, and Passenger Count are identical.

## Handling 10GB+ Files (Scalability)

Processing a 10GB file requires minimizing memory usage. The current in-memory `HashSet` would crash with an `OutOfMemoryException`.

1.  **Move Deduplication to Database:** Instead of checking for duplicates in C# (RAM), I would insert all raw data into a temporary **Staging Table** in SQL Server. Then, I would run a SQL query to remove duplicates before moving clean data to the main table.
2.  **Streaming:** Continue using `StreamReader` (as implemented) to read the file line-by-line. This ensures that memory usage remains low (e.g., ~50MB) regardless of whether the file is 100MB or 100GB.
---
*Author: Vladyslav Kozhukhivskyi*