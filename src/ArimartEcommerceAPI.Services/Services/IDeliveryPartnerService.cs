using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Services.Services
{
    public interface IDeliveryPartnerService
    {
        Task<(bool success, string message)> AssignOrderToPartnerAsync(string trackId, int partnerId);
        Task<List<DeliveryPartnerDto>> GetAvailablePartnersAsync(double latitude, double longitude);
        Task<RouteInfoDto> GetOptimizedRouteAsync(double sourceLat, double sourceLng, double destLat, double destLng);
        Task UpdatePartnerLocationAsync(int partnerId, double latitude, double longitude);
        Task<DeliveryPartnerStatsDto> GetPartnerStatsAsync(int partnerId);
    }
}
