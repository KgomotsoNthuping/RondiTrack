using Api.Contracts;
using Api.Models;

namespace Api.Mapping;

public static class StokvelMapping
{
    public static Stokvel ToDomain(
        this CreateStokvelRequest request)
    {
        return new Stokvel(
            request.Name,
            request.MonthlyContribution,
            request.CurrentPeriod,
            request.TotalPeriods,
            request.Rules);
    }

    public static void ApplyTo(
        this UpdateStokvelRequest request,
        Stokvel stokvel)
    {
        stokvel.UpdateDetails(
            request.Name,
            request.MonthlyContribution,
            request.CurrentPeriod,
            request.TotalPeriods,
            request.Rules);
    }

    public static StokvelResponse ToResponse(
        this Stokvel stokvel)
    {
        return new StokvelResponse(
            stokvel.Id,
            stokvel.Name,
            stokvel.MonthlyContribution,
            stokvel.CurrentPeriod,
            stokvel.TotalPeriods,
            stokvel.Rules,
            stokvel.Members.Count);
    }
}