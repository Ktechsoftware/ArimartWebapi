using System.Linq;
using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.DTO;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using ArimartEcommerceAPI.Services.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly ApplicationDbContext _context;
        private readonly IFcmPushService _fcmPushService;

    public OrderController(ApplicationDbContext context, IFcmPushService fcmPushService)
    {
        _context = context;
        _fcmPushService = fcmPushService;
    }
    [HttpPost("checkout")]
    public async Task<IActionResult> CheckoutCart([FromBody] CartCheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Addid))
            return BadRequest(new { message = "Cart item IDs are required." });

        try
        {
            var cartIds = request.Addid
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => Convert.ToInt64(id.Trim()))
                .ToList();

            var cartItems = await _context.TblAddcarts
                .Where(c => cartIds.Contains(c.Id) && c.Qty > 0)
                .ToListAsync();

            if (cartItems.Count == 0)
                return BadRequest(new { message = "No valid cart items found for checkout." });

            var trackId = GenerateTrackId();
            await ValidateGroupOrders(cartItems);
            var newOrders = cartItems.Select(c => new TblOrdernow
            {
                Qty = c.Qty,
                Pid = c.Pid,
                Pdid = c.Pdid,
                Userid = request.Userid,
                Sipid = request.Sipid,
                Groupid = c.Groupid,
                Deliveryprice = c.Price,
                TrackId = trackId,
                AddedDate = DateTime.UtcNow,
                IsDeleted = false,
                IsActive = true
            }).ToList();

            await _context.TblOrdernows.AddRangeAsync(newOrders);
            await _context.SaveChangesAsync();

            // Promo usage
            if (!string.IsNullOrEmpty(request.PromoCode))
            {
                var promo = await _context.TblPromocodes.FirstOrDefaultAsync(p => p.Code == request.PromoCode);
                if (promo != null)
                {
                    foreach (var order in newOrders)
                    {
                        _context.TblPromocodeUsages.Add(new TblPromocodeUsage
                        {
                            PromoId = promo.PromoId,
                            UserId = request.Userid,
                            UsedAt = DateTime.Now,
                            OrderId = order.Id
                        });
                    }
                }
            }

            // Update cart
            foreach (var item in cartItems)
            {
                if (item.Groupid != null)
                {
                    item.IsDeleted = false;
                    item.Qty = 0;
                }
                else
                {
                    item.IsDeleted = true;
                }
                item.ModifiedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // FCM
            var fcmToken = await _context.FcmDeviceTokens
                .Where(t => t.UserId == request.Userid)
                .Select(t => t.Token)
                .FirstOrDefaultAsync();

            // 🔔 Send FCM Notification
            string fcmStatus = "FCM not sent";

            try
            {
                if (!string.IsNullOrEmpty(fcmToken))
                {
                    var (success, error) = await _fcmPushService.SendNotificationAsync(
                        fcmToken,
                        "Order Placed ✅",
                        $"Your order with ID {trackId} has been successfully placed!"
                    );

                    fcmStatus = success ? "✅ FCM sent successfully." : $"❌ FCM failed: {error}";
                }
                else
                {
                    fcmStatus = "❌ FCM token not found for user.";
                }
            }
            catch (Exception ex)
            {
                fcmStatus = $"❌ FCM exception: {ex.Message}";
            }


            // 👇 Return everything inside a single "message" string
            return Ok(new
            {
                message = $"Checkout successful ✅\nOrder ID: {trackId}\n{fcmStatus}",
                orderid = trackId
            });

        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Checkout failed.",
                error = ex.Message
            });
        }
    }


    // POST: api/order/place
    [HttpPost("place")]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        if (request.Qty <= 0)
            return BadRequest(new { message = "Quantity must be greater than 0." });

        var trackId = GenerateTrackId();

        var order = new TblOrdernow
        {
            Qty = request.Qty,
            Pid = request.Pid,
            Pdid = request.Pdid,
            Userid = request.Userid,
            Groupid = null,
            Deliveryprice = request.Deliveryprice,
            TrackId = trackId,
            AddedDate = DateTime.UtcNow,
            IsDeleted = false,
            IsActive = true
        };

        await _context.TblOrdernows.AddAsync(order);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Order placed successfully.", orderid = trackId });
    }

    // POST: api/order/place/group
    [HttpPost("place/group")]
    public async Task<IActionResult> PlaceGroupOrder([FromBody] PlaceOrderRequest request)
    {
        if (request.Qty <= 0)
            return BadRequest(new { message = "Quantity must be greater than 0." });

        if (!request.Groupid.HasValue)
            return BadRequest(new { message = "Group ID is required for group order." });

        var order = new TblOrdernow
        {
            Qty = request.Qty,
            Pid = request.Pid,
            Pdid = request.Pdid,
            Userid = request.Userid,
            Groupid = request.Groupid,
            Deliveryprice = request.Deliveryprice,
        };

        await _context.TblOrdernows.AddAsync(order);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Group order placed successfully." });
    }

    // GET: api/order/history/{userid}
    [HttpGet("history/{userid}")]
    public async Task<IActionResult> GetOrderHistory(int userid, [FromQuery] string status = null)
    {
        var baseQuery = from order in _context.TblOrdernows
                        where order.Userid == userid
                        join p in _context.VwProducts on (int?)order.Pdid equals (int?)p.Pdid into pJoin
                        from p in pJoin.DefaultIfEmpty()
                        join c in _context.TblCategories on (p != null ? p.Categoryid : (int?)null) equals (int?)c.Id into cJoin
                        from c in cJoin.DefaultIfEmpty()
                        join sc in _context.TblSubcategories on (p != null ? p.Subcategoryid : (int?)null) equals (int?)sc.Id into scJoin
                        from sc in scJoin.DefaultIfEmpty()
                        join cc in _context.TblChildSubcategories on (p != null ? p.ChildCategoryId : (int?)null) equals (int?)cc.Id into ccJoin
                        from cc in ccJoin.DefaultIfEmpty()
                        select new
                        {
                            order.TrackId,
                            order.Id,
                            order.Groupid,
                            order.AddedDate,
                            order.Qty,
                            order.Deliveryprice,
                            ProductName = p != null ? p.ProductName : null,
                            ProductImage = p != null ? p.Image : null,
                            Unit = p != null ? p.Wtype : null,
                            GroupCode = p != null ? p.GroupCode : null,
                            CategoryName = c != null ? c.CategoryName : null,
                            SubCategoryName = sc != null ? sc.SubcategoryName : null,
                            ChildCategoryName = cc != null ? cc.ChildcategoryName : null,

                            Status = order.IsDeleted ? "Canceled"
                                    : order.Groupid != null ? GetGroupOrderStatus(order.Groupid.Value, order)
                                    : order.DdeliverredidTime != null ? "Delivered"
                                    : order.ShipOrderidTime != null ? "Shipped"
                                    : order.DvendorpickupTime != null ? "Picked Up"
                                    : order.DassignidTime != null ? "Assigned"
                                    : "Placed"
                        };

        if (!string.IsNullOrWhiteSpace(status))
        {
            baseQuery = baseQuery.Where(o => o.Status == status);
        }

        var orders = await baseQuery
            .OrderByDescending(o => o.AddedDate)
            .ToListAsync();

        var groupedOrders = orders
            .GroupBy(o => o.TrackId)
            .Select(g => new
            {
                TrackId = g.Key,
                OrderDate = g.First().AddedDate,
                TotalItems = g.Count(),
                TotalAmount = g.Sum(o => o.Deliveryprice * o.Qty),
                Status = g.First().Status,
                Items = g.Select(o => new
                {
                    o.Id,
                    o.Qty,
                    o.Groupid,
                    o.GroupCode,
                    o.Unit,
                    o.Deliveryprice,
                    o.ProductName,
                    o.ProductImage,
                    o.CategoryName,
                    o.SubCategoryName,
                    o.ChildCategoryName,
                    o.Status
                }).ToList()
            })
            .OrderByDescending(o => o.OrderDate)
            .ToList();

        return Ok(groupedOrders);
    }


    [HttpGet("history/{userid}/{groupid}")]
    public async Task<IActionResult> GetGroupOrderHistory(int userid, long groupid)
    {
        var orders = from order in _context.TblOrdernows
                     where order.Userid == userid
                           && order.Groupid == groupid
                     join p in _context.VwProducts on (int?)order.Pdid equals (int?)p.Pdid into pJoin
                     from p in pJoin.DefaultIfEmpty()
                     join c in _context.TblCategories on (p != null ? p.Categoryid : (int?)null) equals (int?)c.Id into cJoin
                     from c in cJoin.DefaultIfEmpty()
                     join sc in _context.TblSubcategories on (p != null ? p.Subcategoryid : (int?)null) equals (int?)sc.Id into scJoin
                     from sc in scJoin.DefaultIfEmpty()
                     join cc in _context.TblChildSubcategories on (p != null ? p.ChildCategoryId : (int?)null) equals (int?)cc.Id into ccJoin
                     from cc in ccJoin.DefaultIfEmpty()
                     orderby order.AddedDate descending
                     select new
                     {
                         order.TrackId,
                         order.Id,
                         order.AddedDate,
                         order.Qty,
                         order.Deliveryprice,
                         ProductName = p != null ? p.ProductName : null,
                         ProductImage = p != null ? p.Image : null,
                         Unit = p != null ? p.Wtype : null,
                         GroupCode = p != null ? p.GroupCode : null,
                         CategoryName = c != null ? c.CategoryName : null,
                         SubCategoryName = sc != null ? sc.SubcategoryName : null,
                         ChildCategoryName = cc != null ? cc.ChildcategoryName : null,
                         Status = order.IsDeleted ? "Canceled"
                                    : order.DdeliverredidTime != null ? "Delivered"
                                    : order.ShipOrderidTime != null ? "Shipped"
                                    : order.DvendorpickupTime != null ? "Picked Up"
                                    : order.DassignidTime != null ? "Assigned"
                                    : "Placed"
                     };

        var result = await orders.ToListAsync();
        return Ok(result);
    }

    // 🔥 FIXED: Track Order - Now returns ALL items with same track ID
    [HttpGet("track/{trackId}")]
    public async Task<IActionResult> TrackOrder(string trackId)
    {
        var orders = await (
            from order in _context.TblOrdernows
            join p in _context.VwProducts on (int?)order.Pdid equals (int?)p.Pdid into pJoin
            from p in pJoin.DefaultIfEmpty()
            join c in _context.TblCategories on (p != null ? p.Categoryid : (int?)null) equals (int?)c.Id into cJoin
            from c in cJoin.DefaultIfEmpty()
            join sc in _context.TblSubcategories on (p != null ? p.Subcategoryid : (int?)null) equals (int?)sc.Id into scJoin
            from sc in scJoin.DefaultIfEmpty()
            join cc in _context.TblChildSubcategories on (p != null ? p.ChildCategoryId : (int?)null) equals (int?)cc.Id into ccJoin
            from cc in ccJoin.DefaultIfEmpty()
            join pu in _context.TblPromocodeUsages on order.Id equals pu.OrderId into puJoin
            from pu in puJoin.DefaultIfEmpty()
            join promo in _context.TblPromocodes on pu.PromoId equals promo.PromoId into promoJoin
            from promo in promoJoin.DefaultIfEmpty()

            where order.TrackId == trackId && !order.IsDeleted
            select new
            {
                order.TrackId,
                order.Id,
                order.Pid,
                order.Pdid,
                order.Qty,
                order.Groupid,
                order.Deliveryprice,
                order.AddedDate,
                order.DassignidTime,
                order.DvendorpickupTime,
                order.ShipOrderidTime,
                order.DdeliverredidTime,
                order.DuserassginidTime,

                Status = order.DdeliverredidTime != null ? "Delivered"
                        : order.ShipOrderidTime != null ? "Shipped"
                        : order.DvendorpickupTime != null ? "Picked Up"
                        : order.DassignidTime != null ? "Assigned"
                        : "Placed",

                ProductDetails = new
                {
                    Id = p != null ? p.Id : (long?)null,
                    Name = p != null ? p.ProductName : null,
                    Description = p != null ? p.Longdesc : null,
                    Image = p != null ? p.Image : null,
                    Price = p != null ? p.Price : null,
                    Unit = p != null ? p.Wtype : null,
                    Weight = p != null ? p.Wweight : null,
                    Groupcode = p != null ? p.GroupCode : null,
                },
                AppliedPromo = promo != null ? new
                {
                    promo.Code,
                    promo.Description,
                    promo.DiscountType,
                    promo.DiscountValue,
                    promo.MaxDiscount
                } : null,

                Category = new
                {
                    Name = c != null ? c.CategoryName : null
                },

                SubCategory = new
                {
                    Name = sc != null ? sc.SubcategoryName : null
                },

                ChildCategory = new
                {
                    Name = cc != null ? cc.ChildcategoryName : null
                }
            })
            .ToListAsync(); // 🔥 Changed from FirstOrDefaultAsync to ToListAsync

        if (!orders.Any())
            return NotFound(new { message = "Order not found." });

        // Return structured response with order summary and all items
        var result = new
        {
            TrackId = trackId,
            OrderDate = orders.First().AddedDate,
            TotalItems = orders.Count,
            TotalAmount = orders.Sum(o => o.Deliveryprice * o.Qty),
            OverallStatus = orders.First().Status,
            AppliedPromo = orders.First().AppliedPromo,
            Items = orders.Select(order => new
            {
                order.Id,
                order.Pid,
                order.Pdid,
                order.Qty,
                order.Groupid,
                order.Deliveryprice,
                order.Status,
                order.DassignidTime,
                order.DvendorpickupTime,
                order.ShipOrderidTime,
                order.DdeliverredidTime,
                order.DuserassginidTime,
                order.ProductDetails,
                order.Category,
                order.SubCategory,
                order.ChildCategory
            }).ToList()
        };

        return Ok(result);
    }

    [HttpDelete("{orderid}")]
    public async Task<IActionResult> CancelOrder(long orderid)
    {
        var order = await _context.TblOrdernows.FirstOrDefaultAsync(o => o.Id == orderid && !o.IsDeleted);
        if (order == null)
            return NotFound(new { message = "Order not found or already deleted." });

        order.IsDeleted = true;
        order.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Order cancelled successfully." });
    }

    // 🔥 NEW: Cancel entire order by Track ID (all items)
    [HttpDelete("track/{trackId}")]
    public async Task<IActionResult> CancelOrderByTrackId(string trackId)
    {
        var orders = await _context.TblOrdernows
            .Where(o => o.TrackId == trackId && !o.IsDeleted)
            .ToListAsync();

        if (!orders.Any())
            return NotFound(new { message = "Order not found or already deleted." });

        foreach (var order in orders)
        {
            order.IsDeleted = true;
            order.ModifiedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Order {trackId} cancelled successfully. {orders.Count} items cancelled." });
    }

    private async Task ValidateGroupOrders(List<TblAddcart> cartItems)
    {
        foreach (var item in cartItems.Where(i => i.Groupid.HasValue))
        {
            var group = await _context.VwGroups
                .FirstOrDefaultAsync(g => g.Gid == item.Groupid && g.IsDeleted==false);

            if (group == null || group.EventSend1 <= DateTime.UtcNow)
            {
                // Remove expired group item
                item.IsDeleted = true;
                item.ModifiedDate = DateTime.UtcNow;
            }
        }

        // Remove expired items from processing
        cartItems.RemoveAll(i => i.IsDeleted);
        await _context.SaveChangesAsync();
    }

    // Check if a group has expired (time limit passed)
    private bool IsGroupExpired(long groupId)
    {
        var group = _context.VwGroups
            .FirstOrDefault(g => g.Gid == groupId && g.IsDeleted == false);

        if (group == null) return true; // Group not found = consider expired

        return group.EventSend1 <= DateTime.UtcNow; // Check if end time has passed
    }

    // Check if a group is complete (enough members joined)
    private bool IsGroupComplete(long groupId)
    {
        var group = _context.VwGroups
            .FirstOrDefault(g => g.Gid == groupId && g.IsDeleted == false);

        if (group == null) return false; // Group not found = not complete

        // Get required number of members
        int required = int.TryParse(group.Gqty, out var qty) ? qty : 0;
        if (required <= 0) return false;

        // Count current members
        int joined = _context.TblGroupjoins
            .Count(j => j.Groupid == groupId && !j.IsDeleted && j.IsActive == true);

        return joined >= required; // Complete if enough members joined
    }

    // Optional: Combined method for efficiency
    private (bool isExpired, bool isComplete) GetGroupStatus(long groupId)
    {
        var group = _context.VwGroups
            .FirstOrDefault(g => g.Gid == groupId && g.IsDeleted == false);

        if (group == null) return (true, false); // Not found = expired, not complete

        bool isExpired = group.EventSend1 <= DateTime.UtcNow;

        int required = int.TryParse(group.Gqty, out var qty) ? qty : 0;
        int joined = _context.TblGroupjoins
            .Count(j => j.Groupid == groupId && !j.IsDeleted && j.IsActive == true);

        bool isComplete = joined >= required;

        return (isExpired, isComplete);
    }

    private string GetGroupOrderStatus(long groupId, dynamic order)
    {
        var (isExpired, isComplete) = GetGroupStatus(groupId);

        if (isExpired && !isComplete)
            return "Group Expired - Reorder Available";

        if (!isExpired && !isComplete)
            return "Waiting for Group Members";

        if (isComplete)
        {
            // Group is complete, check normal order status
            return order.DdeliverredidTime != null ? "Delivered"
                 : order.ShipOrderidTime != null ? "Shipped"
                 : order.DvendorpickupTime != null ? "Picked Up"
                 : order.DassignidTime != null ? "Assigned"
                 : "Group Complete - Processing";
        }

        return "Processing"; // Fallback
    }
    private string GenerateTrackId()
    {
        string datePart = DateTime.UtcNow.ToString("ddHHmm"); // e.g., "221540"
        string randomPart = Guid.NewGuid().ToString("N").Substring(0, 2).ToUpper(); // e.g., "A9"

        return $"ORD-{datePart}{randomPart}"; // e.g., "ORD-221540A9" (8 characters after ORD-)
    }


    // ================================= Delivery Partner =================================
    // 1. QR/Barcode Scan Endpoint
    [HttpPost("delivery/scan")]
    public async Task<IActionResult> ScanOrderForDelivery([FromBody] ScanOrderRequest request)
    {
        try
        {
            var order = await _context.TblOrdernows
                .Where(o => o.TrackId == request.TrackId && !o.IsDeleted)
                .Include(o => o.Product)
                .FirstOrDefaultAsync();

            if (order == null)
                return NotFound(new { message = "Order not found." });

            if (order.DassignidTime != null)
                return BadRequest(new { message = "Order not yet assigned for delivery." });

            if (order.DeliveryPartnerId != null)
                return BadRequest(new { message = "Order already picked up by another delivery partner." });

            // Assign to delivery partner
            order.DeliveryPartnerId = request.DeliveryPartnerId;
            order.DvendorpickupTime = DateTime.UtcNow;
            order.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Get customer address details
            var addressDetails = await GetDeliveryAddressDetails(order.Userid, order.Sipid);

            return Ok(new
            {
                message = "Order assigned successfully for delivery",
                orderDetails = new
                {
                    order.TrackId,
                    order.Id,
                    order.Qty,
                    order.Deliveryprice,
                    productId = order.Pid,
                    CustomerName = addressDetails.CustomerName,
                    DeliveryAddress = addressDetails.Address,
                    CustomerPhone = addressDetails.Phone,
                    PickupLocation = await GetPickupLocation(), // Vendor/warehouse location
                    DeliveryLocation = new
                    {
                        addressDetails.Latitude,
                        addressDetails.Longitude,
                        addressDetails.Address
                    },
                    EstimatedDistance = CalculateDistance(
                        await GetPickupLocation(),
                        new { addressDetails.Latitude, addressDetails.Longitude }
                    )
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Scan failed", error = ex.Message });
        }
    }

    // 2. Get Delivery Route/Map Data
    [HttpGet("delivery/route/{trackId}")]
    public async Task<IActionResult> GetDeliveryRoute(string trackId, [FromQuery] int deliveryPartnerId)
    {
        var order = await _context.TblOrdernows
            .FirstOrDefaultAsync(o => o.TrackId == trackId && o.DeliveryPartnerId == deliveryPartnerId);

        if (order == null)
            return NotFound(new { message = "Order not found or not assigned to you." });

        var addressDetails = await GetDeliveryAddressDetails(order.Userid, order.Sipid);
        var pickupLocation = await GetPickupLocation();

        return Ok(new
        {
            route = new
            {
                source = pickupLocation,
                destination = new
                {
                    latitude = addressDetails.Latitude,
                    longitude = addressDetails.Longitude,
                    address = addressDetails.Address
                },
                waypoints = new object[0], // Add waypoints if needed
                estimatedTime = "15-20 mins", // Calculate based on distance
                estimatedDistance = CalculateDistance(pickupLocation, new { addressDetails.Latitude, addressDetails.Longitude })
            },
            orderInfo = new
            {
                order.TrackId,
                customerName = addressDetails.CustomerName,
                customerPhone = addressDetails.Phone,
                totalAmount = order.Deliveryprice * order.Qty,
                paymentMethod = "COD", // Add payment method to order model
                specialInstructions = addressDetails.Instructions
            }
        });
    }

    // 3. Update Delivery Status with Location
    [HttpPost("delivery/update-status")]
    public async Task<IActionResult> UpdateDeliveryStatus([FromBody] DeliveryStatusRequest request)
    {
        var order = await _context.TblOrdernows
            .FirstOrDefaultAsync(o => o.TrackId == request.TrackId && o.DeliveryPartnerId == request.DeliveryPartnerId);

        if (order == null)
            return NotFound(new { message = "Order not found or not assigned to you." });

        switch (request.Status.ToLower())
        {
            case "shipped":
                order.ShipOrderidTime = DateTime.UtcNow;
                break;
            case "delivered":
                order.DdeliverredidTime = DateTime.UtcNow;
                // Add delivery proof if provided
                if (!string.IsNullOrEmpty(request.DeliveryProofImage))
                {
                    order.DeliveryProofImage = request.DeliveryProofImage;
                }
                break;
            default:
                return BadRequest(new { message = "Invalid status." });
        }

        // Update delivery partner location
        if (request.CurrentLatitude.HasValue && request.CurrentLongitude.HasValue)
        {
            await UpdateDeliveryPartnerLocation(request.DeliveryPartnerId,
                request.CurrentLatitude.Value, request.CurrentLongitude.Value);
        }

        order.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Send FCM to customer
        await SendDeliveryUpdateToCustomer(order.Userid, order.TrackId, request.Status);

        return Ok(new { message = $"Order status updated to {request.Status}" });
    }

    // 4. Get Active Deliveries for Partner - Enhanced with full customer details
    [HttpGet("delivery/active/{deliveryPartnerId}")]
    public async Task<IActionResult> GetActiveDeliveries(int deliveryPartnerId)
    {
        // ✅ First step: fetch raw data (only DB mappable fields)
        var rawOrders = await (
            from order in _context.TblOrdernows
            join user in _context.TblUsers on order.Userid equals (int?)user.Id into userJoin
            from user in userJoin.DefaultIfEmpty()
            join shipping in _context.TblShipings on order.Sipid equals (int?)shipping.Id into shipJoin
            from shipping in shipJoin.DefaultIfEmpty()
            join product in _context.VwProducts on order.Pdid equals (int?)product.Pdid into prodJoin
            from product in prodJoin.DefaultIfEmpty()
            join category in _context.TblCategories on (product != null ? (int?)product.Categoryid : null)
        equals (int?)category.Id into catJoin
            from category in catJoin.DefaultIfEmpty()

            where order.DeliveryPartnerId == deliveryPartnerId
                  && order.DvendorpickupTime != null
                  && order.DdeliverredidTime == null
                  && !order.IsDeleted

            orderby order.AddedDate descending

            select new
            {
                order,
                user,
                shipping,
                product,
                category
            }
        ).ToListAsync();

        // ✅ Second step: use C# methods & complex projections in memory
        var pickupLocation = GetPickupLocationSync();

        var activeOrders = rawOrders.Select(x => new
        {
            // Order Info
            x.order.TrackId,
            x.order.Id,
            OrderDate = x.order.AddedDate,
            x.order.Qty,
            x.order.Deliveryprice,
            TotalAmount = x.order.Deliveryprice * x.order.Qty,
            PaymentMethod = "COD",

            // Order Status
            Status = x.order.ShipOrderidTime != null ? "In Transit" : "Picked Up",
            PickedUpTime = x.order.DvendorpickupTime,
            ShippedTime = x.order.ShipOrderidTime,
            EstimatedDeliveryTime = x.order.AddedDate.AddHours(2),

            // Customer Info
            Customer = new
            {
                Id = x.user?.Id ?? 0,
                Name = x.user?.ContactPerson ?? "Unknown Customer",
                Phone = x.user?.Phone ?? "N/A",
                Email = x.user?.Email ?? "N/A",
                ProfileImage = x.user?.Image ?? "default-avatar.png" // Simple fallback
            },

            // Shipping Info
            ShippingAddress = new
            {
                Id = x.shipping?.Id ?? 0,
                ContactPerson = x.shipping?.ContactPerson ?? "N/A",
                Phone = x.shipping?.Phone ?? "N/A",
                Email = x.shipping?.Email ?? "N/A",
                FullAddress = x.shipping != null
                    ? $"{x.shipping.Address}, {x.shipping.City}, {x.shipping.State} - {x.shipping.PostalCode}"
                    : "Address not available",
                Address = x.shipping?.Address ?? "N/A",
                City = x.shipping?.City ?? "N/A",
                State = x.shipping?.State ?? "N/A",
                PostalCode = x.shipping?.PostalCode ?? "N/A",
                Country = x.shipping?.Country ?? "India",
                Latitude = x.shipping?.Latitude ?? 0.0,
                Longitude = x.shipping?.Longitude ?? 0.0,
                Instructions = x.shipping?.Instructions ?? "",
                VendorName = x.shipping?.VendorName ?? ""
            },

            // Product Info
            Product = new
            {
                Id = x.product?.Id ?? 0,
                Name = x.product?.ProductName ?? "Product not found",
                Description = x.product?.Shortdesc ?? "",
                Image = x.product?.Image ?? "",
                Price = x.product?.Price ?? "",
                Unit = x.product?.Wtype ?? "",
                Weight = x.product?.Wweight ?? "",
                Category = x.category?.CategoryName ?? "Unknown",
                GroupCode = x.product?.GroupCode ?? ""
            },

            // Delivery Info
            DeliveryInfo = new
            {
                PickupLocation = pickupLocation,
                DeliveryLocation = x.shipping != null
                    ? new
                    {
                        Latitude = x.shipping.Latitude ?? 0.0,
                        Longitude = x.shipping.Longitude ?? 0.0,
                        Address = $"{x.shipping.Address}, {x.shipping.City}"
                    }
                    : null,
                EstimatedDistance = x.shipping != null
                    ? CalculateDistanceSync(pickupLocation, x.shipping.Latitude ?? 0.0, x.shipping.Longitude ?? 0.0)
                    : "Unknown",
                EstimatedTime = "15-30 mins",
                Priority = GetDeliveryPriority(x.order.AddedDate),
                DeliveryWindow = GetDeliveryWindow(x.order.AddedDate)
            },

            // Extras
            SpecialInstructions = x.shipping?.Instructions ?? "",
            IsFragile = x.product != null &&
                       (x.product.ProductName.ToLower().Contains("glass") ||
                        x.product.ProductName.ToLower().Contains("fragile")),
            IsCOD = true,
            CustomerRating = GetCustomerRating(x.user?.Id ?? 0),
            DeliveryNotes = x.order.DeliveryNotes ?? ""
        }).ToList();

        // ✅ Grouped by customer
        var groupedByCustomer = activeOrders
            .GroupBy(o => o.Customer.Id)
            .Select(g => new
            {
                CustomerInfo = g.First().Customer,
                TotalOrders = g.Count(),
                TotalAmount = g.Sum(o => o.TotalAmount),
                Orders = g.ToList()
            })
            .ToList();

        // ✅ Summary
        var summary = new
        {
            TotalActiveDeliveries = activeOrders?.Count,
            TotalCustomers = groupedByCustomer?.Count,
            TotalRevenue = activeOrders.Sum(o => o.TotalAmount),
            PickedUpCount = activeOrders.Count(o => o.Status == "Picked Up"),
            InTransitCount = activeOrders.Count(o => o.Status == "In Transit"),
            UrgentDeliveries = activeOrders.Count(o => o.DeliveryInfo.Priority == "Urgent"),
            CODAmount = activeOrders.Where(o => o.IsCOD).Sum(o => o.TotalAmount),
            AverageOrderValue = activeOrders.Any() ? activeOrders.Average(o => o.TotalAmount) : 0
        };

        return Ok(new
        {
            summary,
            deliveries = activeOrders,
            groupedByCustomer,
            message = $"Found {activeOrders?.Count} active deliveries for partner {deliveryPartnerId}"
        });
    }

    // Helper method for synchronous pickup location
    private dynamic GetPickupLocationSync()
    {
        return new
        {
            Latitude = 28.6139, // Your warehouse coordinates
            Longitude = 77.2090,
            Address = "Arimart Warehouse, Delhi",
            ContactPhone = "+91-9876543210",
            WorkingHours = "9:00 AM - 9:00 PM"
        };
    }

    // Helper method for synchronous distance calculation
    private string CalculateDistanceSync(dynamic pickup, double destLat, double destLng)
    {
        // Simple Haversine formula implementation
        var R = 6371; // Earth's radius in km
        var dLat = ToRadians(destLat - pickup.Latitude);
        var dLng = ToRadians(destLng - pickup.Longitude);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(pickup.Latitude)) * Math.Cos(ToRadians(destLat)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = R * c;

        return $"{distance:F1} km";
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    // Helper method to determine delivery priority
    private string GetDeliveryPriority(DateTime orderDate)
    {
        var hoursSinceOrder = (DateTime.UtcNow - orderDate).TotalHours;
        return hoursSinceOrder switch
        {
            > 4 => "Urgent",
            > 2 => "High",
            _ => "Normal"
        };
    }

    // Helper method to determine delivery window
    private string GetDeliveryWindow(DateTime orderDate)
    {
        var deliveryTime = orderDate.AddHours(2); // Estimated delivery time
        var hour = deliveryTime.Hour;

        return hour switch
        {
            >= 6 and < 12 => "Morning (6 AM - 12 PM)",
            >= 12 and < 17 => "Afternoon (12 PM - 5 PM)",
            >= 17 and < 21 => "Evening (5 PM - 9 PM)",
            _ => "Night (9 PM - 6 AM)"
        };
    }

    // Helper method to get customer rating
    private decimal GetCustomerRating(long customerId)
    {
        if (customerId == 0) return 0;

        var ratings = _context.DeliveryRatings
            .Where(r => r.CustomerId == customerId);

        if (!ratings.Any())
            return 5.0m; // default rating

        return ratings.Average(r => r.Rating);
    }


    // 5. Real-time Location Tracking
    [HttpPost("delivery/location")]
    public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateRequest request)
    {
        await UpdateDeliveryPartnerLocation(request.DeliveryPartnerId,
            request.Latitude, request.Longitude);

        return Ok(new { message = "Location updated successfully" });
    }

    // Helper Methods
    private async Task<dynamic> GetDeliveryAddressDetails(int? userId, int? sipId)
    {
        // Query your shipping address table
        var address = await _context.TblShipings
            .FirstOrDefaultAsync(a => a.Userid == userId && a.Id == sipId);

        return new
        {
            CustomerName = address?.VendorName ?? "Customer",
            Phone = address?.Phone ?? "",
            Address = $"{address?.Address}, {address?.City}, {address?.State} - {address?.PostalCode}",
            Latitude = address?.Latitude ?? 0.0,
            Longitude = address?.Longitude ?? 0.0,
            Instructions = address?.Instructions ?? ""
        };
    }

    private async Task<dynamic> GetPickupLocation()
    {
        // Return your warehouse/store location
        return new
        {
            Latitude = 28.6139, // Example: Delhi coordinates
            Longitude = 77.2090,
            Address = "Warehouse, Delhi"
        };
    }

    private string CalculateDistance(dynamic source, dynamic destination)
    {
        // Implement distance calculation using Haversine formula or use Google Distance Matrix API
        return "5.2 km"; // Placeholder
    }

    private async Task UpdateDeliveryPartnerLocation(int partnerId, double lat, double lng)
    {
        var location = await _context.DeliveryPartnerLocations
            .FirstOrDefaultAsync(l => l.DeliveryPartnerId == partnerId);

        if (location == null)
        {
            _context.DeliveryPartnerLocations.Add(new DeliveryPartnerLocation
            {
                DeliveryPartnerId = partnerId,
                Latitude = lat,
                Longitude = lng,
                LastUpdated = DateTime.UtcNow
            });
        }
        else
        {
            location.Latitude = lat;
            location.Longitude = lng;
            location.LastUpdated = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private async Task SendDeliveryUpdateToCustomer(int? userId, string trackId, string status)
    {
        var fcmToken = await _context.FcmDeviceTokens
            .Where(t => t.UserId == userId)
            .Select(t => t.Token)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrEmpty(fcmToken))
        {
            var title = status.ToLower() == "delivered" ? "Order Delivered! 🎉" : "Order Update 📦";
            var body = $"Your order {trackId} is now {status.ToLower()}.";

            await _fcmPushService.SendNotificationAsync(fcmToken, title, body);
        }
    }


    public class ScanOrderRequest
    {
        public string TrackId { get; set; }
        public int DeliveryPartnerId { get; set; }
    }

    public class DeliveryStatusRequest
    {
        public string TrackId { get; set; }
        public int DeliveryPartnerId { get; set; }
        public string Status { get; set; } // "shipped", "delivered"
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
        public string? DeliveryProofImage { get; set; }
    }

    public class LocationUpdateRequest
    {
        public int DeliveryPartnerId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class ShippingAddressDto
    {
        public long Id { get; set; }
        public string ContactPerson { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string FullAddress { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Instructions { get; set; }
        public string VendorName { get; set; }
    }

    public class ProductDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public string Price { get; set; }   // better than string
        public string Unit { get; set; }
        public string Weight { get; set; }
        public string Category { get; set; }
        public string GroupCode { get; set; }
    }

    public class ApplyPromoRequest
    {
        public string? Code { get; set; }
        public int UserId { get; set; }
        public decimal OrderAmount { get; set; }
        public int? OrderId { get; set; }
    }
}