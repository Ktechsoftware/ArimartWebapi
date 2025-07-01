public class AddToCartRequest
{
    public long Pid { get; set; }
    public long Pdid { get; set; }
    public int Qty { get; set; }
    public int? Userid { get; set; } // only for user cart
    public long? Groupid { get; set; } // only for guest cart
    public decimal Price { get; set; }  
}
