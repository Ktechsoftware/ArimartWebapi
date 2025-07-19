namespace ArimartEcommerceAPI.Infrastructure.Data.DTO
{
    public class CartCheckoutRequest
    {
        public int Userid { get; set; }
        public int? Sipid { get; set; }
        public string Addid { get; set; } = string.Empty; // Comma-separated cart item IDs
    }
}
