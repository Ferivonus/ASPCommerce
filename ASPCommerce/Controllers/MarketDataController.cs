using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using ASPCommerce.Models;
using System.Data.Common;

namespace ASPCommerce.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MarketDataController : ControllerBase
    {
        private readonly ILogger<MarketDataController>? _logger;
        private readonly string? _connectionString;

        public MarketDataController(ILogger<MarketDataController>? logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("ConnectionString");
        }

        [HttpGet(Name = "Get-All-Product-Data")]
        public async Task<IActionResult> GetAllMarketData()
        {
            try
            {
                var marketDataList = await RetrieveMarketDataAsync(null);
                return Ok(marketDataList);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error retrieving all market data: {ex.Message}");
                return StatusCode(500); // Internal Server Error
            }
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                string query = "SELECT DISTINCT Category FROM MarketItem";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                List<string> categories = new List<string>();
                while (await reader.ReadAsync())
                {
                    string category = reader.GetString("Category");
                    categories.Add(category);
                }

                return Ok(categories);
            }
            catch (Exception ex)
            {
                // Log the exception
                // Return internal server error
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        [HttpGet("ByCategory", Name = "GetMarketDataByCategory")]
        public async Task<IActionResult> GetMarketDataByCategory([FromQuery(Name = "category")] string category)
        {
            try
            {
                var marketDataList = await RetrieveMarketDataAsync(category);
                return Ok(marketDataList);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error retrieving market data by category: {ex.Message}");
                return StatusCode(500); // Internal Server Error
            }
        }

        private async Task<IEnumerable<ProductModel>> RetrieveMarketDataAsync(string? category)
        {
            var marketDataList = new List<ProductModel>();

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                _logger?.LogError("Database connection string is not configured.");
                return marketDataList;
            }

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = "SELECT * FROM MarketItem";
            if (!string.IsNullOrEmpty(category))
            {
                query += " WHERE Category = @Category";
            }

            using var cmd = new MySqlCommand(query, conn);
            if (!string.IsNullOrEmpty(category))
            {
                cmd.Parameters.AddWithValue("@Category", category);
            }

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var marketData = new ProductModel
                {
                    Id = reader.GetInt32("Id"),
                    ProductName = reader.GetString("ProductName"),
                    Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                    Price = reader.GetDecimal("Price"),
                    StockQuantity = reader.GetInt32("StockQuantity"),
                    Category = reader.IsDBNull("Category") ? null : reader.GetString("Category"),
                    Brand = reader.IsDBNull("Brand") ? null : reader.GetString("Brand"),
                    ImageUrl = reader.IsDBNull("ImageUrl") ? null : reader.GetString("ImageUrl"),
                    DateAdded = reader.GetDateTime("DateAdded")
                };
                marketDataList.Add(marketData);
            }

            return marketDataList;
        }

        // Orders API Controller
        [HttpPost("add-order")]
        public async Task<IActionResult> AddOrder([FromBody] AddOrderModel addOrderModel)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Insert new order into Orders table
                    string insertOrderQuery = @"INSERT INTO Orders (UserId, OrderDate, TotalAmount) 
                                        VALUES (@UserId, @OrderDate, @TotalAmount)";
                    using (MySqlCommand insertOrderCommand = new MySqlCommand(insertOrderQuery, connection))
                    {
                        insertOrderCommand.Parameters.AddWithValue("@UserId", addOrderModel.UserId);
                        insertOrderCommand.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                        insertOrderCommand.Parameters.AddWithValue("@TotalAmount", addOrderModel.TotalAmount);
                        await insertOrderCommand.ExecuteNonQueryAsync();
                    }

                    // Get the generated OrderId
                    int orderId;
                    string orderIdQuery = "SELECT LAST_INSERT_ID();";
                    using (MySqlCommand orderIdCommand = new MySqlCommand(orderIdQuery, connection))
                    {
                        orderId = Convert.ToInt32(await orderIdCommand.ExecuteScalarAsync());
                    }

                    // Insert order details into OrderDetails table
                    foreach (var orderDetail in addOrderModel.OrderDetails)
                    {
                        string insertOrderDetailQuery = @"INSERT INTO OrderDetails (OrderId, ProductId, Quantity, UnitPrice) 
                                                  VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)";
                        using (MySqlCommand insertOrderDetailCommand = new MySqlCommand(insertOrderDetailQuery, connection))
                        {
                            insertOrderDetailCommand.Parameters.AddWithValue("@OrderId", orderId);
                            insertOrderDetailCommand.Parameters.AddWithValue("@ProductId", orderDetail.ProductId);
                            insertOrderDetailCommand.Parameters.AddWithValue("@Quantity", orderDetail.Quantity);
                            insertOrderDetailCommand.Parameters.AddWithValue("@UnitPrice", orderDetail.UnitPrice);
                            await insertOrderDetailCommand.ExecuteNonQueryAsync();
                        }
                    }

                    return Ok("Order added successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while adding order: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        [HttpGet("get-order/{orderId}")]
        public async Task<IActionResult> GetOrder(int orderId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT * FROM Orders WHERE OrderId = @OrderId";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@OrderId", orderId);

                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                var order = new
                                {
                                    OrderId = reader.GetInt32("OrderId"),
                                    UserId = reader.GetInt32("UserId"),
                                    OrderDate = reader.GetDateTime("OrderDate"),
                                    TotalAmount = reader.GetDecimal("TotalAmount")
                                };
                                return Ok(order);
                            }
                            else
                            {
                                return NotFound("Order not found");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving order: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        [HttpGet("get-orders-by-user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(int userId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT * FROM Orders WHERE UserId = @UserId";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);

                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            List<object> orders = new List<object>();
                            while (reader.Read())
                            {
                                var order = new
                                {
                                    OrderId = reader.GetInt32("OrderId"),
                                    UserId = reader.GetInt32("UserId"),
                                    OrderDate = reader.GetDateTime("OrderDate"),
                                    TotalAmount = reader.GetDecimal("TotalAmount")
                                };
                                orders.Add(order);
                            }
                            return Ok(orders);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving orders by user: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        // OrderDetails API Controller
        [HttpGet("get-order-details/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT * FROM OrderDetails WHERE OrderId = @OrderId";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@OrderId", orderId);

                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            List<object> orderDetails = new List<object>();
                            while (reader.Read())
                            {
                                var orderDetail = new
                                {
                                    OrderDetailId = reader.GetInt32("OrderDetailId"),
                                    OrderId = reader.GetInt32("OrderId"),
                                    ProductId = reader.GetInt32("ProductId"),
                                    Quantity = reader.GetInt32("Quantity"),
                                    UnitPrice = reader.GetDecimal("UnitPrice")
                                };
                                orderDetails.Add(orderDetail);
                            }
                            return Ok(orderDetails);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving order details: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        [HttpGet("get-order-details-by-product/{productId}")]
        public async Task<IActionResult> GetOrderDetailsByProduct(int productId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT * FROM OrderDetails WHERE ProductId = @ProductId";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ProductId", productId);

                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            List<object> orderDetails = new List<object>();
                            while (reader.Read())
                            {
                                var orderDetail = new
                                {
                                    OrderDetailId = reader.GetInt32("OrderDetailId"),
                                    OrderId = reader.GetInt32("OrderId"),
                                    ProductId = reader.GetInt32("ProductId"),
                                    Quantity = reader.GetInt32("Quantity"),
                                    UnitPrice = reader.GetDecimal("UnitPrice")
                                };
                                orderDetails.Add(orderDetail);
                            }
                            return Ok(orderDetails);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving order details by product: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }


        [HttpPost("add-comment")]
        public async Task<IActionResult> AddComment([FromBody] CommentModel comment)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"INSERT INTO ProductComments (ProductId, UserId, Comment) 
                             VALUES (@ProductId, @UserId, @Comment)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ProductId", comment.ProductId);
                        command.Parameters.AddWithValue("@UserId", comment.UserId);
                        command.Parameters.AddWithValue("@Comment", comment.Comment);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return Ok("Comment added successfully");
                        }
                        else
                        {
                            return StatusCode(500, "Failed to add comment");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while adding comment: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        [HttpGet("get-comments-by-product/{productId}")]
        public async Task<IActionResult> GetCommentsByProduct(int productId)
        {
            try
            {
                List<CommentModel> comments = new List<CommentModel>();

                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"SELECT * FROM ProductComments WHERE ProductId = @ProductId";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ProductId", productId);

                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                CommentModel comment = new CommentModel
                                {
                                    CommentId = reader.GetInt32(reader.GetOrdinal("CommentId")),
                                    ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                                    Comment = reader.GetString(reader.GetOrdinal("Comment")),
                                    CommentDate = reader.GetDateTime(reader.GetOrdinal("CommentDate"))
                                };
                                comments.Add(comment);
                            }
                        }
                    }
                }

                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving comments: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }



    }

}

    
