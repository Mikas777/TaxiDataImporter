using Domain.Models;
using Infrastructure.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using Infrastructure.DAOs;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Repositories;

public class TripRepository(IConfiguration configuration) : ITripRepository
{
    public async Task BulkInsert(List<TaxiTrip> trips)
    {
        if (trips.Count == 0)
        {
            return;
        }

        var table = new DataTable();
        table.Columns.Add(nameof(TaxiTripDao.tpep_pickup_datetime), typeof(DateTime));
        table.Columns.Add(nameof(TaxiTripDao.tpep_dropoff_datetime), typeof(DateTime));
        table.Columns.Add(nameof(TaxiTripDao.passenger_count), typeof(int));
        table.Columns.Add(nameof(TaxiTripDao.trip_distance), typeof(decimal));
        table.Columns.Add(nameof(TaxiTripDao.store_and_fwd_flag), typeof(string));
        table.Columns.Add(nameof(TaxiTripDao.PULocationID), typeof(int));
        table.Columns.Add(nameof(TaxiTripDao.DOLocationID), typeof(int));
        table.Columns.Add(nameof(TaxiTripDao.fare_amount), typeof(decimal));
        table.Columns.Add(nameof(TaxiTripDao.tip_amount), typeof(decimal));

        foreach (var t in trips)
        {
            table.Rows.Add(
                t.TpepPickupDatetime,
                t.TpepDropoffDatetime,
                t.PassengerCount,
                t.TripDistance,
                t.StoreAndFwdFlag,
                t.PULocationID,
                t.DOLocationID,
                t.FareAmount,
                t.TipAmount
            );
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        using (var bulk = new SqlBulkCopy(connectionString))
        {
            bulk.DestinationTableName = "dbo.Trips";

            foreach (DataColumn column in table.Columns)
            {
                bulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }

            await bulk.WriteToServerAsync(table);
        }
    }
}