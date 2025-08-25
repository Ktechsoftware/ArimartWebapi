using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArimartEcommerceAPI.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using ArimartEcommerceAPI.Services;

namespace ArimartEcommerceAPI.Services.Services
{
    //public class DeliveryPartnerService : IDeliveryPartnerService
    //{
    //    private readonly ApplicationDbContext _context;
    //    private readonly IConfiguration _configuration;

    //    public DeliveryPartnerService(ApplicationDbContext context, IConfiguration configuration)
    //    {
    //        _context = context;
    //        _configuration = configuration;
    //    }

    //    public async Task<(bool success, string message)> AssignOrderToPartnerAsync(string trackId, int partnerId)
    //    {
    //        var partner = await _context.TblDeliveryPartners
    //            .FirstOrDefaultAsync(p => p.Id == partnerId && p.IsActive && p.IsAvailable);

    //        if (partner == null)
    //            return (false, "Delivery partner not available");

    //        var orders = await _context.TblOrdernows
    //            .Where(o => o.TrackId == trackId && !o.IsDeleted)
    //            .ToListAsync();

    //        if (!orders.Any())
    //            return (false, "Order not found");

    //        foreach (var order in orders)
    //        {
    //            order.DeliveryPartnerId = partnerId;
    //            order.DvendorpickupTime = DateTime.UtcNow;
    //        }

    //        // Mark partner as busy
    //        partner.IsAvailable = false;

    //        await _context.SaveChangesAsync();
    //        return (true, "Order assigned successfully");
    //    }

    //    public async Task<List<DeliveryPartnerDto>> GetAvailablePartnersAsync(double latitude, double longitude)
    //    {
    //        var partners = await _context.TblDeliveryPartners
    //            .Where(p => p.IsActive && p.IsAvailable)
    //            .ToListAsync();

    //        var partnersWithLocation = new List<DeliveryPartnerDto>();

    //        foreach (var partner in partners)
    //        {
    //            var location = await _context.DeliveryPartnerLocations
    //                .FirstOrDefaultAsync(l => l.DeliveryPartnerId == partner.Id);

    //            if (location != null)
    //            {
    //                var distance = CalculateDistance(latitude, longitude, location.Latitude, location.Longitude);
    //                partnersWithLocation.Add(new DeliveryPartnerDto
    //                {
    //                    Id = partner.Id,
    //                    Name = partner.Name,
    //                    Phone = partner.Phone,
    //                    VehicleType = partner.VehicleType,
    //                    Rating = partner.Rating,
    //                    Distance = distance,
    //                    IsOnline = location.IsOnline && location.LastUpdated > DateTime.UtcNow.AddMinutes(-5)
    //                });
    //            }
    //        }

    //        return partnersWithLocation.OrderBy(p => p.Distance).ToList();
    //    }

    //    public async Task<RouteInfoDto> GetOptimizedRouteAsync(double sourceLat, double sourceLng, double destLat, double destLng)
    //    {
    //        // Integrate with Google Maps Directions API or similar
    //        var distance = CalculateDistance(sourceLat, sourceLng, destLat, destLng);
    //        var estimatedTime = (int)(distance * 3); // 3 minutes per km estimate

    //        return new RouteInfoDto
    //        {
    //            Distance = $"{distance:F1} km",
    //            EstimatedTime = $"{estimatedTime} mins",
    //            Source = new LocationDto { Latitude = sourceLat, Longitude = sourceLng },
    //            Destination = new LocationDto { Latitude = destLat, Longitude = destLng },
    //            GoogleMapsUrl = $"https://www.google.com/maps/dir/{sourceLat},{sourceLng}/{destLat},{destLng}"
    //        };
    //    }

    //    public async Task UpdatePartnerLocationAsync(int partnerId, double latitude, double longitude)
    //    {
    //        var location = await _context.DeliveryPartnerLocations
    //            .FirstOrDefaultAsync(l => l.DeliveryPartnerId == partnerId);

    //        if (location == null)
    //        {
    //            _context.DeliveryPartnerLocations.Add(new DeliveryPartnerLocation
    //            {
    //                DeliveryPartnerId = partnerId,
    //                Latitude = latitude,
    //                Longitude = longitude,
    //                LastUpdated = DateTime.UtcNow,
    //                IsOnline = true
    //            });
    //        }
    //        else
    //        {
    //            location.Latitude = latitude;
    //            location.Longitude = longitude;
    //            location.LastUpdated = DateTime.UtcNow;
    //            location.IsOnline = true;
    //        }

    //        await _context.SaveChangesAsync();
    //    }

    //    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    //    {
    //        // Haversine formula for calculating distance between two points
    //        var R = 6371; // Earth's radius in kilometers
    //        var dLat = ToRadians(lat2 - lat1);
    //        var dLon = ToRadians(lon2 - lon1);
    //        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
    //                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
    //                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
    //        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    //        return R * c;
    //    }

    //    private double ToRadians(double degrees) => degrees * (Math.PI / 180);
    //}

    // DTOs
    public class DeliveryPartnerDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string VehicleType { get; set; }
        public decimal Rating { get; set; }
        public double Distance { get; set; }
        public bool IsOnline { get; set; }
    }

    public class RouteInfoDto
    {
        public string Distance { get; set; }
        public string EstimatedTime { get; set; }
        public LocationDto Source { get; set; }
        public LocationDto Destination { get; set; }
        public string GoogleMapsUrl { get; set; }
    }

    public class LocationDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class DeliveryPartnerStatsDto
    {
        public int TotalDeliveries { get; set; }
        public decimal Rating { get; set; }
        public int DeliveriesThisMonth { get; set; }
        public decimal EarningsThisMonth { get; set; }
    }
}
