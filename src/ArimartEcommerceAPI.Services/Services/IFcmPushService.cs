using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Services.Services
{
    public interface IFcmPushService
    {
        Task<(bool success, string errorMessage)> SendNotificationAsync(string deviceToken, string title, string body);
    }

}
