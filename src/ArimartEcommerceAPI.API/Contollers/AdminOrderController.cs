// ================================= ADMIN ORDER MANAGEMENT CONTROLLER =================================
using System;
using System.Linq;
using System.Threading.Tasks;
using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using ArimartEcommerceAPI.Services.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

//namespace ArimartEcommerceAPI.Controllers
//{
//    [Route("api/admin/[controller]")]
//    [ApiController]
//    public class AdminOrderController : ControllerBase
//    {
//        private readonly ApplicationDbContext _context;

//        public AdminOrderController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        // 🔹 GET: All Active Orders (Placed but not completed)
//        [HttpGet("active")]
//        public async Task<IActionResult> GetActiveOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
//        {
//            var query = from order in _context.TblOrdernows
//                        join user in _context.TblUsers on order.Userid equals user.Id into userJoin
//                        from user in userJoin.DefaultIfEmpty()
//                        join shipping in _context.TblShipings on order.Sipid equals shipping.Id into shipJoin
//                        from shipping in shipJoin.DefaultIfEmpty()
//                        join product in _context.VwProducts on order.Pdid equals product.Pdid into prodJoin
//                        from product in prodJoin.DefaultIfEmpty()
//                        join category in _context.TblCategories on product.Categoryid equals category.Id into catJoin
//                        from category in catJoin.DefaultIfEmpty()
//                        join subcategory in _context.TblSubcategories on product.Subcategoryid equals subcategory.Id into subJoin
//                        from subcategory in subJoin.DefaultIfEmpty()
//                        join childcat in _context.TblChildSubcategories on product.ChildCategoryId equals childcat.Id into childJoin
//                        from childcat in childJoin.DefaultIfEmpty()
//                        join delivery in _context.TblDeliveryusers on order.DeliveryPartnerId equals delivery.Id into delJoin
//                        from delivery in delJoin.DefaultIfEmpty()
//                        where !order.IsDeleted
//                              && order.DdeliverredidTime == null  // Not delivered = Active
//                        select new
//                        {
//                            // Order Details
//                            OrderId = order.Id,
//                            TrackId = order.TrackId,
//                            OrderDate = order.AddedDate,
//                            Quantity = order.Qty,
//                            DeliveryPrice = order.Deliveryprice,
//                            TotalAmount = order.Deliveryprice * order.Qty,
//                            GroupId = order.Groupid,

//                            // Order Status & Timeline
//                            Status = order.DdeliverredidTime != null ? "Delivered"
//                                   : order.ShipOrderidTime != null ? "Shipped"
//                                   : order.DvendorpickupTime != null ? "Picked Up"
//                                   : order.DassignidTime != null ? "Assigned"
//                                   : "Placed",

//                            PlacedAt = order.AddedDate,
//                            AssignedAt = order.DassignidTime,
//                            PickedUpAt = order.DvendorpickupTime,
//                            ShippedAt = order.ShipOrderidTime,
//                            DeliveredAt = order.DdeliverredidTime,

//                            // Customer Details
//                            Customer = new
//                            {
//                                UserId = user.Id,
//                                Name = user.FirstName + " " + user.LastName,
//                                Email = user.Email,
//                                Phone = user.Mobile,
//                                RegisteredDate = user.CreatedDate,
//                                IsActive = user.IsActive,
//                                ProfileImage = user.ProfileImage
//                            },

//                            // Detailed Shipping Address
//                            ShippingAddress = shipping != null ? new
//                            {
//                                AddressId = shipping.Id,
//                                VendorName = shipping.VendorName,
//                                ContactPerson = shipping.ContactPerson,
//                                Email = shipping.Email,
//                                Phone = shipping.Phone,
//                                FullAddress = shipping.Address,
//                                City = shipping.City,
//                                State = shipping.State,
//                                PostalCode = shipping.PostalCode,
//                                Country = shipping.Country,
//                                Latitude = shipping.Latitude,
//                                Longitude = shipping.Longitude,
//                                Instructions = shipping.Instructions,
//                                AddressType = shipping.AddressType,
//                                IsDefault = shipping.IsDefault
//                            } : null,

//                            // Product Details
//                            Product = product != null ? new
//                            {
//                                ProductId = product.Id,
//                                Name = product.ProductName,
//                                Description = product.Longdesc,
//                                ShortDesc = product.Shortdesc,
//                                Image = product.Image,
//                                Price = product.Price,
//                                DiscountedPrice = product.Dprice,
//                                Unit = product.Wtype,
//                                Weight = product.Wweight,
//                                GroupCode = product.GroupCode,
//                                SKU = product.Sku,
//                                Brand = product.Brand,
//                                IsActive = product.IsActive,
//                                Stock = product.Stock,

//                                // Category Information
//                                Category = new
//                                {
//                                    Id = category?.Id,
//                                    Name = category?.CategoryName,
//                                    Image = category?.CategoryImage
//                                },
//                                SubCategory = new
//                                {
//                                    Id = subcategory?.Id,
//                                    Name = subcategory?.SubcategoryName,
//                                    Image = subcategory?.SubcategoryImage
//                                },
//                                ChildCategory = new
//                                {
//                                    Id = childcat?.Id,
//                                    Name = childcat?.ChildcategoryName,
//                                    Image = childcat?.ChildcategoryImage
//                                }
//                            } : null,

//                            // Delivery Partner Details
//                            DeliveryPartner = delivery != null ? new
//                            {
//                                PartnerId = delivery.Id,
//                                Name = delivery.Name,
//                                Phone = delivery.Phone,
//                                Email = delivery.Email,
//                                VehicleType = delivery.VehicleType,
//                                VehicleNumber = delivery.VehicleNumber,
//                                Rating = delivery.Rating,
//                                IsActive = delivery.IsActive,
//                                CurrentLocation = GetDeliveryPartnerLocation(delivery.Id)
//                            } : null,

//                            // Additional Information
//                            EstimatedDeliveryTime = order.AddedDate.AddHours(24),
//                            PaymentMethod = "COD", // Add to order model if needed
//                            OrderSource = "Mobile App", // Add to order model if needed
//                            Priority = order.Groupid != null ? "Group Order" : "Regular",

//                            // Group Order Details (if applicable)
//                            GroupOrder = order.Groupid != null ? GetGroupOrderDetails(order.Groupid.Value) : null
//                        };

//            var totalCount = await query.CountAsync();
//            var orders = await query
//                .OrderByDescending(o => o.OrderDate)
//                .Skip((page - 1) * pageSize)
//                .Take(pageSize)
//                .ToListAsync();

//            return Ok(new
//            {
//                ActiveOrders = orders,
//                TotalCount = totalCount,
//                Page = page,
//                PageSize = pageSize,
//                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
//            });
//        }

//        // 🔹 GET: All Pending Orders (Assigned but not picked up)
//        [HttpGet("pending")]
//        public async Task<IActionResult> GetPendingOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
//        {
//            var query = from order in _context.TblOrdernows
//                        join user in _context.TblUsers on order.Userid equals user.Id into userJoin
//                        from user in userJoin.DefaultIfEmpty()
//                        join shipping in _context.TblShipings on order.Sipid equals shipping.Id into shipJoin
//                        from shipping in shipJoin.DefaultIfEmpty()
//                        join product in _context.VwProducts on order.Pdid equals product.Pdid into prodJoin
//                        from product in prodJoin.DefaultIfEmpty()
//                        where !order.IsDeleted
//                              && order.DassignidTime == null  // Not yet assigned = Pending
//                        select new
//                        {
//                            OrderId = order.Id,
//                            TrackId = order.TrackId,
//                            OrderDate = order.AddedDate,
//                            Quantity = order.Qty,
//                            DeliveryPrice = order.Deliveryprice,
//                            TotalAmount = order.Deliveryprice * order.Qty,

//                            WaitingTime = DateTime.UtcNow - order.AddedDate,
//                            Priority = order.Groupid != null ? "High (Group Order)" :
//                                      order.AddedDate < DateTime.UtcNow.AddHours(-2) ? "High (Delayed)" : "Normal",

//                            Customer = new
//                            {
//                                UserId = user.Id,
//                                Name = user.FirstName + " " + user.LastName,
//                                Phone = user.Mobile,
//                                Email = user.Email
//                            },

//                            ShippingAddress = shipping != null ? new
//                            {
//                                FullAddress = shipping.Address + ", " + shipping.City + ", " + shipping.State + " - " + shipping.PostalCode,
//                                ContactPerson = shipping.ContactPerson,
//                                Phone = shipping.Phone,
//                                PostalCode = shipping.PostalCode,
//                                Coordinates = new { shipping.Latitude, shipping.Longitude }
//                            } : null,

//                            Product = new
//                            {
//                                Name = product.ProductName,
//                                Image = product.Image,
//                                Price = product.Price,
//                                Unit = product.Wtype,
//                                Weight = product.Wweight
//                            },

//                            // Suggested delivery partners based on location
//                            SuggestedDeliveryPartners = GetNearbyDeliveryPartners(shipping?.Latitude, shipping?.Longitude)
//                        };

//            var totalCount = await query.CountAsync();
//            var orders = await query
//                .OrderByDescending(o => o.WaitingTime)
//                .Skip((page - 1) * pageSize)
//                .Take(pageSize)
//                .ToListAsync();

//            return Ok(new
//            {
//                PendingOrders = orders,
//                TotalCount = totalCount,
//                Page = page,
//                PageSize = pageSize,
//                HighPriorityCount = orders.Count(o => o.Priority.Contains("High"))
//            });
//        }

//        // 🔹 GET: All Completed Orders (Delivered)
//        [HttpGet("completed")]
//        public async Task<IActionResult> GetCompletedOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
//                                                           [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
//        {
//            var query = from order in _context.TblOrdernows
//                        join user in _context.TblUsers on order.Userid equals user.Id into userJoin
//                        from user in userJoin.DefaultIfEmpty()
//                        join shipping in _context.TblShipings on order.Sipid equals shipping.Id into shipJoin
//                        from shipping in shipJoin.DefaultIfEmpty()
//                        join product in _context.VwProducts on order.Pdid equals product.Pdid into prodJoin
//                        from product in prodJoin.DefaultIfEmpty()
//                        join delivery in _context.TblDeliveryusers on order.DeliveryPartnerId equals delivery.Id into delJoin
//                        from delivery in delJoin.DefaultIfEmpty()
//                        where !order.IsDeleted && order.DdeliverredidTime != null  // Delivered orders only
//                        select new
//                        {
//                            OrderId = order.Id,
//                            TrackId = order.TrackId,
//                            OrderDate = order.AddedDate,
//                            DeliveredDate = order.DdeliverredidTime,
//                            Quantity = order.Qty,
//                            DeliveryPrice = order.Deliveryprice,
//                            TotalAmount = order.Deliveryprice * order.Qty,

//                            // Delivery Performance Metrics
//                            TotalDeliveryTime = order.DdeliverredidTime.Value - order.AddedDate,
//                            ProcessingTime = order.DassignidTime.HasValue ? order.DassignidTime.Value - order.AddedDate : TimeSpan.Zero,
//                            DeliveryTime = order.DdeliverredidTime.Value - (order.DvendorpickupTime ?? order.DassignidTime ?? order.AddedDate),

//                            Customer = new
//                            {
//                                UserId = user.Id,
//                                Name = user.FirstName + " " + user.LastName,
//                                Phone = user.Mobile,
//                                Email = user.Email,
//                                TotalOrders = GetCustomerTotalOrders(user.Id),
//                                CustomerSince = user.CreatedDate
//                            },

//                            ShippingAddress = shipping != null ? new
//                            {
//                                FullAddress = shipping.Address + ", " + shipping.City + ", " + shipping.State + " - " + shipping.PostalCode,
//                                ContactPerson = shipping.ContactPerson,
//                                Phone = shipping.Phone,
//                                City = shipping.City,
//                                State = shipping.State,
//                                PostalCode = shipping.PostalCode
//                            } : null,

//                            Product = new
//                            {
//                                Name = product.ProductName,
//                                Image = product.Image,
//                                Price = product.Price,
//                                Unit = product.Wtype
//                            },

//                            DeliveryPartner = delivery != null ? new
//                            {
//                                PartnerId = delivery.Id,
//                                Name = delivery.Name,
//                                Phone = delivery.Phone,
//                                Rating = delivery.Rating,
//                                CompletedDeliveries = GetPartnerCompletedDeliveries(delivery.Id)
//                            } : null,

//                            // Order Timeline
//                            OrderTimeline = new
//                            {
//                                Placed = order.AddedDate,
//                                Assigned = order.DassignidTime,
//                                PickedUp = order.DvendorpickupTime,
//                                Shipped = order.ShipOrderidTime,
//                                Delivered = order.DdeliverredidTime
//                            },

//                            DeliveryRating = GetOrderRating(order.Id),
//                            CustomerFeedback = GetOrderFeedback(order.Id)
//                        };

//            // Apply date filters if provided
//            if (fromDate.HasValue)
//                query = query.Where(o => o.DeliveredDate >= fromDate.Value);
//            if (toDate.HasValue)
//                query = query.Where(o => o.DeliveredDate <= toDate.Value);

//            var totalCount = await query.CountAsync();
//            var orders = await query
//                .OrderByDescending(o => o.DeliveredDate)
//                .Skip((page - 1) * pageSize)
//                .Take(pageSize)
//                .ToListAsync();

//            // Calculate summary statistics
//            var summary = new
//            {
//                TotalCompleted = totalCount,
//                TotalRevenue = orders.Sum(o => o.TotalAmount),
//                AverageDeliveryTime = orders.Any() ? orders.Average(o => o.TotalDeliveryTime.TotalHours) : 0,
//                AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
//                TopPerformingPartner = orders.GroupBy(o => o.DeliveryPartner?.PartnerId)
//                                            .OrderByDescending(g => g.Count())
//                                            .FirstOrDefault()?.FirstOrDefault()?.DeliveryPartner?.Name ?? "N/A"
//            };

//            return Ok(new
//            {
//                CompletedOrders = orders,
//                Summary = summary,
//                TotalCount = totalCount,
//                Page = page,
//                PageSize = pageSize
//            });
//        }

//        // 🔹 POST: Assign Order to Delivery Partner
//        [HttpPost("assign/{orderId}")]
//        public async Task<IActionResult> AssignOrder(long orderId, [FromBody] AssignOrderRequest request)
//        {
//            var order = await _context.TblOrdernows.FindAsync(orderId);
//            if (order == null || order.IsDeleted)
//                return NotFound(new { message = "Order not found." });

//            if (order.DassignidTime != null)
//                return BadRequest(new { message = "Order already assigned." });

//            var deliveryPartner = await _context.TblDeliveryusers.FindAsync(request.DeliveryPartnerId);
//            if (deliveryPartner == null || deliveryPartner.IsActive == false)
//                return BadRequest(new { message = "Invalid or inactive delivery partner." });

//            order.DeliveryPartnerId = request.DeliveryPartnerId;
//            order.DassignidTime = DateTime.UtcNow;
//            order.ModifiedDate = DateTime.UtcNow;
//            order.Dassignid = request.AssignedBy; // Admin user ID

//            await _context.SaveChangesAsync();

//            // Send notification to delivery partner
//            await NotifyDeliveryPartner(request.DeliveryPartnerId, order.TrackId);

//            return Ok(new { message = "Order assigned successfully.", assignedAt = order.DassignidTime });
//        }

//        // 🔹 GET: Order Details by Track ID
//        [HttpGet("details/{trackId}")]
//        public async Task<IActionResult> GetOrderDetails(string trackId)
//        {
//            var orderDetails = await (from order in _context.TblOrdernows
//                                      join user in _context.TblUsers on order.Userid equals user.Id into userJoin
//                                      from user in userJoin.DefaultIfEmpty()
//                                      join shipping in _context.TblShipings on order.Sipid equals shipping.Id into shipJoin
//                                      from shipping in shipJoin.DefaultIfEmpty()
//                                      join product in _context.VwProducts on order.Pdid equals product.Pdid into prodJoin
//                                      from product in prodJoin.DefaultIfEmpty()
//                                      join category in _context.TblCategories on product.Categoryid equals category.Id into catJoin
//                                      from category in catJoin.DefaultIfEmpty()
//                                      join delivery in _context.DeliveryPartners on order.DeliveryPartnerId equals delivery.Id into delJoin
//                                      from delivery in delJoin.DefaultIfEmpty()
//                                      where order.TrackId == trackId && !order.IsDeleted
//                                      select new
//                                      {
//                                          // Complete Order Information
//                                          OrderId = order.Id,
//                                          TrackId = order.TrackId,
//                                          OrderDate = order.AddedDate,
//                                          Quantity = order.Qty,
//                                          DeliveryPrice = order.Deliveryprice,
//                                          TotalAmount = order.Deliveryprice * order.Qty,
//                                          GroupId = order.Groupid,

//                                          // Detailed Customer Information
//                                          Customer = new
//                                          {
//                                              UserId = user.Id,
//                                              FirstName = user.FirstName,
//                                              LastName = user.LastName,
//                                              FullName = user.FirstName + " " + user.LastName,
//                                              Email = user.Email,
//                                              Mobile = user.Mobile,
//                                              AlternatePhone = user.AlternatePhone,
//                                              DateOfBirth = user.DateOfBirth,
//                                              Gender = user.Gender,
//                                              ProfileImage = user.ProfileImage,
//                                              IsActive = user.IsActive,
//                                              RegisteredDate = user.CreatedDate,
//                                              LastLoginDate = user.LastLoginDate,
//                                              TotalOrders = GetCustomerTotalOrders(user.Id),
//                                              CustomerType = GetCustomerType(user.Id)
//                                          },

//                                          // Complete Shipping Address Details
//                                          ShippingAddress = shipping != null ? new
//                                          {
//                                              AddressId = shipping.Id,
//                                              VendorName = shipping.VendorName,
//                                              ContactPerson = shipping.ContactPerson,
//                                              Email = shipping.Email,
//                                              Phone = shipping.Phone,
//                                              AlternatePhone = shipping.AlternatePhone,
//                                              AddressLine1 = shipping.Address,
//                                              AddressLine2 = shipping.AddressLine2,
//                                              Landmark = shipping.Landmark,
//                                              City = shipping.City,
//                                              State = shipping.State,
//                                              PostalCode = shipping.PostalCode,
//                                              Country = shipping.Country,
//                                              Latitude = shipping.Latitude,
//                                              Longitude = shipping.Longitude,
//                                              AddressType = shipping.AddressType,
//                                              IsDefault = shipping.IsDefault,
//                                              Instructions = shipping.Instructions,
//                                              GateCode = shipping.GateCode,
//                                              BuildingInfo = shipping.BuildingInfo,
//                                              FloorNumber = shipping.FloorNumber,
//                                              IsBusinessAddress = shipping.IsBusinessAddress,
//                                              BusinessName = shipping.BusinessName,
//                                              GSTIN = shipping.GSTIN
//                                          } : null,

//                                          // Complete Product Information
//                                          Product = product != null ? new
//                                          {
//                                              ProductId = product.Id,
//                                              Name = product.ProductName,
//                                              Description = product.Longdesc,
//                                              ShortDescription = product.Shortdesc,
//                                              Image = product.Image,
//                                              Images = GetProductImages(product.Id),
//                                              Price = product.Price,
//                                              DiscountedPrice = product.Dprice,
//                                              Unit = product.Wtype,
//                                              Weight = product.Wweight,
//                                              Dimensions = product.Dimensions,
//                                              GroupCode = product.GroupCode,
//                                              SKU = product.Sku,
//                                              Barcode = product.Barcode,
//                                              Brand = product.Brand,
//                                              Manufacturer = product.Manufacturer,
//                                              CountryOfOrigin = product.CountryOfOrigin,
//                                              HSNCode = product.HSNCode,
//                                              TaxRate = product.TaxRate,
//                                              IsActive = product.IsActive,
//                                              Stock = product.Stock,
//                                              MinOrderQuantity = product.MinOrderQuantity,
//                                              MaxOrderQuantity = product.MaxOrderQuantity,
//                                              IsReturnable = product.IsReturnable,
//                                              ReturnWindow = product.ReturnWindow,

//                                              Category = new
//                                              {
//                                                  Id = category?.Id,
//                                                  Name = category?.CategoryName,
//                                                  Image = category?.CategoryImage
//                                              }
//                                          } : null,

//                                          // Delivery Partner Complete Information
//                                          DeliveryPartner = delivery != null ? new
//                                          {
//                                              PartnerId = delivery.Id,
//                                              Name = delivery.Name,
//                                              Phone = delivery.Phone,
//                                              Email = delivery.Email,
//                                              VehicleType = delivery.VehicleType,
//                                              VehicleNumber = delivery.VehicleNumber,
//                                              LicenseNumber = delivery.LicenseNumber,
//                                              Rating = delivery.Rating,
//                                              TotalDeliveries = delivery.TotalDeliveries,
//                                              CompletedDeliveries = delivery.CompletedDeliveries,
//                                              IsActive = delivery.IsActive,
//                                              JoinedDate = delivery.CreatedDate,
//                                              ProfileImage = delivery.ProfileImage,
//                                              CurrentLocation = GetDeliveryPartnerLocation(delivery.Id),
//                                              EmergencyContact = delivery.EmergencyContact
//                                          } : null,

//                                          // Complete Order Timeline
//                                          OrderTimeline = new
//                                          {
//                                              Placed = new { Date = order.AddedDate, Status = "Order Placed", Description = "Order received and confirmed" },
//                                              Assigned = order.DassignidTime.HasValue ? new { Date = order.DassignidTime, Status = "Assigned", Description = "Order assigned to delivery partner" } : null,
//                                              PickedUp = order.DvendorpickupTime.HasValue ? new { Date = order.DvendorpickupTime, Status = "Picked Up", Description = "Order picked up from warehouse" } : null,
//                                              Shipped = order.ShipOrderidTime.HasValue ? new { Date = order.ShipOrderidTime, Status = "In Transit", Description = "Order is on the way to delivery address" } : null,
//                                              Delivered = order.DdeliverredidTime.HasValue ? new { Date = order.DdeliverredidTime, Status = "Delivered", Description = "Order successfully delivered to customer" } : null
//                                          },

//                                          // Payment & Billing Information
//                                          PaymentInfo = new
//                                          {
//                                              Method = "Cash on Delivery", // Add payment method field
//                                              Status = "Pending", // Add payment status field
//                                              TransactionId = order.TransactionId,
//                                              PaymentDate = order.PaymentDate,
//                                              RefundStatus = order.RefundStatus,
//                                              TaxAmount = CalculateTax(order.Deliveryprice * order.Qty),
//                                              DeliveryCharges = CalculateDeliveryCharges(order.Deliveryprice, shipping?.PostalCode),
//                                              TotalAmount = order.Deliveryprice * order.Qty,
//                                              Currency = "INR"
//                                          },

//                                          // Additional Information
//                                          OrderStatus = GetDetailedOrderStatus(order),
//                                          EstimatedDeliveryTime = CalculateEstimatedDelivery(order),
//                                          SpecialInstructions = order.SpecialInstructions,
//                                          CancellationReason = order.CancellationReason,
//                                          GroupOrderDetails = order.Groupid.HasValue ? GetGroupOrderDetails(order.Groupid.Value) : null,
//                                          PromoCodeUsed = GetAppliedPromoCode(order.Id),
//                                          OrderRating = GetOrderRating(order.Id),
//                                          CustomerFeedback = GetOrderFeedback(order.Id),
//                                          ReturnRequestStatus = GetReturnRequestStatus(order.Id),

//                                          // System Information
//                                          CreatedBy = order.CreatedBy,
//                                          ModifiedDate = order.ModifiedDate,
//                                          OrderSource = "Mobile App", // Add order source field
//                                          DeviceInfo = order.DeviceInfo,
//                                          IPAddress = order.IPAddress,
//                                          AppVersion = order.AppVersion
//                                      }).FirstOrDefaultAsync();

//            if (orderDetails == null)
//                return NotFound(new { message = "Order not found." });

//            return Ok(orderDetails);
//        }

//        // Helper Methods
//        private dynamic GetDeliveryPartnerLocation(int partnerId)
//        {
//            var location = _context.DeliveryPartnerLocations
//                .Where(l => l.DeliveryPartnerId == partnerId)
//                .OrderByDescending(l => l.LastUpdated)
//                .FirstOrDefault();

//            return location != null ? new
//            {
//                Latitude = location.Latitude,
//                Longitude = location.Longitude,
//                LastUpdated = location.LastUpdated,
//                IsOnline = location.LastUpdated > DateTime.UtcNow.AddMinutes(-10)
//            } : null;
//        }


//        private List<dynamic> GetNearbyDeliveryPartners(double? lat, double? lng)
//        {
//            if (!lat.HasValue || !lng.HasValue) return new List<dynamic>();

//            return _context.TblDeliveryusers
//                .Where(dp => dp.IsActive == false)
//                .Take(5)
//                .Select(dp => new
//                {
//                    dp.Id,
//                    dp.ContactPerson,
//                    dp.Phone,
//                    dp.Rating,
//                    dp.VehicleType,
//                    IsAvailable = !_context.TblOrdernows.Any(o => o.DeliveryPartnerId == dp.Id
//                                                               && o.DvendorpickupTime != null
//                                                               && o.DdeliverredidTime == null),
//                    Distance = "2.3 km" // Calculate actual distance
//                })
//                .ToList<dynamic>();
//        }

//        private int GetCustomerTotalOrders(int? userId)
//        {
//            return _context.TblOrdernows.Count(o => o.Userid == userId && !o.IsDeleted);
//        }

//        private string GetCustomerType(int? userId)
//        {
//            var orderCount = GetCustomerTotalOrders(userId);
//            return orderCount >= 10 ? "Premium" : orderCount >= 5 ? "Regular" : "New";
//        }

//        private int GetPartnerCompletedDeliveries(int partnerId)
//        {
//            return _context.TblOrdernows.Count(o => o.DeliveryPartnerId == partnerId && o.DdeliverredidTime != null);
//        }

//        private List<string> GetProductImages(long productId)
//        {
//            return _context.VwProducts
//                .Where(pi => pi.Pdid == productId)
//                .Select(pi => pi.Image)
//                .ToList();
//        }

//        private decimal CalculateTax(decimal amount)
//        {
//            return amount * 0.18m; // 18% GST
//        }

//        private decimal CalculateDeliveryCharges(decimal orderAmount, string postalCode)
//        {
//            return orderAmount < 500 ? 50 : 0; // Free delivery above 500
//        }

//        private string GetDetailedOrderStatus(dynamic order)
//        {
//            if (order.IsDeleted) return "Cancelled";
//            if (order.DdeliverredidTime != null) return "Delivered";
//            if (order.ShipOrderidTime != null) return "Out for Delivery";
//            if (order.DvendorpickupTime != null) return "Picked Up from Warehouse";
//            if (order.DassignidTime != null) return "Assigned to Delivery Partner";
//            return "Order Placed - Awaiting Assignment";
//        }

//        private DateTime CalculateEstimatedDelivery(dynamic order)
//        {
//            if (order.DdeliverredidTime != null) return order.DdeliverredidTime;

//            var baseDeliveryTime = order.AddedDate.AddHours(24); // Default 24 hours

//            // Adjust based on current status
//            if (order.ShipOrderidTime != null)
//                return DateTime.UtcNow.AddHours(2); // 2 hours if shipped
//            if (order.DvendorpickupTime != null)
//                return DateTime.UtcNow.AddHours(4); // 4 hours if picked up
//            if (order.DassignidTime != null)
//                return DateTime.UtcNow.AddHours(8); // 8 hours if assigned

//            return baseDeliveryTime;
//        }

//        private dynamic GetAppliedPromoCode(long orderId)
//        {
//            var promoUsage = _context.TblPromocodeUsages
//                .Include(pu => pu.Promocode)
//                .FirstOrDefault(pu => pu.OrderId == orderId);

//            return promoUsage != null ? new
//            {
//                Code = promoUsage.Promocode?.Code,
//                Description = promoUsage.Promocode?.Description,
//                DiscountType = promoUsage.Promocode?.DiscountType,
//                DiscountValue = promoUsage.Promocode?.DiscountValue,
//                SavedAmount = promoUsage.DiscountAmount
//            } : null;
//        }

//        private dynamic GetOrderRating(long orderId)
//        {
//            var rating = _context.TblOrderRatings.FirstOrDefault(r => r.OrderId == orderId);
//            return rating != null ? new
//            {
//                Rating = rating.Rating,
//                Review = rating.Review,
//                RatedDate = rating.CreatedDate
//            } : null;
//        }

//        private string GetOrderFeedback(long orderId)
//        {
//            return _context.TblOrderFeedbacks
//                .Where(f => f.OrderId == orderId)
//                .Select(f => f.Feedback)
//                .FirstOrDefault();
//        }

//        private string GetReturnRequestStatus(long orderId)
//        {
//            return _context.TblReturnRequests
//                .Where(r => r.OrderId == orderId)
//                .Select(r => r.Status)
//                .FirstOrDefault();
//        }

//        private async Task NotifyDeliveryPartner(int partnerId, string trackId)
//        {
//            // Implementation for sending FCM notification to delivery partner
//            var partner = await _context.DeliveryPartners.FindAsync(partnerId);
//            if (partner?.FcmToken != null)
//            {
//                // Send FCM notification
//                // await _fcmService.SendNotificationAsync(partner.FcmToken, "New Order Assigned", $"Order {trackId} assigned to you");
//            }
//        }

//        public class AssignOrderRequest
//        {
//            public int DeliveryPartnerId { get; set; }
//            public int AssignedBy { get; set; }
//            public string Notes { get; set; }
//        }
//    }
//}

//// ================================= CUSTOMER ORDER TRACKING CONTROLLER =================================
//namespace ArimartEcommerceAPI.Controllers
//{
//    [Route("api/customer/[controller]")]
//    [ApiController]
//    public class OrderTrackingController : ControllerBase
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly IFcmPushService _fcmPushService;

//        public OrderTrackingController(ApplicationDbContext context, IFcmPushService fcmPushService)
//        {
//            _context = context;
//            _fcmPushService = fcmPushService;
//        }

//        // 🔹 GET: Complete Order History with All Details
//        [HttpGet("history/{userId}")]
//        public async Task<IActionResult> GetOrderHistory(int userId,
//            [FromQuery] string status = "all",
//            [FromQuery] int page = 1,
//            [FromQuery] int pageSize = 20,
//            [FromQuery] DateTime? fromDate = null,
//            [FromQuery] DateTime? toDate = null)
//        {
//            var query = from order in _context.TblOrdernows
//                        join user in _context.TblUsers on order.Userid equals user.I

//// ================================= DELIVERY PARTNER CONTROLLER =================================
//namespace ArimartEcommerceAPI.Controllers
//    {
//        [Route("api/delivery/[controller]")]
//        [ApiController]
//        public class DeliveryPartnerController : ControllerBase
//        {
//            private readonly ApplicationDbContext _context;
//            private readonly IFcmPushService _fcmPushService;

//            public DeliveryPartnerController(ApplicationDbContext context, IFcmPushService fcmPushService)
//            {
//                _context = context;
//                _fcmPushService = fcmPushService;
//            }

//            // 🔹 GET: Assigned Orders for Delivery Partner
//            [HttpGet("assigned/{partnerId}")]
//            public async Task<IActionResult> GetAssignedOrders(int partnerId, [FromQuery] string status = "all")
//            {
//                var query = from order in _context.TblOrdernows
//                            join user in _context.TblUsers on order.Userid equals user.Id into userJoin
//                            from user in userJoin.DefaultIfEmpty()
//                            join shipping in _context.TblShipings on order.Sipid equals shipping.Id into shipJoin
//                            from shipping in shipJoin.DefaultIfEmpty()
//                            join product in _context.VwProducts on order.Pdid equals product.Pdid into prodJoin
//                            from product in prodJoin.DefaultIfEmpty()
//                            where order.DeliveryPartnerId == partnerId && !order.IsDeleted
//                            select new
//                            {
//                                OrderId = order.Id,
//                                TrackId = order.TrackId,
//                                OrderDate = order.AddedDate,
//                                AssignedDate = order.DassignidTime,
//                                Quantity = order.Qty,
//                                TotalAmount = order.Deliveryprice * order.Qty,

//                                Status = order.DdeliverredidTime != null ? "delivered"
//                                       : order.ShipOrderidTime != null ? "shipped"
//                                       : order.DvendorpickupTime != null ? "picked_up"
//                                       : "assigned",

//                                // Complete Customer Details
//                                Customer = new
//                                {
//                                    UserId = user.Id,
//                                    Name = user.FirstName + " " + user.LastName,
//                                    Phone = user.Mobile,
//                                    AlternatePhone = user.AlternatePhone,
//                                    Email = user.Email,
//                                    ProfileImage = user.ProfileImage,
//                                    PreferredLanguage = user.PreferredLanguage,
//                                    CustomerNotes = user.DeliveryInstructions
//                                },

//                                // Detailed Shipping Address with GPS
//                                ShippingAddress = shipping != null ? new
//                                {
//                                    AddressId = shipping.Id,
//                                    ContactPerson = shipping.ContactPerson,
//                                    Phone = shipping.Phone,
//                                    AlternatePhone = shipping.AlternatePhone,
//                                    FullAddress = shipping.Address + ", " + shipping.City + ", " + shipping.State + " - " + shipping.PostalCode,
//                                    AddressLine1 = shipping.Address,
//                                    AddressLine2 = shipping.AddressLine2,
//                                    Landmark = shipping.Landmark,
//                                    City = shipping.City,
//                                    State = shipping.State,
//                                    PostalCode = shipping.PostalCode,
//                                    Country = shipping.Country,

//                                    // GPS Coordinates
//                                    Latitude = shipping.Latitude,
//                                    Longitude = shipping.Longitude,

//                                    // Delivery Instructions
//                                    Instructions = shipping.Instructions,
//                                    GateCode = shipping.GateCode,
//                                    BuildingInfo = shipping.BuildingInfo,
//                                    FloorNumber = shipping.FloorNumber,

//                                    // Address Type & Business Info
//                                    AddressType = shipping.AddressType,
//                                    IsBusinessAddress = shipping.IsBusinessAddress,
//                                    BusinessName = shipping.BusinessName,
//                                    BusinessHours = shipping.BusinessHours,

//                                    // Distance from current location
//                                    EstimatedDistance = CalculateDistanceFromPartner(partnerId, shipping.Latitude, shipping.Longitude),
//                                    EstimatedTime = CalculateTimeFromPartner(partnerId, shipping.Latitude, shipping.Longitude)
//                                } : null,

//                                // Product Details for Verification
//                                Product = product != null ? new
//                                {
//                                    Name = product.ProductName,
//                                    Image = product.Image,
//                                    SKU = product.Sku,
//                                    Barcode = product.Barcode,
//                                    Price = product.Price,
//                                    Unit = product.Wtype,
//                                    Weight = product.Wweight,
//                                    Dimensions = product.Dimensions,
//                                    IsFragile = product.IsFragile,
//                                    RequiresColdStorage = product.RequiresColdStorage,
//                                    SpecialHandling = product.SpecialHandling
//                                } : null,

//                                // Pickup Information
//                                PickupDetails = new
//                                {
//                                    WarehouseAddress = "Main Warehouse, Delhi", // From config
//                                    WarehousePhone = "+91-9999999999",
//                                    PickupInstructions = "Show your delivery partner ID and order barcode",
//                                    WarehouseCoordinates = new { Latitude = 28.6139, Longitude = 77.2090 },
//                                    ContactPerson = "Warehouse Manager",
//                                    OperatingHours = "9 AM - 8 PM"
//                                },

//                                // Payment Information
//                                PaymentInfo = new
//                                {
//                                    Method = order.PaymentMethod ?? "COD",
//                                    Status = order.PaymentStatus ?? "Pending",
//                                    AmountToCollect = order.PaymentMethod == "COD" ? order.Deliveryprice * order.Qty : 0,
//                                    Currency = "INR",
//                                    ChangeRequired = order.ChangeRequired ?? 0
//                                },

//                                // Timeline & Priority
//                                Priority = CalculateOrderPriority(order),
//                                ExpectedDeliveryTime = CalculateExpectedDelivery(order),
//                                TimeRemaining = CalculateTimeRemaining(order),

//                                // Order Actions Available
//                                AvailableActions = GetAvailableActions(order),

//                                // Special Requirements
//                                SpecialRequirements = new
//                                {
//                                    RequiresSignature = order.RequiresSignature ?? false,
//                                    RequiresPhoto = order.RequiresPhoto ?? false,
//                                    RequiresOTP = order.RequiresOTP ?? false,
//                                    AgeVerification = order.AgeVerification ?? false,
//                                    IdentityProof = order.IdentityProof ?? false
//                                }
//                            };

//                // Apply status filter
//                if (status != "all")
//                {
//                    query = query.Where(o => o.Status == status);
//                }

//                var orders = await query
//                    .OrderBy(o => o.Priority == "High" ? 1 : o.Priority == "Medium" ? 2 : 3)
//                    .ThenBy(o => o.ExpectedDeliveryTime)
//                    .ToListAsync();

//                return Ok(new
//                {
//                    AssignedOrders = orders,
//                    Summary = new
//                    {
//                        TotalOrders = orders.Count,
//                        HighPriorityCount = orders.Count(o => o.Priority == "High"),
//                        PendingPickup = orders.Count(o => o.Status == "assigned"),
//                        InTransit = orders.Count(o => o.Status == "picked_up" || o.Status == "shipped"),
//                        ReadyForDelivery = orders.Count(o => o.Status == "shipped"),
//                        TotalEarnings = orders.Where(o => o.Status == "delivered").Sum(o => o.TotalAmount * 0.05m) // 5% commission
//                    }
//                });
//            }

//            // 🔹 POST: Update Order Status (Pickup, Ship, Deliver)
//            [HttpPost("update-status")]
//            public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateStatusRequest request)
//            {
//                var order = await _context.TblOrdernows
//                    .Include(o => o.Customer)
//                    .FirstOrDefaultAsync(o => o.TrackId == request.TrackId && o.DeliveryPartnerId == request.DeliveryPartnerId);

//                if (order == null)
//                    return NotFound(new { message = "Order not found or not assigned to you." });

//                var currentTime = DateTime.UtcNow;
//                string statusMessage = "";

//                switch (request.Status.ToLower())
//                {
//                    case "picked_up":
//                        if (order.DassignidTime == null)
//                            return BadRequest(new { message = "Order must be assigned first." });
//                        if (order.DvendorpickupTime != null)
//                            return BadRequest(new { message = "Order already picked up." });

//                        order.DvendorpickupTime = currentTime;
//                        statusMessage = "Order picked up from warehouse successfully.";

//                        // Verify all items are picked
//                        if (request.PickedItems?.Any() == true)
//                        {
//                            foreach (var item in request.PickedItems)
//                            {
//                                // Log picked items for verification
//                                _context.OrderPickupItems.Add(new OrderPickupItem
//                                {
//                                    OrderId = order.Id,
//                                    ProductId = item.ProductId,
//                                    QuantityPicked = item.Quantity,
//                                    PickedAt = currentTime,
//                                    PickedBy = request.DeliveryPartnerId
//                                });
//                            }
//                        }
//                        break;

//                    case "shipped":
//                        if (order.DvendorpickupTime == null)
//                            return BadRequest(new { message = "Order must be picked up first." });
//                        if (order.ShipOrderidTime != null)
//                            return BadRequest(new { message = "Order already shipped." });

//                        order.ShipOrderidTime = currentTime;
//                        statusMessage = "Order is now out for delivery.";
//                        break;

//                    case "delivered":
//                        if (order.ShipOrderidTime == null)
//                            return BadRequest(new { message = "Order must be shipped first." });
//                        if (order.DdeliverredidTime != null)
//                            return BadRequest(new { message = "Order already delivered." });

//                        order.DdeliverredidTime = currentTime;
//                        statusMessage = "Order delivered successfully.";

//                        // Handle delivery proof
//                        if (request.DeliveryProof != null)
//                        {
//                            order.DeliveryProofImage = request.DeliveryProof.ImageUrl;
//                            order.DeliverySignature = request.DeliveryProof.SignatureUrl;
//                            order.DeliveryOTP = request.DeliveryProof.OTP;
//                            order.ReceiverName = request.DeliveryProof.ReceiverName;
//                            order.ReceiverRelation = request.DeliveryProof.ReceiverRelation;
//                        }

//                        // Handle payment collection for COD
//                        if (request.PaymentCollected != null)
//                        {
//                            order.PaymentStatus = "Collected";
//                            order.PaymentCollectedAmount = request.PaymentCollected.AmountCollected;
//                            order.ChangeGiven = request.PaymentCollected.ChangeGiven;
//                            order.PaymentCollectedAt = currentTime;
//                        }
//                        break;

//                    default:
//                        return BadRequest(new { message = "Invalid status." });
//                }

//                // Update location if provided
//                if (request.CurrentLocation != null)
//                {
//                    await UpdateDeliveryPartnerLocation(request.DeliveryPartnerId,
//                        request.CurrentLocation.Latitude, request.CurrentLocation.Longitude);
//                }

//                order.ModifiedDate = currentTime;

//                // Add status history
//                _context.VwOrders.Add(new VwOrder
//                {
//                    orde = order.Id,
//                    Status = request.Status,
//                    UpdatedBy = request.DeliveryPartnerId,
//                    UpdatedAt = currentTime,
//                    Location = request.CurrentLocation != null ?
//                        $"{request.CurrentLocation.Latitude},{request.CurrentLocation.Longitude}" : null,
//                    Notes = request.Notes
//                });

//                await _context.SaveChangesAsync();

//                // Send notification to customer
//                await SendCustomerNotification(order.Userid, order.TrackId, request.Status, statusMessage);

//                // Calculate earnings for delivered orders
//                decimal earnings = 0;
//                if (request.Status.ToLower() == "delivered")
//                {
//                    earnings = CalculateDeliveryEarnings(order);

//                    // Add to delivery partner earnings
//                    _context.DeliveryEarnings.Add(new DeliveryEarning
//                    {
//                        DeliveryPartnerId = request.DeliveryPartnerId,
//                        OrderId = order.Id,
//                        BaseAmount = order.Deliveryprice * order.Qty,
//                        CommissionRate = 0.05m,
//                        EarningAmount = earnings,
//                        EarnedAt = currentTime
//                    });
//                    await _context.SaveChangesAsync();
//                }

//                return Ok(new
//                {
//                    message = statusMessage,
//                    updatedStatus = request.Status,
//                    updatedAt = currentTime,
//                    earnings = earnings > 0 ? earnings : (decimal?)null,
//                    nextAction = GetNextAction(request.Status),
//                    customerNotified = true
//                });
//            }

//            // 🔹 GET: Delivery Route & Navigation
//            [HttpGet("route/{trackId}")]
//            public async Task<IActionResult> GetDeliveryRoute(string trackId, [FromQuery] int deliveryPartnerId)
//            {
//                var order = await (from o in _context.TblOrdernows
//                                   where o.TrackId == trackId && o.DeliveryPartnerId == deliveryPartnerId && !o.IsDeleted
//                                   join shipping in _context.TblShipings on o.Sipid equals shipping.Id
//                                   join customer in _context.TblUsers on o.Userid equals customer.Id
//                                   where o.TrackId == trackId && o.DeliveryPartnerId == deliveryPartnerId
//                                   select new
//                                   {
//                                       Order = o,
//                                       Shipping = shipping,
//                                       Customer = customer
//                                   }).FirstOrDefaultAsync();

//                if (order == null)
//                    return NotFound(new { message = "Order not found or not assigned to you." });

//                var partnerLocation = await GetDeliveryPartnerCurrentLocation(deliveryPartnerId);
//                var warehouseLocation = GetWarehouseLocation();

//                return Ok(new
//                {
//                    RouteInfo = new
//                    {
//                        // Current delivery partner location
//                        CurrentLocation = partnerLocation,

//                        // Warehouse/Pickup location
//                        PickupLocation = new
//                        {
//                            Name = "Main Warehouse",
//                            Address = "Warehouse Address, Delhi",
//                            Coordinates = warehouseLocation,
//                            ContactPerson = "Warehouse Manager",
//                            Phone = "+91-9999999999",
//                            Instructions = "Show delivery partner ID and scan order barcode"
//                        },

//                        // Customer delivery location
//                        DeliveryLocation = new
//                        {
//                            Name = order.Shipping.VendorName,
//                            ContactPerson = order.Shipping.ContactPerson,
//                            Phone = order.Shipping.Phone,
//                            Address = $"{order.Shipping.Address}, {order.Shipping.City}, {order.Shipping.State} - {order.Shipping.PostalCode}",
//                            Coordinates = new
//                            {
//                                Latitude = order.Shipping.Latitude,
//                                Longitude = order.Shipping.Longitude
//                            },
//                            Landmark = order.Shipping.Landmark,
//                            Instructions = order.Shipping.Instructions,
//                            GateCode = order.Shipping.GateCode,
//                            FloorNumber = order.Shipping.FloorNumber,
//                            BuildingInfo = order.Shipping.BuildingInfo
//                        },

//                        // Route calculations
//                        RouteDetails = new
//                        {
//                            TotalDistance = CalculateTotalDistance(partnerLocation, warehouseLocation,
//                                new { order.Shipping.Latitude, order.Shipping.Longitude }),
//                            EstimatedTime = CalculateTotalTime(partnerLocation, warehouseLocation,
//                                new { order.Shipping.Latitude, order.Shipping.Longitude }),
//                            PickupToDeliveryDistance = CalculateDistance(warehouseLocation,
//                                new { order.Shipping.Latitude, order.Shipping.Longitude }),
//                            PickupToDeliveryTime = CalculateTime(warehouseLocation,
//                                new { order.Shipping.Latitude, order.Shipping.Longitude })
//                        }
//                    },

//                    // Order information for delivery
//                    OrderInfo = new
//                    {
//                        TrackId = order.TrackId,
//                        Items = GetOrderItems(order.Id),
//                        TotalAmount = order.Deliveryprice * order.Qty,
//                        PaymentMethod = order.PaymentMethod ?? "COD",
//                        AmountToCollect = order.PaymentMethod == "COD" ? order.Deliveryprice * order.Qty : 0,
//                        SpecialInstructions = order.SpecialInstructions,
//                        RequiresSignature = order.RequiresSignature ?? false,
//                        RequiresPhoto = order.RequiresPhoto ?? false,
//                        RequiresOTP = order.RequiresOTP ?? false
//                    },

//                    // Customer information
//                    CustomerInfo = new
//                    {
//                        Name = order.Customer.FirstName + " " + order.Customer.LastName,
//                        Phone = order.Customer.Mobile,
//                        AlternatePhone = order.Customer.AlternatePhone,
//                        PreferredLanguage = order.Customer.PreferredLanguage,
//                        DeliveryPreferences = order.Customer.DeliveryInstructions
//                    },

//                    // Navigation waypoints for GPS apps
//                    NavigationWaypoints = new[]
//                    {
//                    new { Type = "pickup", Name = "Warehouse", Coordinates = warehouseLocation },
//                    new { Type = "delivery", Name = "Customer", Coordinates = new { order.Shipping.Latitude, order.Shipping.Longitude } }
//                }
//                });
//            }

//            // Helper Methods
//            private string CalculateOrderPriority(dynamic order)
//            {
//                var orderTime = (DateTime)order.AddedDate;
//                var timeSinceOrder = DateTime.UtcNow - orderTime;

//                if (timeSinceOrder.TotalHours > 4) return "High";
//                if (timeSinceOrder.TotalHours > 2) return "Medium";
//                return "Normal";
//            }

//            private DateTime CalculateExpectedDelivery(dynamic order)
//            {
//                var orderTime = (DateTime)order.AddedDate;
//                return orderTime.AddHours(24); // Standard 24-hour delivery
//            }

//            private TimeSpan CalculateTimeRemaining(dynamic order)
//            {
//                var expected = CalculateExpectedDelivery(order);
//                var remaining = expected - DateTime.UtcNow;
//                return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
//            }

//            private List<string> GetAvailableActions(dynamic order)
//            {
//                var actions = new List<string>();

//                if (order.DassignidTime != null && order.DvendorpickupTime == null)
//                    actions.Add("pickup");
//                if (order.DvendorpickupTime != null && order.ShipOrderidTime == null)
//                    actions.Add("start_delivery");
//                if (order.ShipOrderidTime != null && order.DdeliverredidTime == null)
//                    actions.Add("deliver");

//                return actions;
//            }

//            private string GetNextAction(string currentStatus)
//            {
//                return currentStatus.ToLower() switch
//                {
//                    "picked_up" => "Start delivery to customer",
//                    "shipped" => "Complete delivery and collect payment",
//                    "delivered" => "View next assigned order",
//                    _ => "Pick up order from warehouse"
//                };
//            }

//            private decimal CalculateDeliveryEarnings(dynamic order)
//            {
//                var baseAmount = (decimal)(order.Deliveryprice * order.Qty);
//                return baseAmount * 0.05m; // 5% commission
//            }

//            private async Task<dynamic> GetDeliveryPartnerCurrentLocation(int partnerId)
//            {
//                var location = await _context.DeliveryPartnerLocations
//                    .Where(l => l.DeliveryPartnerId == partnerId)
//                    .OrderByDescending(l => l.LastUpdated)
//                    .FirstOrDefaultAsync();

//                return location != null ? new
//                {
//                    Latitude = location.Latitude,
//                    Longitude = location.Longitude,
//                    LastUpdated = location.LastUpdated
//                } : GetWarehouseLocation(); // Default to warehouse if no location
//            }

//            private dynamic GetWarehouseLocation()
//            {
//                return new
//                {
//                    Latitude = 28.6139,
//                    Longitude = 77.2090
//                };
//            }

//            private string CalculateDistanceFromPartner(int partnerId, double? lat, double? lng)
//            {
//                // Implement actual distance calculation
//                return "3.2 km";
//            }

//            private string CalculateTimeFromPartner(int partnerId, double? lat, double? lng)
//            {
//                // Implement actual time calculation
//                return "15 mins";
//            }

//            private string CalculateDistance(dynamic from, dynamic to)
//            {
//                // Implement Haversine formula or use mapping API
//                return "5.8 km";
//            }

//            private string CalculateTime(dynamic from, dynamic to)
//            {
//                // Calculate based on distance and traffic
//                return "25 mins";
//            }

//            private string CalculateTotalDistance(dynamic current, dynamic warehouse, dynamic delivery)
//            {
//                // Calculate total route distance
//                return "8.5 km";
//            }

//            private string CalculateTotalTime(dynamic current, dynamic warehouse, dynamic delivery)
//            {
//                // Calculate total route time
//                return "35 mins";
//            }

//            private List<dynamic> GetOrderItems(long orderId)
//            {
//                return _context.TblOrdernows
//                    .Where(o => o.Id == orderId)
//                    .Join(_context.VwProducts, o => o.Pdid, p => p.Pdid, (o, p) => new
//                    {
//                        ProductName = p.ProductName,
//                        Image = p.Image,
//                        Quantity = o.Qty,
//                        Unit = p.Wtype,
//                        Price = o.Deliveryprice,
//                        SKU = p.Sku,
//                        Barcode = p.code
//                    })
//                    .ToList<dynamic>();
//            }

//            private async Task UpdateDeliveryPartnerLocation(int partnerId, double lat, double lng)
//            {
//                var location = await _context.DeliveryPartnerLocations
//                    .FirstOrDefaultAsync(l => l.DeliveryPartnerId == partnerId);

//                if (location == null)
//                {
//                    _context.DeliveryPartnerLocations.Add(new DeliveryPartnerLocation
//                    {
//                        DeliveryPartnerId = partnerId,
//                        Latitude = lat,
//                        Longitude = lng,
//                        LastUpdated = DateTime.UtcNow
//                    });
//                }
//                else
//                {
//                    location.Latitude = lat;
//                    location.Longitude = lng;
//                    location.LastUpdated = DateTime.UtcNow;
//                }

//                await _context.SaveChangesAsync();
//            }

//            private async Task SendCustomerNotification(int? userId, string trackId, string status, string message)
//            {
//                var fcmToken = await _context.FcmDeviceTokens
//                    .Where(t => t.UserId == userId)
//                    .Select(t => t.Token)
//                    .FirstOrDefaultAsync();

//                if (!string.IsNullOrEmpty(fcmToken))
//                {
//                    var title = status.ToLower() switch
//                    {
//                        "picked_up" => "Order Picked Up! 📦",
//                        "shipped" => "Order On The Way! 🚚",
//                        "delivered" => "Order Delivered! 🎉",
//                        _ => "Order Update 📋"
//                    };

//                    await _fcmPushService.SendNotificationAsync(fcmToken, title, $"Order {trackId}: {message}");
//                }
//            }

//            // Request Models
//            public class UpdateStatusRequest
//            {
//                public string TrackId { get; set; }
//                public int DeliveryPartnerId { get; set; }
//                public string Status { get; set; } // picked_up, shipped, delivered
//                public LocationData CurrentLocation { get; set; }
//                public string Notes { get; set; }
//                public List<PickedItemData> PickedItems { get; set; }
//                public DeliveryProofData DeliveryProof { get; set; }
//                public PaymentCollectionData PaymentCollected { get; set; }
//            }

//            public class LocationData
//            {
//                public double Latitude { get; set; }
//                public double Longitude { get; set; }
//            }

//            public class PickedItemData
//            {
//                public long ProductId { get; set; }
//                public int Quantity { get; set; }
//            }

//            public class DeliveryProofData
//            {
//                public string ImageUrl { get; set; }
//                public string SignatureUrl { get; set; }
//                public string OTP { get; set; }
//                public string ReceiverName { get; set; }
//                public string ReceiverRelation { get; set; }
//            }

//            public class PaymentCollectionData
//            {
//                public decimal AmountCollected { get; set; }
//                public decimal ChangeGiven { get; set; }
//                public string PaymentMethod { get; set; }
//            }
//        }
//    }
//}