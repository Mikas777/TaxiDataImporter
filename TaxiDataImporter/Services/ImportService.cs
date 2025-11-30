using CsvHelper;
using CsvHelper.Configuration;
using Domain.Models;
using Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using TaxiDataImporter.Mappers;
using TaxiDataImporter.Services.Interfaces;

namespace TaxiDataImporter.Services;

public class ImportService(ITripRepository repository, IConfiguration configuration) : IImportService
{
    private const int DefaultBatchSize = 10000;

    public async Task Process()
    {
        var (inputPath, duplicatesPath, batchSize) = GetSettings();

        Console.WriteLine($"[Service] Starting import from: {inputPath}");

        var uniqueKeys = new HashSet<(DateTime, DateTime, int)>();
        var batchBuffer = new List<TaxiTrip>(batchSize);

        using var reader = new StreamReader(inputPath);
        using var csv = new CsvReader(reader, GetCsvConfig());
        await using var writer = new StreamWriter(duplicatesPath);
        await using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.Context.RegisterClassMap<TaxiTripMapper>();
        csvWriter.WriteHeader<TaxiTrip>();
        await csvWriter.NextRecordAsync();

        var stats = new ImportStats();

        foreach (var record in csv.GetRecords<TaxiTrip>())
        {
            stats.TotalRows++;

            if (IsDuplicate(record, uniqueKeys))
            {
                WriteDuplicate(csvWriter, record);
                stats.DuplicatesCount++;
            }
            else
            {
                await AddToBatch(batchBuffer, record, batchSize);
            }
        }

        await FlushRemainingBatch(batchBuffer);
        
        PrintSummary(stats);
    }

    private (string Input, string Output, int Batch) GetSettings()
    {
        var input = configuration["FileSettings:InputCsvPath"]
                    ?? throw new InvalidOperationException("Input path not found");

        var output = configuration["FileSettings:DuplicatesCsvPath"]
                     ?? "duplicates.csv";

        var batch = int.TryParse(configuration["FileSettings:BatchSize"], out var b) ? b : DefaultBatchSize;

        return (input, output, batch);
    }

    private static CsvConfiguration GetCsvConfig()
    {
        return new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
            HeaderValidated = null,
            MissingFieldFound = null
        };
    }

    private static bool IsDuplicate(TaxiTrip record, HashSet<(DateTime, DateTime, int)> uniqueKeys)
    {
        var key = (
            record.TpepPickupDatetime,
            record.TpepDropoffDatetime,
            record.PassengerCount ?? 0
        );

        return !uniqueKeys.Add(key);
    }

    private void WriteDuplicate(CsvWriter writer, TaxiTrip record)
    {
        writer.WriteRecord(record);
        writer.NextRecord();
    }

    private async Task AddToBatch(List<TaxiTrip> buffer, TaxiTrip record, int limit)
    {
        buffer.Add(record);

        if (buffer.Count >= limit)
        {
            await repository.BulkInsert(buffer);
            buffer.Clear();
            Console.Write(".");
        }
    }

    private async Task FlushRemainingBatch(List<TaxiTrip> buffer)
    {
        if (buffer.Count > 0)
        {
            await repository.BulkInsert(buffer);
        }
    }

    private static void PrintSummary(ImportStats stats)
    {
        Console.WriteLine("\n------------------------------------------------");
        Console.WriteLine("Import Completed Successfully!");
        Console.WriteLine($"Total processed: {stats.TotalRows}");
        Console.WriteLine($"Duplicates:      {stats.DuplicatesCount}");
        Console.WriteLine($"Inserted to DB:  {stats.TotalRows - stats.DuplicatesCount}");
        Console.WriteLine("------------------------------------------------");
    }
}