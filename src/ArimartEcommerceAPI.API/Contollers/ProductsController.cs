using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/products?page=1&pageSize=10
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<ProductsResponse>> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            // Ensure reasonable pagination limits
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var query = _context.VwProducts
                .Where(p => p.IsDeleted == false && p.IsActive == true)
                .OrderByDescending(p => p.AddedDate);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new ProductsResponse
            {
                Products = products,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };

            return Ok(response);
        }

        // GET: api/products/{id}/image-url
        [AllowAnonymous]
        [HttpGet("{id}/image-url")]
        public async Task<ActionResult<string>> GetProductImageUrl(int id)
        {
            var product = await _context.VwProducts
                .Where(p => p.Id == id && p.IsDeleted == false)
                .Select(p => p.Image)
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(product))
            {
                return Ok(null);
            }

            var imageUrl = $"{Request.Scheme}://{Request.Host}/Uploads/{product}";
            return Ok(imageUrl);
        }

        // GET: api/products/{id}
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<VwProduct>> GetProduct(long id)
        {
            var product = await _context.VwProducts
                .FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted == false && p.IsActive == true);

            if (product == null)
                return NotFound();

            return product;
        }

        // GET: api/products/search?query=radish&page=1&pageSize=10


        // Main search endpoint with suggestions
        [AllowAnonymous]
        [HttpGet("search")]
        public async Task<ActionResult<SearchResponse>> SearchWithSuggestions(
            [FromQuery] string query,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool showAll = false,
            [FromQuery] bool includeSuggestions = true)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Search query is required." });

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            // Get all active products
            var allProducts = await _context.VwProducts
                .Where(p => p.IsDeleted == false && p.IsActive == true)
                .ToListAsync();

            var queryLower = query.ToLower();

            // Create ranked search results
            var searchResults = allProducts
                .Select(p => new
                {
                    Product = p,
                    Rank = CalculateSearchRank(p, queryLower)
                })
                .Where(x => x.Rank > 0)
                .OrderByDescending(x => x.Rank)
                .ThenByDescending(x => x.Product.AddedDate)
                .Select(x => x.Product)
                .ToList();

            // Generate search suggestions
            var suggestions = new List<SearchSuggestion>();
            if (includeSuggestions)
            {
                suggestions = await GenerateSearchSuggestions(query, allProducts);
            }

            var totalCount = searchResults.Count;

            List<VwProduct> products;
            int totalPages;
            bool hasNextPage;
            bool hasPreviousPage;
            int currentPage;
            int currentPageSize;

            if (showAll)
            {
                products = searchResults;
                totalPages = 1;
                hasNextPage = false;
                hasPreviousPage = false;
                currentPage = 1;
                currentPageSize = totalCount;
            }
            else
            {
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                products = searchResults
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                hasNextPage = page < totalPages;
                hasPreviousPage = page > 1;
                currentPage = page;
                currentPageSize = pageSize;
            }

            var response = new SearchResponse
            {
                Products = products,
                Suggestions = suggestions,
                CurrentPage = currentPage,
                PageSize = currentPageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = hasNextPage,
                HasPreviousPage = hasPreviousPage,
                SearchQuery = query,
                ShowAll = showAll
            };

            return Ok(response);
        }

        // Separate endpoint for autocomplete/suggestions only
        [AllowAnonymous]
        [HttpGet("autocomplete")]
        public async Task<ActionResult<List<SearchSuggestion>>> GetAutocomplete([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Ok(new List<SearchSuggestion>());

            var allProducts = await _context.VwProducts
                .Where(p => p.IsDeleted == false && p.IsActive == true)
                .ToListAsync();

            var suggestions = await GenerateSearchSuggestions(query, allProducts);

            return Ok(suggestions.Take(10).ToList()); // Limit to 10 suggestions
        }

        private async Task<List<SearchSuggestion>> GenerateSearchSuggestions(string query, List<VwProduct> allProducts)
        {
            var suggestions = new List<SearchSuggestion>();
            var queryLower = query.ToLower();

            // 1. Exact product name matches (highest priority)
            var exactProductMatches = allProducts
                .Where(p => !string.IsNullOrEmpty(p.ProductName) &&
                           p.ProductName.ToLower().Contains(queryLower))
                .GroupBy(p => p.ProductName.ToLower())
                .Select(g => new SearchSuggestion
                {
                    Text = g.First().ProductName,
                    Type = "product",
                    MatchCount = g.Count(),
                    Priority = g.First().ProductName.ToLower().StartsWith(queryLower) ? 1000 : 500
                })
                .OrderByDescending(s => s.Priority)
                .Take(3)
                .ToList();

            suggestions.AddRange(exactProductMatches);

            // 2. Keyword suggestions
            var keywordSuggestions = allProducts
                .Where(p => !string.IsNullOrEmpty(p.Keywords))
                .SelectMany(p => p.Keywords.ToLower().Split(',')
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrEmpty(k) && k.Contains(queryLower))
                    .Select(k => new { Keyword = k, Product = p }))
                .GroupBy(x => x.Keyword)
                .Select(g => new SearchSuggestion
                {
                    Text = g.Key,
                    Type = "keyword",
                    MatchCount = g.Count(),
                    Priority = g.Key.StartsWith(queryLower) ? 800 : 400
                })
                .OrderByDescending(s => s.Priority)
                .ThenByDescending(s => s.MatchCount)
                .Take(5)
                .ToList();

            suggestions.AddRange(keywordSuggestions);

            // 3. Category suggestions
            var categorySuggestions = allProducts
                .Where(p => (!string.IsNullOrEmpty(p.CategoryName) && p.CategoryName.ToLower().Contains(queryLower)) ||
                           (!string.IsNullOrEmpty(p.SubcategoryName) && p.SubcategoryName.ToLower().Contains(queryLower)) ||
                           (!string.IsNullOrEmpty(p.ChildCategoryName) && p.ChildCategoryName.ToLower().Contains(queryLower)))
                .SelectMany(p => new[]
                {
            new { Name = p.CategoryName, Type = "category", Product = p },
            new { Name = p.SubcategoryName, Type = "subcategory", Product = p },
            new { Name = p.ChildCategoryName, Type = "childcategory", Product = p }
                })
                .Where(x => !string.IsNullOrEmpty(x.Name) && x.Name.ToLower().Contains(queryLower))
                .GroupBy(x => new { x.Name, x.Type })
                .Select(g => new SearchSuggestion
                {
                    Text = g.Key.Name,
                    Type = g.Key.Type,
                    Category = g.Key.Type,
                    MatchCount = g.Count(),
                    Priority = g.Key.Name.ToLower().StartsWith(queryLower) ? 600 : 300
                })
                .OrderByDescending(s => s.Priority)
                .Take(3)
                .ToList();

            suggestions.AddRange(categorySuggestions);

            // 4. Auto-complete suggestions (combinations)
            var autoCompleteSuggestions = new List<SearchSuggestion>();

            // Generate combinations like "kela chips", "kela combo", etc.
            var baseKeywords = allProducts
                .Where(p => !string.IsNullOrEmpty(p.Keywords))
                .SelectMany(p => p.Keywords.ToLower().Split(',').Select(k => k.Trim()))
                .Where(k => !string.IsNullOrEmpty(k) && k.StartsWith(queryLower))
                .Distinct()
                .ToList();

            foreach (var baseKeyword in baseKeywords.Take(3))
            {
                // Find products with this keyword and get related terms
                var relatedTerms = allProducts
                    .Where(p => !string.IsNullOrEmpty(p.Keywords) &&
                               p.Keywords.ToLower().Contains(baseKeyword))
                    .SelectMany(p => p.ProductName.ToLower().Split(' '))
                    .Where(w => w.Length > 2 && w != baseKeyword.ToLower() && w != queryLower)
                    .GroupBy(w => w)
                    .OrderByDescending(g => g.Count())
                    .Take(3)
                    .Select(g => g.Key)
                    .ToList();

                foreach (var term in relatedTerms)
                {
                    var suggestionText = $"{baseKeyword} {term}";
                    var matchCount = allProducts.Count(p =>
                        (!string.IsNullOrEmpty(p.ProductName) && p.ProductName.ToLower().Contains(suggestionText)) ||
                        (!string.IsNullOrEmpty(p.Keywords) && p.Keywords.ToLower().Contains(baseKeyword)));

                    if (matchCount > 0)
                    {
                        autoCompleteSuggestions.Add(new SearchSuggestion
                        {
                            Text = suggestionText,
                            Type = "autocomplete",
                            MatchCount = matchCount,
                            Priority = 200
                        });
                    }
                }
            }

            suggestions.AddRange(autoCompleteSuggestions.Take(4));

            // Remove duplicates and sort by priority
            return suggestions
                .GroupBy(s => s.Text.ToLower())
                .Select(g => g.OrderByDescending(s => s.Priority).First())
                .OrderByDescending(s => s.Priority)
                .ThenByDescending(s => s.MatchCount)
                .ToList();
        }

        private int CalculateSearchRank(VwProduct product, string query)
        {
            int rank = 0;

            // Check product name
            if (!string.IsNullOrEmpty(product.ProductName))
            {
                var productName = product.ProductName.ToLower();
                if (productName.Equals(query, StringComparison.OrdinalIgnoreCase))
                    rank += 1000;
                else if (productName.StartsWith(query))
                    rank += 500;
                else if (productName.Contains(query))
                    rank += 100;
            }

            // Check keywords
            if (!string.IsNullOrEmpty(product.Keywords))
            {
                var keywords = product.Keywords.ToLower().Split(',')
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrEmpty(k))
                    .ToList();

                foreach (var keyword in keywords)
                {
                    if (keyword.Equals(query, StringComparison.OrdinalIgnoreCase))
                    {
                        rank += 800;
                        break;
                    }
                }

                if (rank < 800)
                {
                    foreach (var keyword in keywords)
                    {
                        if (keyword.Contains(query))
                        {
                            rank += 200;
                            break;
                        }
                    }
                }
            }

            // Check other fields
            if (!string.IsNullOrEmpty(product.Shortdesc) && product.Shortdesc.ToLower().Contains(query))
                rank += 50;

            if (!string.IsNullOrEmpty(product.PPros) && product.PPros.ToLower().Contains(query))
                rank += 25;

            return rank;
        }

        // GET: api/products/names?query=veg
        [AllowAnonymous]
        [HttpGet("names")]
        public async Task<ActionResult<IEnumerable<string>>> GetProductNames([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                var defaultNames = await _context.VwProducts
                    .Where(p => !p.IsDeleted && p.IsActive == true && !string.IsNullOrEmpty(p.ProductName))
                    .OrderBy(p => p.ProductName)
                    .Select(p => p.ProductName)
                    .Take(10)
                    .ToListAsync();
                return Ok(defaultNames);
            }

            var productNames = await _context.VwProducts
                .Where(p => !p.IsDeleted && p.IsActive == true &&
                            !string.IsNullOrEmpty(p.ProductName) &&
                            p.ProductName.ToLower().Contains(query.ToLower()))
                .OrderBy(p => p.ProductName)
                .Select(p => p.ProductName)
                .Take(15)
                .ToListAsync();

            return Ok(productNames);
        }

        // GET: api/products/by-childcategory/5?page=1&pageSize=10
        [AllowAnonymous]
        [HttpGet("by-childcategory/{childCategoryId}")]
        public async Task<ActionResult<ProductsResponse>> GetProductsByChildCategory(
            int childCategoryId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var query = _context.VwProducts
               .Where(p => p.ChildCategoryId == childCategoryId && !p.IsDeleted && p.IsActive == true)
                .OrderByDescending(p => p.AddedDate);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new ProductsResponse
            {
                Products = products,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("by-subcategory/{subCategoryId}")]
        public async Task<ActionResult<ProductsResponse>> GetProductsBySubCategory(
            int subCategoryId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var query = _context.VwProducts
               .Where(p => p.Subcategoryid == subCategoryId && !p.IsDeleted && p.IsActive == true)
                .OrderByDescending(p => p.AddedDate);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new ProductsResponse
            {
                Products = products,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };

            return Ok(response);
        }

        // ✅ GET: GroupBuys by ProductId (Only gid, pid, pdid)
        [AllowAnonymous]
        [HttpGet("groupbuy/{pid}")]
        public async Task<ActionResult<IEnumerable<object>>> GetGroupIdsByProduct(long pid)
        {
            var groups = await _context.VwGroups
                .Where(g => g.Pid == pid && g.IsDeleted1 == false)
                .Select(g => new
                {
                    g.Gid,
                    g.Pid,
                    g.Pdid
                })
                .ToListAsync();

            if (groups == null || !groups.Any())
                return NotFound("No group deals found for this product.");

            return Ok(groups);
        }

        // ✅ GET: GroupBuys by ProductId + ProductDetailId (Only gid, pid, pdid)
        [AllowAnonymous]
        [HttpGet("groupbuy/{pid}/{pdid}")]
        public async Task<ActionResult<IEnumerable<object>>> GetGroupIdsByProductDetail(long pid, long pdid)
        {
            var groups = await _context.VwGroups
                .Where(g => g.Pid == pid && g.Pdid == pdid && g.IsDeleted1 == false)
                .Select(g => new
                {
                    g.Gid,
                    g.Pid,
                    g.Pdid
                })
                .ToListAsync();

            if (groups == null || !groups.Any())
                return NotFound("No group deals found for this product and detail.");

            return Ok(groups);
        }


        // ====================  FILTER SEARCH PRODUCTS =========================
        // Enhanced Filter Models

        // Enhanced ProductController Methods
        [AllowAnonymous]
        [HttpPost("search/filtered")]
        public async Task<ActionResult<EnhancedSearchResponse>> SearchWithFilters([FromBody] FilterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return BadRequest(new { message = "Search query is required." });

            // Validate pagination parameters
            request.Page = Math.Max(1, request.Page);
            request.PageSize = Math.Clamp(request.PageSize, 1, 50);

            try
            {
                // Build the query with filters applied at database level
                var query = _context.VwProducts
                    .Where(p => p.IsDeleted == false && p.IsActive == true);

                // Apply search filter at database level
                var searchQuery = request.Query.ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(searchQuery) ||
                    p.Keywords.ToLower().Contains(searchQuery) ||
                    p.Shortdesc.ToLower().Contains(searchQuery));

                // Apply category filters
                if (request.CategoryIds.Any())
                {
                    query = query.Where(p => request.CategoryIds.Contains(p.Categoryid ?? 0));
                }

                if (request.SubcategoryIds.Any())
                {
                    query = query.Where(p => request.SubcategoryIds.Contains(p.Subcategoryid ?? 0));
                }

                if (request.ChildCategoryIds.Any())
                {
                    query = query.Where(p => request.ChildCategoryIds.Contains(p.ChildCategoryId ?? 0));
                }

                // Apply brand filters
                if (request.Brands.Any())
                {
                    query = query.Where(p => request.Brands.Contains(p.CompanyName));
                }

                if (request.CompanyNames.Any())
                {
                    query = query.Where(p => request.CompanyNames.Contains(p.CompanyName));
                }

                // Apply weight type filters
                if (request.WeightTypes.Any())
                {
                    query = query.Where(p => request.WeightTypes.Contains(p.Wtype));
                }

                // Apply pay on delivery filter
                if (request.PayOnDelivery.HasValue && request.PayOnDelivery.Value)
                {
                    //query = query.Where(p => p.CashOnDelivery == true);
                }

                // Get filtered products for further processing
                var filteredProducts = await query.ToListAsync();

                // Apply complex filters that require calculations
                var processedProducts = ApplyComplexFilters(filteredProducts, request);

                // Apply search ranking
                var rankedProducts = ApplySearchRanking(processedProducts, searchQuery);

                // Apply sorting
                var sortedProducts = ApplySorting(rankedProducts, request.SortBy);

                // Generate available filters based on all search results (before pagination)
                var availableFilters = await GenerateAvailableFilters(filteredProducts, request);

                // Generate search suggestions
                var suggestions = new List<SearchSuggestion>();
                if (request.IncludeSuggestions)
                {
                    suggestions = await GenerateSearchSuggestions(request.Query, filteredProducts);
                }

                // Apply pagination
                var paginationResult = ApplyPagination(sortedProducts, request);

                var response = new EnhancedSearchResponse
                {
                    Products = paginationResult.Products,
                    Suggestions = suggestions,
                    CurrentPage = paginationResult.CurrentPage,
                    PageSize = paginationResult.PageSize,
                    TotalCount = paginationResult.TotalCount,
                    TotalPages = paginationResult.TotalPages,
                    HasNextPage = paginationResult.HasNextPage,
                    HasPreviousPage = paginationResult.HasPreviousPage,
                    SearchQuery = request.Query,
                    ShowAll = request.ShowAll,
                    Filters = availableFilters,
                    SortOptions = GetSortOptions(request.SortBy),
                    CurrentSort = request.SortBy
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [AllowAnonymous]
        [HttpGet("filters")]
        public async Task<ActionResult<AvailableFilters>> GetAvailableFilters(
            [FromQuery] string query = "",
            [FromQuery] int? categoryId = null,
            [FromQuery] int? subcategoryId = null)
        {
            try
            {
                var baseQuery = _context.VwProducts
                    .Where(p => p.IsDeleted == false && p.IsActive == true);

                // Apply basic filters if provided
                if (!string.IsNullOrWhiteSpace(query))
                {
                    var queryLower = query.ToLower();
                    baseQuery = baseQuery.Where(p =>
                        p.ProductName.ToLower().Contains(queryLower) ||
                        p.Keywords.ToLower().Contains(queryLower) ||
                        p.Shortdesc.ToLower().Contains(queryLower));
                }

                if (categoryId.HasValue)
                {
                    baseQuery = baseQuery.Where(p => p.Categoryid == categoryId.Value);
                }

                if (subcategoryId.HasValue)
                {
                    baseQuery = baseQuery.Where(p => p.Subcategoryid == subcategoryId.Value);
                }

                var filteredProducts = await baseQuery.ToListAsync();
                var request = new FilterRequest { Query = query ?? string.Empty };
                var filters = await GenerateAvailableFilters(filteredProducts, request);

                return Ok(filters);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, new { message = "An error occurred while retrieving filters." });
            }
        }

        private List<VwProduct> ApplyComplexFilters(List<VwProduct> products, FilterRequest request)
        {
            var filteredProducts = products.AsEnumerable();

            // Price filters
            if (request.PriceMin.HasValue)
            {
                filteredProducts = filteredProducts.Where(p =>
                    decimal.TryParse(p.Price, out var price) && price >= request.PriceMin.Value);
            }

            if (request.PriceMax.HasValue)
            {
                filteredProducts = filteredProducts.Where(p =>
                    decimal.TryParse(p.Price, out var price) && price <= request.PriceMax.Value);
            }

            // Discount price filters
            if (request.DiscountPriceMin.HasValue)
            {
                filteredProducts = filteredProducts.Where(p =>
                    decimal.TryParse(p.Discountprice, out var discountPrice) && discountPrice >= request.DiscountPriceMin.Value);
            }

            if (request.DiscountPriceMax.HasValue)
            {
                filteredProducts = filteredProducts.Where(p =>
                    decimal.TryParse(p.Discountprice, out var discountPrice) && discountPrice <= request.DiscountPriceMax.Value);
            }

            // Discount percentage filters
            if (request.MinDiscountPercentage.HasValue)
            {
                filteredProducts = filteredProducts.Where(p =>
                    CalculateDiscountPercentage(p) >= request.MinDiscountPercentage.Value);
            }

            if (request.DiscountRanges.Any())
            {
                filteredProducts = filteredProducts.Where(p =>
                    request.DiscountRanges.Any(dr =>
                        decimal.TryParse(dr, out var discountThreshold) &&
                        CalculateDiscountPercentage(p) >= discountThreshold));
            }

            // Weight filters
            if (request.WeightMin.HasValue)
            {
                filteredProducts = filteredProducts.Where(p =>
                    decimal.TryParse(p.Wweight, out var weight) && weight >= request.WeightMin.Value);
            }

            if (request.WeightMax.HasValue)
            {
                filteredProducts = filteredProducts.Where(p =>
                    decimal.TryParse(p.Wweight, out var weight) && weight <= request.WeightMax.Value);
            }

            // Attribute filters
            if (request.Occasions.Any())
            {
                filteredProducts = filteredProducts.Where(p =>
                    request.Occasions.Any(o =>
                        (!string.IsNullOrEmpty(p.Keywords) && p.Keywords.Contains(o, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(p.ProductName) && p.ProductName.Contains(o, StringComparison.OrdinalIgnoreCase))));
            }

            if (request.Flavors.Any())
            {
                filteredProducts = filteredProducts.Where(p =>
                    request.Flavors.Any(f =>
                        (!string.IsNullOrEmpty(p.Keywords) && p.Keywords.Contains(f, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(p.ProductName) && p.ProductName.Contains(f, StringComparison.OrdinalIgnoreCase))));
            }

            if (request.FoodTypes.Any())
            {
                filteredProducts = filteredProducts.Where(p =>
                    request.FoodTypes.Any(ft =>
                        (!string.IsNullOrEmpty(p.Keywords) && p.Keywords.Contains(ft, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(p.ProductName) && p.ProductName.Contains(ft, StringComparison.OrdinalIgnoreCase))));
            }

            return filteredProducts.ToList();
        }

        private List<VwProduct> ApplySearchRanking(List<VwProduct> products, string searchQuery)
        {
            return products
                .Select(p => new { Product = p, Rank = CalculateSearchRank(p, searchQuery) })
                .Where(x => x.Rank > 0)
                .OrderByDescending(x => x.Rank)
                .Select(x => x.Product)
                .ToList();
        }

        private List<VwProduct> ApplySorting(List<VwProduct> products, string sortBy)
        {
            return sortBy?.ToLower() switch
            {
                "price_asc" => products.OrderBy(p => decimal.TryParse(p.Price, out var price) ? price : decimal.MaxValue).ToList(),
                "price_desc" => products.OrderByDescending(p => decimal.TryParse(p.Price, out var price) ? price : 0).ToList(),
                "discount_desc" => products.OrderByDescending(p => CalculateDiscountPercentage(p)).ToList(),
                "newest" => products.OrderByDescending(p => p.AddedDate).ToList(),
                "name_asc" => products.OrderBy(p => p.ProductName).ToList(),
                // "customer_rating" => products.OrderByDescending(p => p. ?? 0).ToList(),
                _ => products // Default: relevance (already sorted by rank)
            };
        }

        private PaginationResult ApplyPagination(List<VwProduct> sortedProducts, FilterRequest request)
        {
            var totalCount = sortedProducts.Count;

            if (request.ShowAll)
            {
                return new PaginationResult
                {
                    Products = sortedProducts,
                    CurrentPage = 1,
                    PageSize = totalCount,
                    TotalCount = totalCount,
                    TotalPages = 1,
                    HasNextPage = false,
                    HasPreviousPage = false
                };
            }

            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
            var products = sortedProducts
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PaginationResult
            {
                Products = products,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = request.Page < totalPages,
                HasPreviousPage = request.Page > 1
            };
        }

        //private decimal CalculateDiscountPercentage(VwProduct product)
        //{
        //    if (decimal.TryParse(product.Price, out var price) &&
        //        decimal.TryParse(product.Discountprice, out var discountPrice) &&
        //        price > 0 && discountPrice > 0 && price > discountPrice)
        //    {
        //        return Math.Round(((price - discountPrice) / price) * 100, 2);
        //    }
        //    return 0;
        //}

        private async Task<AvailableFilters> GenerateAvailableFilters(List<VwProduct> products, FilterRequest request)
        {
            var filters = new AvailableFilters();

            // Generate category filters with hierarchical structure
            var categoryGroups = products
                .Where(p => p.Categoryid.HasValue)
                .GroupBy(p => new { p.Categoryid, p.CategoryName })
                .Select(g => new CategoryFilter
                {
                    Id = g.Key.Categoryid ?? 0,
                    Name = g.Key.CategoryName ?? "Unknown",
                    Count = g.Count(),
                    IsSelected = request.CategoryIds.Contains(g.Key.Categoryid ?? 0),
                    Subcategories = g.Where(p => p.Subcategoryid.HasValue)
                        .GroupBy(p => new { p.Subcategoryid, p.SubcategoryName })
                        .Select(sg => new CategoryFilter
                        {
                            Id = sg.Key.Subcategoryid ?? 0,
                            Name = sg.Key.SubcategoryName ?? "Unknown",
                            Count = sg.Count(),
                            IsSelected = request.SubcategoryIds.Contains(sg.Key.Subcategoryid ?? 0)
                        })
                        .OrderBy(sc => sc.Name)
                        .ToList()
                })
                .OrderBy(c => c.Name)
                .ToList();

            filters.Categories = categoryGroups;

            // Generate price ranges
            filters.PriceRanges = GeneratePriceRanges(products);

            // Generate brand filters
            filters.Brands = products
                .Where(p => !string.IsNullOrWhiteSpace(p.CompanyName))
                .GroupBy(p => p.CompanyName)
                .Select(g => new FilterOption
                {
                    Name = g.Key,
                    Value = g.Key,
                    Count = g.Count(),
                    IsSelected = request.Brands.Contains(g.Key)
                })
                .OrderBy(b => b.Name)
                .ToList();

            // Generate discount ranges
            filters.DiscountRanges = GenerateDiscountRanges(products, request);

            // Generate other filters
            // filters.CustomerReviews = GenerateCustomerReviewFilters(products);
            filters.Grocery = GenerateGroceryFilters(products);
            filters.PayOnDelivery = GeneratePayOnDeliveryFilters(products);
            filters.Occasions = ExtractAttributeFilters(products,
                new[] { "birthday", "thanksgiving", "anniversary", "valentine", "christmas" },
                request.Occasions);
            filters.Flavors = ExtractAttributeFilters(products,
                new[] { "chocolate", "dark chocolate", "almond", "peanut butter", "vanilla", "strawberry" },
                request.Flavors);
            filters.FoodTypes = ExtractAttributeFilters(products,
                new[] { "natural", "high in protein", "no added sugar", "dairy free", "gluten free", "organic" },
                request.FoodTypes);

            // Set active filters
            filters.ActiveFilters = BuildActiveFilters(request);

            return filters;
        }

        //    private List<FilterOption> GenerateCustomerReviewFilters(List<VwProduct> products)
        //    {
        //        return new List<FilterOption>
        //{
        //    new() { Name = "4★ & up", Value = "4", Count = products.Count(p => (p.Rating ?? 0) >= 4), IsSelected = false },
        //    new() { Name = "3★ & up", Value = "3", Count = products.Count(p => (p.Rating ?? 0) >= 3), IsSelected = false },
        //    new() { Name = "2★ & up", Value = "2", Count = products.Count(p => (p.Rating ?? 0) >= 2), IsSelected = false },
        //    new() { Name = "1★ & up", Value = "1", Count = products.Count(p => (p.Rating ?? 0) >= 1), IsSelected = false }
        //};
        //    }

        private List<FilterOption> GenerateGroceryFilters(List<VwProduct> products)
        {
            return new List<FilterOption>
    {
        new() { Name = "Made for Amazon", Value = "made_for_amazon", Count = 0, IsSelected = false },
        new() { Name = "Top Brands", Value = "top_brands", Count = products.Count(IsTopBrand), IsSelected = false }
    };
        }

        private List<FilterOption> GeneratePayOnDeliveryFilters(List<VwProduct> products)
        {
            return new List<FilterOption>
    {
        new() {
            Name = "Eligible for Pay On Delivery",
            Value = "cod",
           // Count = products.Count(p => p.CashOnDelivery == true),
            IsSelected = false
        }
    };
        }

        private List<FilterOption> ExtractAttributeFilters(List<VwProduct> products, string[] attributes, List<string> selectedAttributes)
        {
            var filters = new List<FilterOption>();

            foreach (var attribute in attributes)
            {
                var count = products.Count(p =>
                    (!string.IsNullOrEmpty(p.Keywords) && p.Keywords.Contains(attribute, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(p.ProductName) && p.ProductName.Contains(attribute, StringComparison.OrdinalIgnoreCase)));

                if (count > 0)
                {
                    filters.Add(new FilterOption
                    {
                        Name = attribute.ToTitleCase(),
                        Value = attribute,
                        Count = count,
                        IsSelected = selectedAttributes.Contains(attribute)
                    });
                }
            }

            return filters.OrderBy(f => f.Name).ToList();
        }

        //private bool IsTopBrand(VwProduct product)
        //{
        //    var topBrands = new[] { "Cadbury", "Amul", "KITKAT", "Nestlé", "HERSHEY'S", "Britannia", "Parle" };
        //    return topBrands.Any(brand =>
        //        product.CompanyName?.Contains(brand, StringComparison.OrdinalIgnoreCase) == true);
        //}

        private List<PriceRange> GeneratePriceRanges(List<VwProduct> products)
        {
            var validPrices = products
                .Where(p => decimal.TryParse(p.Price, out _))
                .Select(p => decimal.Parse(p.Price))
                .ToList();

            if (!validPrices.Any()) return new List<PriceRange>();

            var ranges = new List<PriceRange>
    {
        new() { Label = "Under ₹50", Min = 0, Max = 50 },
        new() { Label = "₹50 - ₹100", Min = 50, Max = 100 },
        new() { Label = "₹100 - ₹500", Min = 100, Max = 500 },
        new() { Label = "₹500 - ₹1,000", Min = 500, Max = 1000 },
        new() { Label = "₹1,000 - ₹5,000", Min = 1000, Max = 5000 },
        new() { Label = "₹5,000 - ₹10,000", Min = 5000, Max = 10000 },
        new() { Label = "Over ₹10,000", Min = 10000, Max = null }
    };

            foreach (var range in ranges)
            {
                range.Count = range.Max.HasValue
                    ? validPrices.Count(p => p >= range.Min && p < range.Max)
                    : validPrices.Count(p => p >= range.Min);
            }

            return ranges.Where(r => r.Count > 0).ToList();
        }

        private List<DiscountRange> GenerateDiscountRanges(List<VwProduct> products, FilterRequest request)
        {
            var ranges = new List<DiscountRange>
    {
        new() { Label = "10% Off or more", MinPercentage = 10, IsSelected = request.DiscountRanges.Contains("10") },
        new() { Label = "25% Off or more", MinPercentage = 25, IsSelected = request.DiscountRanges.Contains("25") },
        new() { Label = "50% Off or more", MinPercentage = 50, IsSelected = request.DiscountRanges.Contains("50") },
        new() { Label = "70% Off or more", MinPercentage = 70, IsSelected = request.DiscountRanges.Contains("70") }
    };

            foreach (var range in ranges)
            {
                range.Count = products.Count(p => CalculateDiscountPercentage(p) >= range.MinPercentage);
            }

            return ranges.Where(r => r.Count > 0).ToList();
        }

        private Dictionary<string, object> BuildActiveFilters(FilterRequest request)
        {
            var activeFilters = new Dictionary<string, object>();

            if (request.CategoryIds.Any())
                activeFilters["categories"] = request.CategoryIds;

            if (request.SubcategoryIds.Any())
                activeFilters["subcategories"] = request.SubcategoryIds;

            if (request.PriceMin.HasValue || request.PriceMax.HasValue)
                activeFilters["priceRange"] = new { min = request.PriceMin, max = request.PriceMax };

            if (request.Brands.Any())
                activeFilters["brands"] = request.Brands;

            if (request.DiscountRanges.Any())
                activeFilters["discountRanges"] = request.DiscountRanges;

            if (request.Occasions.Any())
                activeFilters["occasions"] = request.Occasions;

            if (request.Flavors.Any())
                activeFilters["flavors"] = request.Flavors;

            if (request.FoodTypes.Any())
                activeFilters["foodTypes"] = request.FoodTypes;

            if (request.PayOnDelivery.HasValue)
                activeFilters["payOnDelivery"] = request.PayOnDelivery;

            return activeFilters;
        }

        private List<SortOption> GetSortOptions(string currentSort)
        {
            var options = new List<SortOption>
    {
        new() { Key = "relevance", Label = "Best Match" },
        new() { Key = "price_asc", Label = "Price: Low to High" },
        new() { Key = "price_desc", Label = "Price: High to Low" },
        new() { Key = "discount_desc", Label = "Discount: High to Low" },
        new() { Key = "newest", Label = "Newest First" },
        new() { Key = "customer_rating", Label = "Customer Rating" },
        new() { Key = "name_asc", Label = "Name: A to Z" }
    };

            foreach (var option in options)
            {
                option.IsSelected = option.Key == currentSort;
            }

            return options;
        }


        // ====================  POPULAR PRODUCTS & SPECIAL CATEGORIES =========================

        // GET: api/products/popular?page=1&pageSize=10
        [AllowAnonymous]
        [HttpGet("popular")]
        public async Task<ActionResult<ProductsResponse>> GetPopularProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string category = "all") // all, bestseller, trending, featured, new-arrivals
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var baseQuery = _context.VwProducts
                .Where(p => p.IsDeleted == false && p.IsActive == true);

            // Apply category-specific logic
            IQueryable<VwProduct> query = category.ToLower() switch
            {
                "bestseller" => GetBestsellerQuery(baseQuery),
                "trending" => GetTrendingQuery(baseQuery),
                "featured" => GetFeaturedQuery(baseQuery),
                "new-arrivals" => GetNewArrivalsQuery(baseQuery),
                "deals" => await GetDealsQuery(baseQuery),
                "premium" => await GetPremiumQuery(baseQuery),
                _ => await GetGeneralPopularQuery(baseQuery)
            };

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var products = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = new ProductsResponse
            {
                Products = products,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };

            return Ok(response);
        }

        // GET: api/products/categories/special
        [AllowAnonymous]
        [HttpGet("categories/special")]
        public async Task<ActionResult<SpecialCategoriesResponse>> GetSpecialCategories()
        {
            var response = new SpecialCategoriesResponse();

            // Get counts for each special category
            var allActiveProducts = _context.VwProducts
                .Where(p => p.IsDeleted == false && p.IsActive == true);

            var dealsQuery = await GetDealsQuery(allActiveProducts);
            var premiumQuery = await GetPremiumQuery(allActiveProducts);

            response.Categories = new List<SpecialCategory>
    {
        new SpecialCategory
        {
            Key = "bestseller",
            Name = "Best Sellers",
            Description = "Most popular products based on sales",
            Count = GetBestsellerQuery(allActiveProducts).Count(),
            Icon = "🔥"
        },
        new SpecialCategory
        {
            Key = "trending",
            Name = "Trending Now",
            Description = "Products gaining popularity",
            Count = GetTrendingQuery(allActiveProducts).Count(),
            Icon = "📈"
        },
        new SpecialCategory
        {
            Key = "featured",
            Name = "Featured Products",
            Description = "Hand-picked premium selections",
            Count = GetFeaturedQuery(allActiveProducts).Count(),
            Icon = "⭐"
        },
        new SpecialCategory
        {
            Key = "new-arrivals",
            Name = "New Arrivals",
            Description = "Latest products added",
            Count = GetNewArrivalsQuery(allActiveProducts).Count(),
            Icon = "🆕"
        },
        new SpecialCategory
        {
            Key = "deals",
            Name = "Special Deals",
            Description = "Products with significant discounts",
            Count = dealsQuery.Count(),
            Icon = "💰"
        },
        new SpecialCategory
        {
            Key = "premium",
            Name = "Premium Selection",
            Description = "High-quality premium products",
            Count = premiumQuery.Count(),
            Icon = "👑"
        }
    };

            return Ok(response);
        }

        // GET: api/products/recommendations/{productId}
        [AllowAnonymous]
        [HttpGet("recommendations/{productId}")]
        public async Task<ActionResult<List<VwProduct>>> GetRecommendations(
            long productId,
            [FromQuery] int limit = 8)
        {
            var product = await _context.VwProducts
                .FirstOrDefaultAsync(p => p.Id == productId && p.IsDeleted == false && p.IsActive == true);

            if (product == null)
                return NotFound();

            var recommendations = await GetProductRecommendations(product, limit);
            return Ok(recommendations);
        }

        // GET: api/products/homepage-sections
        [AllowAnonymous]
        [HttpGet("homepage-sections")]
        public async Task<ActionResult<HomepageSectionsResponse>> GetHomepageSections()
        {
            var response = new HomepageSectionsResponse();

            // Get different sections with limited products for homepage
            var allActiveProducts = _context.VwProducts
                .Where(p => p.IsDeleted == false && p.IsActive == true);

            var dealsQuery = await GetDealsQuery(allActiveProducts);
            var featuredQuery = GetFeaturedQuery(allActiveProducts);

            response.Sections = new List<HomepageSection>
    {
        new HomepageSection
        {
            Title = "Best Sellers",
            Key = "bestseller",
            Products = GetBestsellerQuery(allActiveProducts).Take(8).ToList()
        },
        new HomepageSection
        {
            Title = "Trending Now",
            Key = "trending",
            Products = GetTrendingQuery(allActiveProducts).Take(8).ToList()
        },
        new HomepageSection
        {
            Title = "New Arrivals",
            Key = "new-arrivals",
            Products = GetNewArrivalsQuery(allActiveProducts).Take(8).ToList()
        },
        new HomepageSection
        {
            Title = "Special Deals",
            Key = "deals",
            Products = dealsQuery.Take(8).ToList()
        },
        new HomepageSection
        {
            Title = "Featured Products",
            Key = "featured",
            Products = featuredQuery.Take(8).ToList()
        }
    };

            return Ok(response);
        }

        // Private helper methods for different product categories

        private IQueryable<VwProduct> GetBestsellerQuery(IQueryable<VwProduct> query)
        {
            // Logic: Products with high sales, good ratings, frequent purchases
            return query
                .Where(p =>
                    !string.IsNullOrEmpty(p.ProductName) &&
                    (p.SpecialTags.Contains("popular") || p.SpecialTags.Contains("bestseller") ||
                     (!string.IsNullOrEmpty(p.Price) && !string.IsNullOrEmpty(p.Discountprice))))
                .OrderByDescending(p => p.AddedDate);
        }

        private IQueryable<VwProduct> GetTrendingQuery(IQueryable<VwProduct> query)
        {
            // Logic: Products gaining popularity recently
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);

            return query
                .Where(p =>
                    p.AddedDate >= thirtyDaysAgo ||
                    p.SpecialTags.Contains("trending") ||
                    p.SpecialTags.Contains("viral"))
                .OrderByDescending(p => p.AddedDate);
        }

        private IQueryable<VwProduct> GetFeaturedQuery(IQueryable<VwProduct> query)
        {
            // Logic: Hand-picked premium or featured products
            return query
                .Where(p =>
                    p.SpecialTags.Contains("featured") ||
                    p.SpecialTags.Contains("premium") ||
                    p.SpecialTags.Contains("editor"))
                .OrderByDescending(p => p.AddedDate);
        }

        private IQueryable<VwProduct> GetNewArrivalsQuery(IQueryable<VwProduct> query)
        {
            // Logic: Recently added products (last 30 days)
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);

            return query
                .Where(p => p.AddedDate >= thirtyDaysAgo)
                .OrderByDescending(p => p.AddedDate);
        }

        private async Task<IQueryable<VwProduct>> GetDealsQuery(IQueryable<VwProduct> query)
        {
            // Logic: Products with significant discounts (>= 15%)
            var productsWithPrices = await query
                .Where(p =>
                    !string.IsNullOrEmpty(p.Price) &&
                    !string.IsNullOrEmpty(p.Discountprice))
                .ToListAsync();

            var dealsProducts = productsWithPrices
                .Where(p =>
                    decimal.TryParse(p.Price, out var price) &&
                    decimal.TryParse(p.Discountprice, out var discountPrice) &&
                    price > discountPrice &&
                    ((price - discountPrice) / price) * 100 >= 15)
                .OrderByDescending(p => CalculateDiscountPercentage(p))
                .ThenByDescending(p => p.AddedDate);

            return dealsProducts.AsQueryable();
        }

        private async Task<IQueryable<VwProduct>> GetPremiumQuery(IQueryable<VwProduct> query)
        {
            // Logic: High-quality, premium products
            var productsWithPrices = await query
                .Where(p => !string.IsNullOrEmpty(p.Price))
                .ToListAsync();

            var premiumProducts = productsWithPrices
                .Where(p =>
                    IsTopBrand(p) ||
                    (decimal.TryParse(p.Price, out var price) && price >= 500) ||
                    (!string.IsNullOrEmpty(p.SpecialTags) && (
                        p.SpecialTags.Contains("premium", StringComparison.OrdinalIgnoreCase) ||
                        p.SpecialTags.Contains("luxury", StringComparison.OrdinalIgnoreCase) ||
                        p.SpecialTags.Contains("organic", StringComparison.OrdinalIgnoreCase)
                    )))
                .OrderByDescending(p => decimal.TryParse(p.Price, out var price) ? price : 0)
                .ThenByDescending(p => p.AddedDate);

            return premiumProducts.AsQueryable();
        }

        private async Task<IQueryable<VwProduct>> GetGeneralPopularQuery(IQueryable<VwProduct> query)
        {
            // Logic: General popularity based on various factors
            var allProducts = await query.ToListAsync();

            var popularProducts = allProducts
                .Where(p =>
                    IsTopBrand(p) ||
                    CalculateDiscountPercentage(p) > 10 ||
                    (p.SpecialTags?.Contains("popular") == true) ||
                    (p.SpecialTags?.Contains("bestseller") == true))
                .OrderByDescending(p => IsTopBrand(p) ? 1 : 0)
                .ThenByDescending(p => CalculateDiscountPercentage(p))
                .ThenByDescending(p => p.AddedDate);

            return popularProducts.AsQueryable();
        }

        private async Task<List<VwProduct>> GetProductRecommendations(VwProduct product, int limit)
        {
            var recommendations = new List<VwProduct>();

            // 1. Products from same category/subcategory
            var sameCategory = await _context.VwProducts
                .Where(p => p.IsDeleted == false && p.IsActive == true &&
                           p.Id != product.Id &&
                           (p.Categoryid == product.Categoryid ||
                            p.Subcategoryid == product.Subcategoryid))
                .OrderByDescending(p => p.AddedDate)
                .Take(limit / 2)
                .ToListAsync();

            recommendations.AddRange(sameCategory);

            // 2. Products with similar SpecialTags
            if (!string.IsNullOrEmpty(product.SpecialTags))
            {
                var specialTags = product.SpecialTags.Split(',').Select(k => k.Trim()).ToList();
                var similarProducts = await _context.VwProducts
                    .Where(p => p.IsDeleted == false && p.IsActive == true &&
                               p.Id != product.Id &&
                               !recommendations.Select(r => r.Id).Contains(p.Id) &&
                               specialTags.Any(k => p.SpecialTags.Contains(k)))
                    .OrderByDescending(p => p.AddedDate)
                    .Take(limit - recommendations.Count)
                    .ToListAsync();

                recommendations.AddRange(similarProducts);
            }

            // 3. Fill remaining slots with popular products
            if (recommendations.Count < limit)
            {
                var popular = await _context.VwProducts
                    .Where(p => p.IsDeleted == false && p.IsActive == true &&
                               p.Id != product.Id &&
                               !recommendations.Select(r => r.Id).Contains(p.Id))
                    .OrderByDescending(p => p.AddedDate)
                    .Take(limit - recommendations.Count)
                    .ToListAsync();

                recommendations.AddRange(popular);
            }

            return recommendations.Take(limit).ToList();
        }

        // Helper methods that can be used with client evaluation
        private decimal CalculateDiscountPercentage(VwProduct product)
        {
            if (string.IsNullOrEmpty(product.Price) || string.IsNullOrEmpty(product.Discountprice))
                return 0;

            if (decimal.TryParse(product.Price, out var price) &&
                decimal.TryParse(product.Discountprice, out var discountPrice) &&
                price > discountPrice)
            {
                return ((price - discountPrice) / price) * 100;
            }

            return 0;
        }

        private bool IsTopBrand(VwProduct product)
        {
            if (string.IsNullOrEmpty(product.CompanyName) && string.IsNullOrEmpty(product.VendorName))
                return false;

            var topBrands = new[] { "Samsung", "Apple", "Nike", "Adidas", "Sony", "LG", "Canon", "Dell", "HP", "Lenovo" };

            return topBrands.Any(brand =>
                (!string.IsNullOrEmpty(product.CompanyName) &&
                 product.CompanyName.Contains(brand, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(product.VendorName) &&
                 product.VendorName.Contains(brand, StringComparison.OrdinalIgnoreCase)));
        }

        // Additional helper method for product ranking
        private int CalculatePopularityScore(VwProduct product)
        {
            int score = 0;

            // Brand score
            if (IsTopBrand(product)) score += 100;

            // Discount score
            var discountPercentage = CalculateDiscountPercentage(product);
            score += (int)(discountPercentage * 2);

            // Recency score
            var daysSinceAdded = (DateTime.Now - product.AddedDate).Days;
            if (daysSinceAdded <= 7) score += 50;
            else if (daysSinceAdded <= 30) score += 25;

            // Keyword score
            if (!string.IsNullOrEmpty(product.SpecialTags))
            {
                var popularSpecialTags = new[] { "popular", "bestseller", "trending", "featured", "premium" };
                score += popularSpecialTags.Count(k => product.SpecialTags.Contains(k, StringComparison.OrdinalIgnoreCase)) * 20;
            }

            return score;
        }
    }
        // Response models
        public class SpecialCategoriesResponse
        {
            public List<SpecialCategory> Categories { get; set; } = new();
        }

        public class SpecialCategory
        {
            public string Key { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int Count { get; set; }
            public string Icon { get; set; } = string.Empty;
        }

        public class HomepageSectionsResponse
        {
            public List<HomepageSection> Sections { get; set; } = new();
        }

        public class HomepageSection
        {
            public string Title { get; set; } = string.Empty;
            public string Key { get; set; } = string.Empty;
            public List<VwProduct> Products { get; set; } = new();
        }

    public class PaginationResult
    {
        public List<VwProduct> Products { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
    public static class StringExtensions
    {
        public static string ToTitleCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
            return string.Join(" ", words);
        }
    }

    // Model classes remain the same...
    public class ProductsResponse
        {
            public List<VwProduct> Products { get; set; } = new();
            public int CurrentPage { get; set; }
            public int PageSize { get; set; }
            public int TotalCount { get; set; }
            public int TotalPages { get; set; }
            public bool HasNextPage { get; set; }
            public bool HasPreviousPage { get; set; }
            public string SearchQuery { get; set; } = string.Empty;
            public bool ShowAll { get; set; }
        }

        public class SearchResponse
        {
            public List<VwProduct> Products { get; set; } = new();
            public List<SearchSuggestion> Suggestions { get; set; } = new();
            public int CurrentPage { get; set; }
            public int PageSize { get; set; }
            public int TotalCount { get; set; }
            public int TotalPages { get; set; }
            public bool HasNextPage { get; set; }
            public bool HasPreviousPage { get; set; }
            public string SearchQuery { get; set; } = string.Empty;
            public bool ShowAll { get; set; }
        }

        public class SearchSuggestion
        {
            public string Text { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty; // "product", "keyword", "category"
            public int MatchCount { get; set; }
            public string Category { get; set; } = string.Empty;
            public int Priority { get; set; }
        }

        public class FilterRequest
        {
            public string Query { get; set; } = string.Empty;
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 10;
            public bool ShowAll { get; set; } = false;
            public bool IncludeSuggestions { get; set; } = true;

            // Price Filters
            public decimal? PriceMin { get; set; }
            public decimal? PriceMax { get; set; }
            public decimal? DiscountPriceMin { get; set; }
            public decimal? DiscountPriceMax { get; set; }

            // Category Filters
            public List<int> CategoryIds { get; set; } = new();
            public List<int> SubcategoryIds { get; set; } = new();
            public List<int> ChildCategoryIds { get; set; } = new();

            // Brand/Company Filters
            public List<string> Brands { get; set; } = new();
            public List<string> CompanyNames { get; set; } = new();

            // Discount Filters
            public List<string> DiscountRanges { get; set; } = new(); // "10", "25", "50"
            public decimal? MinDiscountPercentage { get; set; }

            // Rating Filters
            public decimal? MinRating { get; set; }

            // Special Filters
            public bool? PayOnDelivery { get; set; }
            public bool? MadeForAmazon { get; set; }
            public bool? TopBrands { get; set; }

            // Product Attributes
            public List<string> Occasions { get; set; } = new();
            public List<string> Flavors { get; set; } = new();
            public List<string> FoodTypes { get; set; } = new();
            public List<string> WeightTypes { get; set; } = new();

            // Weight Filters
            public decimal? WeightMin { get; set; }
            public decimal? WeightMax { get; set; }

            // Sorting
            public string SortBy { get; set; } = "relevance";
        }

        public class FilterOption
        {
            public string Name { get; set; } = string.Empty;
            public object Value { get; set; } = string.Empty;
            public int Count { get; set; }
            public bool IsSelected { get; set; }
        }

        public class PriceRange
        {
            public string Label { get; set; } = string.Empty;
            public decimal? Min { get; set; }
            public decimal? Max { get; set; }
            public int Count { get; set; }
            public bool IsSelected { get; set; }
        }

        public class DiscountRange
        {
            public string Label { get; set; } = string.Empty;
            public decimal MinPercentage { get; set; }
            public int Count { get; set; }
            public bool IsSelected { get; set; }
        }

        public class CategoryFilter
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int? Count { get; set; }
            public bool IsSelected { get; set; }
            public List<CategoryFilter> Subcategories { get; set; } = new();
        }

        public class AvailableFilters
        {
            public List<CategoryFilter> Categories { get; set; } = new();
            public List<PriceRange> PriceRanges { get; set; } = new();
            public List<FilterOption> Brands { get; set; } = new();
            public List<DiscountRange> DiscountRanges { get; set; } = new();
            public List<FilterOption> CustomerReviews { get; set; } = new();
            public List<FilterOption> Grocery { get; set; } = new();
            public List<FilterOption> PayOnDelivery { get; set; } = new();
        public List<FilterOption> Occasions { get; set; } = new();
        public List<FilterOption> Flavors { get; set; } = new();
        public List<FilterOption> FoodTypes { get; set; } = new();
        public Dictionary<string, object> ActiveFilters { get; set; } = new();
    }

    public class EnhancedSearchResponse : SearchResponse
    {
        public AvailableFilters Filters { get; set; } = new();
        public List<SortOption> SortOptions { get; set; } = new();
        public string CurrentSort { get; set; } = "relevance";
    }

    public class SortOption
    {
        public string? Key { get; set; }
        public string? Label { get; set; }
        public bool IsSelected { get; set; }
    }
}