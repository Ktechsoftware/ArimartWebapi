using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models
{
    public class UploadDocument
    {
        public int DocumentId { get; set; }
        public int UserId { get; set; }
        public string? DocumentType { get; set; }
        public string? FrontImagePath { get; set; }
        public string? BackImagePath { get; set; }
        public DateTime UploadDate { get; set; }
        public bool IsVerified { get; set; }    
        public bool IsDeleted { get; set; }
        public int? VerifiedBy { get; set; }
        public DateTime? VerifiedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

}
