namespace ArimartEcommerceAPI.Infrastructure.Data.DTO
{
    public class AddWalletRequest
    {
        public long Userid { get; set; }
        public decimal Amount { get; set; }
    }

    public class DeductWalletRequest
    {
        public long Userid { get; set; }
        public decimal Amount { get; set; }
    }
}
