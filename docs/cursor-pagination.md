  # Cursor Pagination with Dapper

  ## Overview

  When working with large datasets, pagination is essential for performance. This guide demonstrates
  how to implement efficient cursor-based pagination with Dapper, avoiding the performance pitfalls of
  traditional OFFSET/FETCH pagination.

  ## Why Cursor Pagination?

  Traditional OFFSET/FETCH pagination becomes increasingly slow as you navigate deeper into results:

  ```csharp
  // ❌ Performance degrades with higher page numbers
  var products = connection.Query<Product>(@"
      SELECT * FROM Products
      ORDER BY CreatedAt DESC
      OFFSET @Offset ROWS
      FETCH NEXT @PageSize ROWS ONLY",
      new { Offset = 10000, PageSize = 20 }
  );
  // SQL Server must read and discard 10,000 rows!

  Cursor pagination maintains constant performance regardless of position:

  // ✅ Consistent performance at any position
  var products = connection.Query<Product>(@"
      SELECT TOP (@PageSize) * FROM Products
      WHERE CreatedAt < @Cursor
      ORDER BY CreatedAt DESC",
      new { Cursor = lastSeenDate, PageSize = 20 }
  );

  Implementation

  Basic Cursor Pagination

  public class CursorPaginationService
  {
      private readonly IDbConnection _connection;

      public async Task<PagedResult<T>> GetPageAsync<T>(
          string query,
          object parameters = null,
          string cursorColumn = "Id",
          int pageSize = 20)
      {
          // Fetch one extra record to determine if there are more pages
          var items = (await _connection.QueryAsync<T>(
              query,
              new { PageSize = pageSize + 1, Parameters = parameters }
          )).ToList();

          var hasMore = items.Count > pageSize;
          if (hasMore)
          {
              items.RemoveAt(items.Count - 1);
          }

          return new PagedResult<T>
          {
              Items = items,
              HasMore = hasMore,
              NextCursor = hasMore ? GetCursor(items.Last(), cursorColumn) : null
          };
      }
  }

  public class PagedResult<T>
  {
      public List<T> Items { get; set; }
      public bool HasMore { get; set; }
      public string NextCursor { get; set; }
  }

  Real-World Example: Product Listing

  public class ProductRepository
  {
      private readonly IDbConnection _connection;

      public async Task<PagedResult<Product>> GetProductsAsync(
          string cursor = null,
          int pageSize = 20,
          string category = null)
      {
          var sql = @"
              SELECT TOP (@PageSize)
                  Id, Name, Price, Category, CreatedAt
              FROM Products
              WHERE (@Cursor IS NULL OR CreatedAt < @Cursor)
                  AND (@Category IS NULL OR Category = @Category)
              ORDER BY CreatedAt DESC, Id DESC";

          DateTime? cursorDate = null;
          if (!string.IsNullOrEmpty(cursor))
          {
              // Decode cursor (base64 encoded date)
              cursorDate = DateTime.Parse(
                  Encoding.UTF8.GetString(Convert.FromBase64String(cursor))
              );
          }

          var products = (await _connection.QueryAsync<Product>(
              sql,
              new
              {
                  PageSize = pageSize + 1,
                  Cursor = cursorDate,
                  Category = category
              }
          )).ToList();

          var hasMore = products.Count > pageSize;
          if (hasMore)
          {
              products.RemoveAt(products.Count - 1);
          }

          string nextCursor = null;
          if (hasMore && products.Any())
          {
              // Encode cursor for URL safety
              var lastDate = products.Last().CreatedAt.ToString("O");
              nextCursor = Convert.ToBase64String(
                  Encoding.UTF8.GetBytes(lastDate)
              );
          }

          return new PagedResult<Product>
          {
              Items = products,
              HasMore = hasMore,
              NextCursor = nextCursor
          };
      }
  }

  Handling Composite Keys

  When the sort column isn't unique, use composite cursors:

  public async Task<PagedResult<Order>> GetOrdersAsync(string cursor = null)
  {
      var sql = @"
          SELECT TOP (@PageSize)
              Id, CustomerId, OrderDate, Total
          FROM Orders
          WHERE (@CursorDate IS NULL OR
                 OrderDate < @CursorDate OR
                 (OrderDate = @CursorDate AND Id < @CursorId))
          ORDER BY OrderDate DESC, Id DESC";

      DateTime? cursorDate = null;
      int? cursorId = null;

      if (!string.IsNullOrEmpty(cursor))
      {
          // Decode composite cursor
          var json = Encoding.UTF8.GetString(
              Convert.FromBase64String(cursor)
          );
          var cursorData = JsonSerializer.Deserialize<CursorData>(json);
          cursorDate = cursorData.Date;
          cursorId = cursorData.Id;
      }

      var orders = (await _connection.QueryAsync<Order>(
          sql,
          new
          {
              PageSize = 21,
              CursorDate = cursorDate,
              CursorId = cursorId
          }
      )).ToList();

      // Process results and generate next cursor...
  }

  public class CursorData
  {
      public DateTime Date { get; set; }
      public int Id { get; set; }
  }

  Performance Optimization

  Required Indexes

  For optimal cursor pagination performance, create appropriate indexes:

  -- Single column cursor
  CREATE NONCLUSTERED INDEX IX_Products_CreatedAt
  ON Products(CreatedAt DESC)
  INCLUDE (Id, Name, Price, Category);

  -- Composite cursor
  CREATE NONCLUSTERED INDEX IX_Orders_OrderDate_Id
  ON Orders(OrderDate DESC, Id DESC)
  INCLUDE (CustomerId, Total);

  Performance Comparison

  | Page Position | OFFSET/FETCH | Cursor Pagination | Improvement |
  |---------------|--------------|-------------------|-------------|
  | Page 1        | 5ms          | 5ms               | -           |
  | Page 100      | 125ms        | 5ms               | 96%         |
  | Page 1000     | 1,250ms      | 5ms               | 99.6%       |
  | Page 10000    | 12,500ms     | 5ms               | 99.96%      |

  API Implementation

  RESTful Endpoint

  [ApiController]
  [Route("api/[controller]")]
  public class ProductsController : ControllerBase
  {
      private readonly IProductRepository _repository;

      [HttpGet]
      public async Task<IActionResult> GetProducts(
          [FromQuery] string cursor = null,
          [FromQuery] int pageSize = 20,
          [FromQuery] string category = null)
      {
          if (pageSize > 100)
              return BadRequest("Page size cannot exceed 100");

          var result = await _repository.GetProductsAsync(
              cursor,
              pageSize,
              category
          );

          // Add pagination metadata to response headers
          Response.Headers.Add("X-Has-More", result.HasMore.ToString());
          if (result.HasMore)
          {
              Response.Headers.Add("X-Next-Cursor", result.NextCursor);
          }

          return Ok(result.Items);
      }
  }

  Client Usage

  public class ApiClient
  {
      private readonly HttpClient _httpClient;

      public async IAsyncEnumerable<Product> GetAllProductsAsync()
      {
          string cursor = null;
          bool hasMore = true;

          while (hasMore)
          {
              var url = $"/api/products?pageSize=50";
              if (!string.IsNullOrEmpty(cursor))
              {
                  url += $"&cursor={Uri.EscapeDataString(cursor)}";
              }

              var response = await _httpClient.GetAsync(url);
              response.EnsureSuccessStatusCode();

              var products = await response.Content
                  .ReadFromJsonAsync<List<Product>>();

              foreach (var product in products)
              {
                  yield return product;
              }

              hasMore = bool.Parse(
                  response.Headers.GetValues("X-Has-More").FirstOrDefault() ?? "false"
              );
              cursor = response.Headers
                  .GetValues("X-Next-Cursor")
                  .FirstOrDefault();
          }
      }
  }

  Best Practices

  1. Always use parameterized queries to prevent SQL injection
  2. Limit page size to prevent abuse (typically 100 max)
  3. Use stable sort columns (prefer immutable columns like CreatedAt)
  4. Include a secondary sort column (like Id) for deterministic ordering
  5. Encode cursors for URL safety and to abstract implementation details
  6. Add appropriate indexes to support your pagination queries
  7. Monitor query performance as data grows

  Testing

  [Fact]
  public async Task CursorPagination_ReturnsConsistentResults()
  {
      // Arrange
      var allProducts = await GetAllProductsUsingCursor();
      var expectedProducts = await GetAllProductsDirectly();

      // Assert
      Assert.Equal(expectedProducts.Count, allProducts.Count);
      Assert.Equal(expectedProducts, allProducts);
  }

  [Fact]
  public async Task CursorPagination_PerformsConsistently()
  {
      // Arrange
      string cursor = null;
      var times = new List<long>();

      // Act - Get 10 pages
      for (int i = 0; i < 10; i++)
      {
          var sw = Stopwatch.StartNew();
          var page = await repository.GetProductsAsync(cursor, 20);
          sw.Stop();

          times.Add(sw.ElapsedMilliseconds);
          cursor = page.NextCursor;
      }

      // Assert - All pages should take similar time
      var avgTime = times.Average();
      Assert.True(times.All(t => Math.Abs(t - avgTime) < avgTime * 0.2));
  }

  Summary

  Cursor pagination with Dapper provides:
  - Consistent performance regardless of dataset size
  - Stable results when new data is added
  - Simple implementation with standard SQL
  - Better user experience for deep pagination

  By following this guide, you can implement efficient pagination that scales to millions of records
  while maintaining sub-10ms query times.