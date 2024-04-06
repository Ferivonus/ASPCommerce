using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ASPCommerce.Models;

namespace ASPCommerce.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class adminController : ControllerBase
    {
        private readonly ILogger<adminController> _logger;
        private readonly string _connectionString;

        public adminController(ILogger<adminController> logger, string connectionString)
        {
            _logger = logger;
            _connectionString = connectionString;
        }

        [HttpPost("add-product")]
        public async Task<IActionResult> AddProduct([FromBody] ProductModel product)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (MySqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Check if the category already exists
                            int CategoryName;
                            string CategoryNameQuery = "SELECT CategoryName FROM Categories WHERE Name = @Name;";
                            using (MySqlCommand CategoryNameCommand = new MySqlCommand(CategoryNameQuery, connection))
                            {
                                CategoryNameCommand.Parameters.AddWithValue("@Name", product.CategoryName);
                                object CategoryNameObj = await CategoryNameCommand.ExecuteScalarAsync();
                                if (CategoryNameObj != null && int.TryParse(CategoryNameObj.ToString(), out CategoryName))
                                {
                                    // Category already exists, use its ID
                                }
                                else
                                {
                                    // Category doesn't exist, insert it
                                    string insertCategoryQuery = @"INSERT INTO Categories (Name, Description) 
                                                 VALUES (@Name, @Description);";
                                    using (MySqlCommand insertCategoryCommand = new MySqlCommand(insertCategoryQuery, connection))
                                    {
                                        insertCategoryCommand.Parameters.AddWithValue("@Name", product.CategoryName);
                                        insertCategoryCommand.Parameters.AddWithValue("@Description", product.CategoryDescription);
                                        await insertCategoryCommand.ExecuteNonQueryAsync();

                                        // Get the generated CategoryName
                                        CategoryName = (int)insertCategoryCommand.LastInsertedId;
                                    }
                                }
                            }

                            // Insert into Products table
                            string productQuery = @"INSERT INTO Products (Name, Description, Price, StockQuantity, CategoryName) 
                                    VALUES (@Name, @Description, @Price, @StockQuantity, @CategoryName);";
                            using (MySqlCommand productCommand = new MySqlCommand(productQuery, connection))
                            {
                                productCommand.Parameters.AddWithValue("@Name", product.ProductName);
                                productCommand.Parameters.AddWithValue("@Description", product.Description);
                                productCommand.Parameters.AddWithValue("@Price", product.Price);
                                productCommand.Parameters.AddWithValue("@StockQuantity", product.StockQuantity);
                                productCommand.Parameters.AddWithValue("@CategoryName", product.CategoryName);

                                int rowsAffected = await productCommand.ExecuteNonQueryAsync();
                                if (rowsAffected > 0)
                                {
                                    transaction.Commit();
                                    return Ok("Product added successfully");
                                }
                                else
                                {
                                    transaction.Rollback();
                                    return StatusCode(500, "Failed to add product");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError($"An error occurred while adding product: {ex.Message}");
                            return StatusCode(500, "An error occurred while processing the request");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while establishing database connection: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }






        [HttpPut("update-product-by-id/{id}")]
        public async Task<IActionResult> UpdateProductById(int id, [FromBody] UpdateProductModel Update_product)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"UPDATE Products 
                             SET Name = @Name, Description = @Description, 
                                 Price = @Price, StockQuantity = @StockQuantity, 
                                 CategoryName = @CategoryName
                             WHERE ProductId = @Id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", Update_product.ProductName);
                        command.Parameters.AddWithValue("@Description", Update_product.Description);
                        command.Parameters.AddWithValue("@Price", Update_product.Price);
                        command.Parameters.AddWithValue("@StockQuantity", Update_product.StockQuantity);
                        command.Parameters.AddWithValue("@CategoryName", Update_product.CategoryName);
                        command.Parameters.AddWithValue("@Id", id);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok("Product updated successfully");
                        }
                        else
                        {
                            return NotFound("Product not found");
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError($"An error occurred while updating product: {ex.Message}");
                return StatusCode(500, "Failed to update product due to a database error");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred while updating product: {ex.Message}");
                return StatusCode(500, "An unexpected error occurred while processing the request");
            }
        }

        [HttpPut("update-categories-by-id/{id}")]
        public async Task<IActionResult> UpdateCategoriesById(int id, [FromBody] CategoryModel category)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"UPDATE Categories 
                             SET Name = @Name, Description = @Description
                             WHERE CategoryName = @Id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", category.Name);
                        command.Parameters.AddWithValue("@Description", category.Description);
                        command.Parameters.AddWithValue("@Id", id);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok("Category updated successfully");
                        }
                        else
                        {
                            return NotFound("Category not found");
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError($"An error occurred while updating category: {ex.Message}");
                return StatusCode(500, "Failed to update category due to a database error");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred while updating category: {ex.Message}");
                return StatusCode(500, "An unexpected error occurred while processing the request");
            }
        }

        [HttpPut("update-category-name/{id}")]
        public async Task<IActionResult> UpdateCategoryNameById(int id, [FromBody] string newName)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"UPDATE Categories 
                             SET Name = @NewName
                             WHERE CategoryId = @Id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@NewName", newName);
                        command.Parameters.AddWithValue("@Id", id);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok("Category name updated successfully");
                        }
                        else
                        {
                            return NotFound("Category not found");
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError($"An error occurred while updating category name: {ex.Message}");
                return StatusCode(500, "Failed to update category name due to a database error");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred while updating category name: {ex.Message}");
                return StatusCode(500, "An unexpected error occurred while processing the request");
            }
        }

        [HttpPut("update-category-description/{id}")]
        public async Task<IActionResult> UpdateCategoryDescriptionById(int id, [FromBody] string newDescription)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"UPDATE Categories 
                             SET Description = @NewDescription
                             WHERE CategoryId = @Id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@NewDescription", newDescription);
                        command.Parameters.AddWithValue("@Id", id);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok("Category description updated successfully");
                        }
                        else
                        {
                            return NotFound("Category not found");
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError($"An error occurred while updating category description: {ex.Message}");
                return StatusCode(500, "Failed to update category description due to a database error");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred while updating category description: {ex.Message}");
                return StatusCode(500, "An unexpected error occurred while processing the request");
            }
        }




        [HttpPut("update-product-price")]
        public async Task<IActionResult> UpdateProductPrice([FromBody] ProductPriceUpdateModel model)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Check if the old price matches the current price in the database
                    string checkOldPriceQuery = @"SELECT Price FROM Products WHERE ProductId = @Id";
                    using (MySqlCommand checkOldPriceCommand = new MySqlCommand(checkOldPriceQuery, connection))
                    {
                        checkOldPriceCommand.Parameters.AddWithValue("@Id", model.Id);
                        decimal currentPrice = (decimal)await checkOldPriceCommand.ExecuteScalarAsync();

                        if (currentPrice != model.OldPrice)
                        {
                            return BadRequest("Old price does not match the current price in the database");
                        }
                    }

                    // Update the product price in the database
                    string updatePriceQuery = @"UPDATE Products 
                                SET Price = @NewPrice
                                WHERE ProductId = @Id";
                    using (MySqlCommand updatePriceCommand = new MySqlCommand(updatePriceQuery, connection))
                    {
                        updatePriceCommand.Parameters.AddWithValue("@NewPrice", model.NewPrice);
                        updatePriceCommand.Parameters.AddWithValue("@Id", model.Id);

                        int rowsAffected = await updatePriceCommand.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return Ok("Product price updated successfully");
                        }
                        else
                        {
                            return StatusCode(500, "Failed to update product price");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while updating product price: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }


        [HttpPut("update-stock-quantity/{id}")]
        public async Task<IActionResult> UpdateStockQuantity(int id, [FromBody] int newQuantity)
        {
            try
            {
                // Validate input
                if (newQuantity < 0)
                {
                    return BadRequest("New quantity cannot be negative");
                }

                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Prepare and execute the update query
                    string query = @"UPDATE Products 
                             SET StockQuantity = @NewQuantity
                             WHERE ProductId = @Id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@NewQuantity", newQuantity);
                        command.Parameters.AddWithValue("@Id", id);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return Ok("Stock quantity updated successfully");
                        }
                        else
                        {
                            return StatusCode(500, "Failed to update stock quantity. Product not found.");
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError($"MySQL error occurred while updating stock quantity: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred while updating stock quantity: {ex.Message}");
                return StatusCode(500, "An unexpected error occurred while processing the request");
            }
        }




        [HttpDelete("delete-product/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                // Validate input
                if (id <= 0)
                {
                    return BadRequest("Invalid product id");
                }

                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Retrieve the category name of the product being deleted
                    string categoryName;
                    string getCategoryNameQuery = @"SELECT CategoryName FROM Products WHERE ProductId = @Id";
                    using (MySqlCommand getCategoryNameCommand = new MySqlCommand(getCategoryNameQuery, connection))
                    {
                        getCategoryNameCommand.Parameters.AddWithValue("@Id", id);
                        categoryName = await getCategoryNameCommand.ExecuteScalarAsync() as string;
                    }

                    // Prepare and execute the delete query for the product
                    string deleteProductQuery = @"DELETE FROM Products WHERE ProductId = @Id";
                    using (MySqlCommand deleteProductCommand = new MySqlCommand(deleteProductQuery, connection))
                    {
                        deleteProductCommand.Parameters.AddWithValue("@Id", id);
                        int rowsAffected = await deleteProductCommand.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            // Check if the category has only one product left
                            string countProductsInCategoryQuery = @"SELECT COUNT(*) FROM Products WHERE CategoryName = @CategoryName";
                            using (MySqlCommand countProductsInCategoryCommand = new MySqlCommand(countProductsInCategoryQuery, connection))
                            {
                                countProductsInCategoryCommand.Parameters.AddWithValue("@CategoryName", categoryName);
                                int productCount = Convert.ToInt32(await countProductsInCategoryCommand.ExecuteScalarAsync());

                                if (productCount == 0)
                                {
                                    // Delete the category if it has only one product left
                                    string deleteCategoryQuery = @"DELETE FROM Categories WHERE Name = @CategoryName";
                                    using (MySqlCommand deleteCategoryCommand = new MySqlCommand(deleteCategoryQuery, connection))
                                    {
                                        deleteCategoryCommand.Parameters.AddWithValue("@CategoryName", categoryName);
                                        await deleteCategoryCommand.ExecuteNonQueryAsync();
                                    }
                                }
                            }

                            return Ok("Product deleted successfully");
                        }
                        else
                        {
                            return StatusCode(500, "Failed to delete product. Product not found.");
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError($"MySQL error occurred while deleting product: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred while deleting product: {ex.Message}");
                return StatusCode(500, "An unexpected error occurred while processing the request");
            }
        }


        [HttpDelete("delete-all-users")]
        public IActionResult DeleteAllUsers()
        {
            try
            {
                // Delete all users from the database
                if (DeleteAllUsersFromDatabase())
                {
                    // Users deleted successfully
                    return Ok("All users deleted successfully");
                }
                else
                {
                    // Failed to delete users
                    return StatusCode(500, "Failed to delete users");
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it accordingly
                _logger?.LogError($"An error occurred while processing the request: {ex.Message}");
                // Return internal server error
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        private bool DeleteAllUsersFromDatabase()
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string deleteQuery = "DELETE FROM Users";
                using (MySqlCommand deleteCommand = new MySqlCommand(deleteQuery, connection))
                {
                    int rowsAffected = deleteCommand.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        [HttpDelete("delete-user/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                if (await DeleteUserFromDatabaseAsync(userId) && await DeleteUserAuthenticationAsync(userId))
                {
                    return Ok($"User with ID {userId} and corresponding authentication deleted successfully");
                }
                else
                {
                    return StatusCode(500, $"Failed to delete user with ID {userId} or corresponding authentication");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"An error occurred while processing the request: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        private async Task<bool> DeleteUserFromDatabaseAsync(int userId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string deleteQuery = "DELETE FROM Users WHERE UserId = @UserId";
                    using (MySqlCommand deleteCommand = new MySqlCommand(deleteQuery, connection))
                    {
                        deleteCommand.Parameters.AddWithValue("@UserId", userId);
                        int rowsAffected = await deleteCommand.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (MySqlException ex)
            {
                _logger?.LogError($"MySQL error occurred while deleting user with ID {userId}: {ex.Message}");
                throw; // Re-throw the exception to be handled at the caller level
            }
            catch (Exception ex)
            {
                _logger?.LogError($"An unexpected error occurred while deleting user with ID {userId}: {ex.Message}");
                throw; // Re-throw the exception to be handled at the caller level
            }
        }

        private async Task<bool> DeleteUserAuthenticationAsync(int userId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string deleteQuery = "DELETE FROM UserAuthentication WHERE UserId = @UserId";
                    using (MySqlCommand deleteCommand = new MySqlCommand(deleteQuery, connection))
                    {
                        deleteCommand.Parameters.AddWithValue("@UserId", userId);
                        int rowsAffected = await deleteCommand.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (MySqlException ex)
            {
                _logger?.LogError($"MySQL error occurred while deleting authentication for user with ID {userId}: {ex.Message}");
                throw; // Re-throw the exception to be handled at the caller level
            }
            catch (Exception ex)
            {
                _logger?.LogError($"An unexpected error occurred while deleting authentication for user with ID {userId}: {ex.Message}");
                throw; // Re-throw the exception to be handled at the caller level
            }
        }

        // Additional functionalities can be implemented here

    }
}
