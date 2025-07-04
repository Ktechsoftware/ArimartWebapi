namespace ArimartEcommerceAPI.Infrastructure.Data.DTO
{
    public class CartCheckoutRequest
    {
        public int Userid { get; set; }
        public string Sipid { get; set; } = string.Empty;
        public string Addid { get; set; } = string.Empty; // Comma-separated cart item IDs
    }
}
