using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class ViewBlog
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public string? SeoFriendlyUrl { get; set; }

    public string? Content { get; set; }

    public string? Category { get; set; }

    public string? Date { get; set; }

    public string? Image1 { get; set; }

    public string? Descr { get; set; }

    public string? Keywords { get; set; }
}
