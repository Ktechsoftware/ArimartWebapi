using ArimartEcommerceAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ArimartEcommerceAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeliveryUserDetailsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public DeliveryUserDetailsController(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Save or update emergency details for delivery user
        /// </summary>
        [HttpPost("emergency-details/{userId}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> SaveEmergencyDetails(long userId, [FromBody] EmergencyDetailsRequest request)
        {
            if (userId <= 0)
                return BadRequest(new ErrorResponse { Message = "Valid user ID is required." });

            try
            {
                // Use ExecuteUpdate for pure update approach (EF Core 7+)
                var rowsAffected = await _db.TblDeliveryusers
                    .Where(u => u.Id == userId && !u.IsDeleted)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.PrimaryContactName, request.PrimaryContactName)
                        .SetProperty(u => u.PrimaryContactPhone, request.PrimaryContactPhone)
                        .SetProperty(u => u.PrimaryContactRelation, request.PrimaryContactRelation)
                        .SetProperty(u => u.SecondaryContactName, request.SecondaryContactName ?? string.Empty)
                        .SetProperty(u => u.SecondaryContactPhone, request.SecondaryContactPhone ?? string.Empty)
                        .SetProperty(u => u.SecondaryContactRelation, request.SecondaryContactRelation ?? string.Empty)
                        .SetProperty(u => u.MedicalConditions, request.MedicalConditions ?? string.Empty)
                        .SetProperty(u => u.Allergies, request.Allergies ?? string.Empty)
                        .SetProperty(u => u.BloodGroup, request.BloodGroup ?? string.Empty)
                        .SetProperty(u => u.EmergencyAddress, request.EmergencyAddress ?? string.Empty)
                        .SetProperty(u => u.Emergencydetail, true)
                        .SetProperty(u => u.ModifiedDate, DateTime.UtcNow)
                    );

                if (rowsAffected == 0)
                {
                    return NotFound(new ErrorResponse { Message = "User not found or inactive." });
                }

                return Ok(new ApiResponse
                {
                    Message = "Emergency details saved successfully.",
                    Success = true,
                    Data = new { EmergencyDetailsComplete = true }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving emergency details: {ex}");
                return StatusCode(500, new ErrorResponse
                {
                    Message = $"Failed to save emergency details: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Save or update bank details for delivery user
        /// </summary>
        [HttpPost("bank-details/{userId}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> SaveBankDetails(long userId, [FromBody] BankDetailsRequest request)
        {
            if (userId <= 0)
                return BadRequest(new ErrorResponse { Message = "Valid user ID is required." });

            // Validate account numbers match
            if (request.AccountNumber != request.ConfirmAccountNumber)
                return BadRequest(new ErrorResponse { Message = "Account numbers do not match." });

            try
            {
                // Use ExecuteUpdate for pure update approach (EF Core 7+)
                var rowsAffected = await _db.TblDeliveryusers
                    .Where(u => u.Id == userId && !u.IsDeleted)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.AccountHolderName, request.AccountHolderName)
                        .SetProperty(u => u.AccountNo, request.AccountNumber)
                        .SetProperty(u => u.Ifsccode, request.IfscCode)
                        .SetProperty(u => u.BankName, request.BankName)
                        .SetProperty(u => u.BranchName, request.BranchName)
                        .SetProperty(u => u.AccountType, request.AccountType)
                        .SetProperty(u => u.UpiId, request.UpiId ?? string.Empty)
                        .SetProperty(u => u.Bankdetail, true)
                        .SetProperty(u => u.ModifiedDate, DateTime.UtcNow)
                    );

                if (rowsAffected == 0)
                {
                    return NotFound(new ErrorResponse { Message = "User not found or inactive." });
                }

                return Ok(new ApiResponse
                {
                    Message = "Bank details saved successfully.",
                    Success = true,
                    Data = new { BankDetailsComplete = true }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving bank details: {ex}");
                return StatusCode(500, new ErrorResponse
                {
                    Message = $"Failed to save bank details: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Save or update vehicle details for delivery user
        /// </summary>
        [HttpPost("vehicle-details/{userId}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> SaveVehicleDetails(long userId, [FromBody] VehicleDetailsRequest request)
        {
            if (userId <= 0)
                return BadRequest(new ErrorResponse { Message = "Valid user ID is required." });

            try
            {
                // Use ExecuteUpdate for pure update approach (EF Core 7+)
                var rowsAffected = await _db.TblDeliveryusers
                    .Where(u => u.Id == userId && !u.IsDeleted)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.VehicleType, request.VehicleType)
                        .SetProperty(u => u.VehicleNumber, request.VehicleNumber)
                        .SetProperty(u => u.VehicleBrand, request.Brand)
                        .SetProperty(u => u.VehicleModel, request.Model)
                        .SetProperty(u => u.VehicleYear, request.Year ?? string.Empty)
                        .SetProperty(u => u.VehicleColor, request.Color ?? string.Empty)
                        .SetProperty(u => u.RegistrationDate, request.RegistrationDate)
                        .SetProperty(u => u.InsuranceNumber, request.InsuranceNumber ?? string.Empty)
                        .SetProperty(u => u.DrivingLicenseNumber, request.DrivingLicense ?? string.Empty)
                        .SetProperty(u => u.Vehicledetail, true)
                        .SetProperty(u => u.ModifiedDate, DateTime.UtcNow)
                    );

                if (rowsAffected == 0)
                {
                    return NotFound(new ErrorResponse { Message = "User not found or inactive." });
                }

                return Ok(new ApiResponse
                {
                    Message = "Vehicle details updated successfully.",
                    Success = true,
                    Data = new { VehicleDetailsComplete = true }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error upserting vehicle details: {ex}");
                return StatusCode(500, new ErrorResponse
                {
                    Message = $"Failed to save vehicle details: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Get user's current details completion status
        /// </summary>
        [HttpGet("status/{userId}")]
        [ProducesResponseType(typeof(UserDetailsStatusResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> GetUserDetailsStatus(long userId)
        {
            var user = await _db.TblDeliveryusers
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
                return NotFound(new ErrorResponse { Message = "User not found." });

            return Ok(new UserDetailsStatusResponse
            {
                UserId = userId,
                CurrentStep = user.CurrentStep ?? 1,
                PersonalInfoComplete = user.PersonalInfoComplete ?? false,
                DocumentsUploaded = user.DocumentsUploaded ?? false,
                EmergencyDetailsComplete = user.Emergencydetail,
                BankDetailsComplete = user.Bankdetail,
                VehicleDetailsComplete = user.Vehicledetail,
                ProfileComplete = user.ProfileComplete ?? false,
                RegistrationStatus = user.RegistrationStatus ?? "pending"
            });
        }

        /// <summary>
        /// Get user's emergency details
        /// </summary>
        [HttpGet("emergency-details/{userId}")]
        [ProducesResponseType(typeof(EmergencyDetailsResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> GetEmergencyDetails(long userId)
        {
            var user = await _db.TblDeliveryusers
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
                return NotFound(new ErrorResponse { Message = "User not found." });

            return Ok(new EmergencyDetailsResponse
            {
                PrimaryContactName = user.PrimaryContactName,
                PrimaryContactPhone = user.PrimaryContactPhone,
                PrimaryContactRelation = user.PrimaryContactRelation,
                SecondaryContactName = user.SecondaryContactName,
                SecondaryContactPhone = user.SecondaryContactPhone,
                SecondaryContactRelation = user.SecondaryContactRelation,
                MedicalConditions = user.MedicalConditions,
                Allergies = user.Allergies,
                BloodGroup = user.BloodGroup,
                EmergencyAddress = user.EmergencyAddress
            });
        }

        /// <summary>
        /// Get user's bank details
        /// </summary>
        [HttpGet("bank-details/{userId}")]
        [ProducesResponseType(typeof(BankDetailsResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> GetBankDetails(long userId)
        {
            var user = await _db.TblDeliveryusers
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
                return NotFound(new ErrorResponse { Message = "User not found." });

            return Ok(new BankDetailsResponse
            {
                AccountHolderName = user.AccountHolderName,
                AccountNumber = user.AccountNo,
                IfscCode = user.Ifsccode,
                BankName = user.BankName,
                BranchName = user.BranchName,
                AccountType = user.AccountType,
                UpiId = user.UpiId
            });
        }

        /// <summary>
        /// Get user's vehicle details
        /// </summary>
        [HttpGet("vehicle-details/{userId}")]
        [ProducesResponseType(typeof(VehicleDetailsResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> GetVehicleDetails(long userId)
        {
            var user = await _db.TblDeliveryusers
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
                return NotFound(new ErrorResponse { Message = "User not found." });

            return Ok(new VehicleDetailsResponse
            {
                VehicleType = user?.VehicleType,
                VehicleNumber = user?.VehicleNumber,
                Brand = user?.VehicleBrand,
                Model = user?.VehicleModel,
                Year = user?.VehicleYear,
                Color = user?.VehicleColor,
                RegistrationDate = user?.RegistrationDate,
                InsuranceNumber = user?.InsuranceNumber,
                DrivingLicense = user?.DrivingLicenseNumber
            });
        }
    }

    // Request Models
    public class EmergencyDetailsRequest
    {
        [Required]
        public string PrimaryContactName { get; set; } = string.Empty;
        [Required]
        public string PrimaryContactPhone { get; set; } = string.Empty;
        [Required]
        public string PrimaryContactRelation { get; set; } = string.Empty;
        public string? SecondaryContactName { get; set; }
        public string? SecondaryContactPhone { get; set; }
        public string? SecondaryContactRelation { get; set; }
        public string? MedicalConditions { get; set; }
        public string? Allergies { get; set; }
        public string? BloodGroup { get; set; }
        public string? EmergencyAddress { get; set; }
    }

    public class BankDetailsRequest
    {
        [Required]
        public string AccountHolderName { get; set; } = string.Empty;
        [Required]
        public string AccountNumber { get; set; } = string.Empty;
        [Required]
        public string ConfirmAccountNumber { get; set; } = string.Empty;
        [Required]
        public string IfscCode { get; set; } = string.Empty;
        [Required]
        public string BankName { get; set; } = string.Empty;
        [Required]
        public string BranchName { get; set; } = string.Empty;
        [Required]
        public string AccountType { get; set; } = string.Empty;
        public string? UpiId { get; set; }
    }

    public class VehicleDetailsRequest
    {
        [Required]
        public string VehicleType { get; set; } = string.Empty;
        [Required]
        public string VehicleNumber { get; set; } = string.Empty;
        [Required]
        public string Brand { get; set; } = string.Empty;
        [Required]
        public string Model { get; set; } = string.Empty;
        public string? Year { get; set; }
        public string? Color { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string? InsuranceNumber { get; set; }
        public string? DrivingLicense { get; set; }
    }

    // Response Models
    public class EmergencyDetailsResponse
    {
        public string? PrimaryContactName { get; set; }
        public string? PrimaryContactPhone { get; set; }
        public string? PrimaryContactRelation { get; set; }
        public string? SecondaryContactName { get; set; }
        public string? SecondaryContactPhone { get; set; }
        public string? SecondaryContactRelation { get; set; }
        public string? MedicalConditions { get; set; }
        public string? Allergies { get; set; }
        public string? BloodGroup { get; set; }
        public string? EmergencyAddress { get; set; }
    }

    public class BankDetailsResponse
    {
        public string? AccountHolderName { get; set; }
        public string? AccountNumber { get; set; }
        public string? IfscCode { get; set; }
        public string? BankName { get; set; }
        public string? BranchName { get; set; }
        public string? AccountType { get; set; }
        public string? UpiId { get; set; }
    }

    public class VehicleDetailsResponse
    {
        public string? VehicleType { get; set; }
        public string? VehicleNumber { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Year { get; set; }
        public string? Color { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string? InsuranceNumber { get; set; }
        public string? DrivingLicense { get; set; }
    }

    public class UserDetailsStatusResponse
    {
        public long UserId { get; set; }
        public int CurrentStep { get; set; }
        public bool PersonalInfoComplete { get; set; }
        public bool DocumentsUploaded { get; set; }
        public bool EmergencyDetailsComplete { get; set; }
        public bool BankDetailsComplete { get; set; }
        public bool VehicleDetailsComplete { get; set; }
        public bool ProfileComplete { get; set; }
        public string RegistrationStatus { get; set; } = string.Empty;
    }

    public class ApiResponse
    {
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
        public object? Data { get; set; }
    }
}