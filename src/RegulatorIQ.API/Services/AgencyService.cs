using RegulatorIQ.Data;
using RegulatorIQ.Models;
using RegulatorIQ.DTOs;
using Microsoft.EntityFrameworkCore;

namespace RegulatorIQ.Services
{
    public interface IAgencyService
    {
        Task<List<AgencyDto>> GetAgenciesAsync(GetAgenciesRequest request);
        Task<AgencyDto?> GetAgencyByIdAsync(Guid id);
    }

    public class AgencyService : IAgencyService
    {
        private readonly RegulatorIQContext _context;
        private readonly ILogger<AgencyService> _logger;

        public AgencyService(
            RegulatorIQContext context,
            ILogger<AgencyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<AgencyDto>> GetAgenciesAsync(GetAgenciesRequest request)
        {
            var query = _context.RegulatoryAgencies.AsQueryable();

            if (!string.IsNullOrEmpty(request.AgencyType))
                query = query.Where(a => a.AgencyType == request.AgencyType);

            if (!string.IsNullOrEmpty(request.Jurisdiction))
                query = query.Where(a => a.Jurisdiction != null &&
                    a.Jurisdiction.ToLower().Contains(request.Jurisdiction.ToLower()));

            if (request.MonitoringEnabled.HasValue)
                query = query.Where(a => a.MonitoringEnabled == request.MonitoringEnabled.Value);

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(a =>
                    a.Name.ToLower().Contains(term) ||
                    (a.Abbreviation != null && a.Abbreviation.ToLower().Contains(term)));
            }

            var agencies = await query
                .OrderBy(a => a.AgencyType)
                .ThenBy(a => a.Name)
                .ToListAsync();

            return agencies.Select(MapToDto).ToList();
        }

        public async Task<AgencyDto?> GetAgencyByIdAsync(Guid id)
        {
            var agency = await _context.RegulatoryAgencies.FindAsync(id);
            return agency != null ? MapToDto(agency) : null;
        }

        private static AgencyDto MapToDto(RegulatoryAgency a) => new()
        {
            Id = a.Id,
            Name = a.Name,
            Abbreviation = a.Abbreviation,
            AgencyType = a.AgencyType,
            Jurisdiction = a.Jurisdiction,
            WebsiteUrl = a.WebsiteUrl,
        };
    }
}
