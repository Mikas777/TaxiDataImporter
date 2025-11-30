using CsvHelper;
using CsvHelper.Configuration;
using Domain.Models;

namespace TaxiDataImporter.Mappers;

public sealed class TaxiTripMapper : ClassMap<TaxiTrip>
{
    private readonly TimeZoneInfo _estZone;

    public TaxiTripMapper()
    {
        try
        {
            _estZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            _estZone = TimeZoneInfo.CreateCustomTimeZone("EST_Fallback", TimeSpan.FromHours(-5), "Eastern Standard Time", "Eastern Standard Time");
        }

        Map(m => m.TpepPickupDatetime).Name("tpep_pickup_datetime")
            .Convert(args => ParseEstToUtc(args, "tpep_pickup_datetime"));

        Map(m => m.TpepDropoffDatetime).Name("tpep_dropoff_datetime")
            .Convert(args => ParseEstToUtc(args, "tpep_dropoff_datetime"));

        Map(m => m.PassengerCount).Name("passenger_count");
        Map(m => m.TripDistance).Name("trip_distance");

        Map(m => m.StoreAndFwdFlag).Name("store_and_fwd_flag")
            .Convert(args =>
            {
                var val = args.Row.GetField("store_and_fwd_flag")?.Trim();
                return string.Equals(val, "Y", StringComparison.OrdinalIgnoreCase) ? "Yes" : "No";
            });

        Map(m => m.PULocationID).Name("PULocationID");
        Map(m => m.DOLocationID).Name("DOLocationID");
        Map(m => m.FareAmount).Name("fare_amount");
        Map(m => m.TipAmount).Name("tip_amount");
    }

    private DateTime ParseEstToUtc(ConvertFromStringArgs args, string fieldName)
    {
        var dtStr = args.Row.GetField(fieldName);

        if (DateTime.TryParse(dtStr, out var dt))
        {
            if (dt.Year < 1753 || dt.Year > 9999)
            {
                return new DateTime(1900, 1, 1);
            }

            return TimeZoneInfo.ConvertTimeToUtc(dt, _estZone);
        }

        return new DateTime(1900, 1, 1);
    }
}