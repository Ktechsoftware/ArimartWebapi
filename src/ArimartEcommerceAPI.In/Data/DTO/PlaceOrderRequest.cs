namespace ArimartEcommerceAPI.Infrastructure.Data.DTO
{
    public class PlaceOrderRequest
    {
        public int Qty { get; set; }
        public long Pid { get; set; }
        public long Pdid { get; set; }
        public int Userid { get; set; }
        public long? Groupid { get; set; }  // Optional
        public decimal? Deliveryprice { get; set; }
    }
}
