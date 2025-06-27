using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class VwImage
{
    public long Id { get; set; }

    public string Image { get; set; } = null!;

    public string? ProductId { get; set; }

    public DateTime AddedDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool? IsActive { get; set; }

    public string Imagepath { get; set; } = null!;

    public string Imagepath1 { get; set; } = null!;

    public string Imagepath2 { get; set; } = null!;
}
