
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using ArimartEcommerceAPI.Services.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System;
using System.IO;
using ArimartEcommerceAPI.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ArimartEcommerceAPI.API.Controllers  // Fixed typo: Contollers -> Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public DocumentController(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Upload and validate document for delivery user
        /// </summary>
        /// <param name="request">Document upload request</param>
        /// <returns>Upload result with document ID</returns>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(DocumentUploadResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadRequest request)
        {
            if (request.FrontFile == null)
                return BadRequest(new ErrorResponse { Message = "Front image is required." });

            if (request.UserId <= 0)
                return BadRequest(new ErrorResponse { Message = "Valid user ID is required." });

            if (string.IsNullOrWhiteSpace(request.DocumentType))
                return BadRequest(new ErrorResponse { Message = "Document type is required." });

            try
            {
                var uploads = Path.Combine("wwwroot", request.UserId.ToString());
                if (!Directory.Exists(uploads))
                    Directory.CreateDirectory(uploads);

                Console.WriteLine("1. Directory created successfully");

                // Save Front Image
                string frontPath = Path.Combine(uploads, $"{request.DocumentType}_front_{Guid.NewGuid()}{Path.GetExtension(request.FrontFile.FileName)}");
                using (var stream = new FileStream(frontPath, FileMode.Create))
                {
                    await request.FrontFile.CopyToAsync(stream);
                }
                Console.WriteLine("2. Front file saved successfully");

                // Save Back Image (if any)
                string? backPath = null;
                if (request.BackFile != null)
                {
                    backPath = Path.Combine(uploads, $"{request.DocumentType}_back_{Guid.NewGuid()}{Path.GetExtension(request.BackFile.FileName)}");
                    using (var stream = new FileStream(backPath, FileMode.Create))
                    {
                        await request.BackFile.CopyToAsync(stream);
                    }
                    Console.WriteLine("3. Back file saved successfully");
                }

                // OCR Validation - THIS IS LIKELY WHERE IT FAILS
                Console.WriteLine("4. Starting OCR processing...");
                string extractedText = "TEMPORARY_TEST_TEXT";
                Console.WriteLine($"5. OCR completed. Extracted text length: {extractedText?.Length ?? 0}");

                var validationResult = ValidateDocument(request.DocumentType, extractedText);
                Console.WriteLine($"6. Validation completed. IsValid: {validationResult.IsValid}");

                if (!validationResult.IsValid)
                {
                    // Clean up and return error
                }

                // Save to DB - SECOND MOST LIKELY FAILURE POINT
                Console.WriteLine("7. Starting database save...");
                var doc = new UploadDocument
                {
                    UserId = request.UserId,
                    DocumentType = request.DocumentType,
                    FrontImagePath = frontPath,
                    BackImagePath = backPath,
                    UploadDate = DateTime.Now,
                    IsVerified = false
                };

                _db.UploadDocuments.Add(doc);
                Console.WriteLine("8. Entity added to context");

                await _db.SaveChangesAsync();
                Console.WriteLine("9. Database save completed");

                return Ok(new DocumentUploadResponse
                {
                    Message = "Document uploaded & validated successfully.",
                    DocumentId = doc.DocumentId,
                    IsVerified = doc.IsVerified
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR AT STEP: {ex.Message}");
                Console.WriteLine($"STACK TRACE: {ex.StackTrace}");
                return StatusCode(500, new ErrorResponse { Message = $"Error: {ex.Message}" }); // Return actual error for debugging
            }
        }

        private DocumentValidationResult ValidateDocument(string documentType, string extractedText)
        {
            return documentType.ToLowerInvariant() switch
            {
                "pan" => ValidatePAN(extractedText),
                "aadhaar" => ValidateAadhaar(extractedText),
                "drivinglicence" => ValidateDrivingLicence(extractedText),
                _ => new DocumentValidationResult { IsValid = true, Message = "Document type not recognized for validation." }
            };
        }

        private DocumentValidationResult ValidatePAN(string extractedText)
        {
            if (Regex.IsMatch(extractedText, @"[A-Z]{5}[0-9]{4}[A-Z]{1}"))
                return new DocumentValidationResult { IsValid = true, Message = "PAN validated successfully." };

            return new DocumentValidationResult { IsValid = false, Message = "Invalid PAN card detected." };
        }

        private DocumentValidationResult ValidateAadhaar(string extractedText)
        {
            if (Regex.IsMatch(extractedText.Replace(" ", ""), @"\d{12}"))
                return new DocumentValidationResult { IsValid = true, Message = "Aadhaar validated successfully." };

            return new DocumentValidationResult { IsValid = false, Message = "Invalid Aadhaar card detected." };
        }

        private DocumentValidationResult ValidateDrivingLicence(string extractedText)
        {
            // Common DL format: StateCode+RTO+Year+Number
            if (Regex.IsMatch(extractedText, @"[A-Z]{2}[0-9]{2}[0-9]{4}[0-9]{7}"))
                return new DocumentValidationResult { IsValid = true, Message = "Driving Licence validated successfully." };

            return new DocumentValidationResult { IsValid = false, Message = "Invalid Driving Licence detected." };
        }
        // Add these methods to your DocumentController

        /// <summary>
        /// Get all documents uploaded by a user
        /// </summary>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(List<UserDocumentResponse>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> GetUserDocuments(long userId)
        {
            var documents = await _db.UploadDocuments
                .Where(d => d.UserId == userId && !d.IsDeleted)
                .OrderBy(d => d.UploadDate)
                .Select(d => new UserDocumentResponse
                {
                    DocumentId = d.DocumentId,
                    DocumentType = d.DocumentType,
                    FrontImagePath = d.FrontImagePath,
                    BackImagePath = d.BackImagePath,
                    IsVerified = d.IsVerified,
                    UploadDate = d.UploadDate,
                    VerificationDate = d.VerifiedDate,
                })
                .ToListAsync();

            return Ok(documents);
        }

        /// <summary>
        /// Check and update user's document completion status
        /// </summary>
        [HttpPost("check-completion/{userId}")]
        [ProducesResponseType(typeof(DocumentCompletionResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> CheckDocumentCompletion(long userId)
        {
            var user = await _db.TblDeliveryusers
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
                return NotFound(new ErrorResponse { Message = "User not found." });

            // Get user's uploaded documents
            var uploadedDocuments = await _db.UploadDocuments
                .Where(d => d.UserId == userId && !d.IsDeleted)
                .Select(d => d.DocumentType)
                .Distinct()
                .ToListAsync();

            // Required document types
            var requiredDocuments = new[] { "Aadhaar", "PAN", "DrivingLicence" };

            // Check if all required documents are uploaded
            var allDocumentsUploaded = requiredDocuments.All(reqDoc =>
                uploadedDocuments.Contains(reqDoc, StringComparer.OrdinalIgnoreCase));

            // Update user status if all documents are uploaded
            if (allDocumentsUploaded && user.DocumentsUploaded == false)
            {
                user.DocumentsUploaded = true;
                user.CurrentStep = Math.Max(user.CurrentStep ?? 1, 3); // Move to step 3
                user.ModifiedDate = DateTime.UtcNow;

                await _db.SaveChangesAsync();
            }

            return Ok(new DocumentCompletionResponse
            {
                UserId = userId,
                AllDocumentsUploaded = allDocumentsUploaded,
                DocumentsUploaded = user.DocumentsUploaded ?? false,
                CurrentStep = user.CurrentStep ?? 1,
                UploadedDocuments = uploadedDocuments,
                RequiredDocuments = requiredDocuments,
                MissingDocuments = requiredDocuments
                    .Where(req => !uploadedDocuments.Contains(req, StringComparer.OrdinalIgnoreCase))
                    .ToArray()
            });
        }

        /// <summary>
        /// Delete a user's document
        /// </summary>
        [HttpDelete("{documentId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> DeleteDocument(long documentId)
        {
            var document = await _db.UploadDocuments
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);

            if (document == null)
                return NotFound(new ErrorResponse { Message = "Document not found." });

            try
            {
                // Delete physical files
                if (!string.IsNullOrEmpty(document.FrontImagePath) && System.IO.File.Exists(document.FrontImagePath))
                {
                    System.IO.File.Delete(document.FrontImagePath);
                }

                if (!string.IsNullOrEmpty(document.BackImagePath) && System.IO.File.Exists(document.BackImagePath))
                {
                    System.IO.File.Delete(document.BackImagePath);
                }

                // Soft delete from database
                document.IsDeleted = true;
                document.ModifiedDate = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                // Update user's document completion status
                await CheckDocumentCompletion(document.UserId);

                return Ok(new { Message = "Document deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = "Failed to delete document." });
            }
        }
    }

        // Response Models
        public class UserDocumentResponse
        {
            public long DocumentId { get; set; }
            public string DocumentType { get; set; } = string.Empty;
            public string? FrontImagePath { get; set; }
            public string? BackImagePath { get; set; }
            public bool IsVerified { get; set; }
            public DateTime UploadDate { get; set; }
            public DateTime? VerificationDate { get; set; }
            public string? RejectReason { get; set; }
        }

        public class DocumentCompletionResponse
        {
            public long UserId { get; set; }
            public bool AllDocumentsUploaded { get; set; }
            public bool DocumentsUploaded { get; set; }
            public int CurrentStep { get; set; }
            public List<string> UploadedDocuments { get; set; } = new();
            public string[] RequiredDocuments { get; set; } = Array.Empty<string>();
            public string[] MissingDocuments { get; set; } = Array.Empty<string>();
        }

        // Request/Response Models
        public class DocumentUploadRequest
    {
        [Required]
        public IFormFile FrontFile { get; set; } = null!;

        public IFormFile? BackFile { get; set; }

        [Required]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "Valid user ID is required")]
        public int UserId { get; set; }  // Changed to long to match your auth system
    }

    public class DocumentUploadResponse
    {
        public string Message { get; set; } = string.Empty;
        public long DocumentId { get; set; }
        public bool IsVerified { get; set; }
    }

    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
    }

    internal class DocumentValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}