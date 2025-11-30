using Domain.Models;

namespace Infrastructure.Repositories.Interfaces;

public interface ITripRepository
{
    Task BulkInsert(List<TaxiTrip> trips);
}