
using Hangfire.Dashboard;
namespace ArimartEcommerceAPI.API.Middleware
{
    public class AllowAllUsersAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context) => true;
    }

}
