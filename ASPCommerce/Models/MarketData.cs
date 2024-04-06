using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.ComponentModel.DataAnnotations;

namespace ASPCommerce.Models
{
    public class ProductModel
    {
        public int Id { get; set; }
       
        [Required(ErrorMessage = "Product name is required")]
        public string ProductName { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a non-negative value")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be a non-negative integer")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public string Category { get; set; }

        public string CategoryName { get; set; }

        public string CategoryDescription { get; set; }

        [Required(ErrorMessage = "Brand is required")]
        public string Brand { get; set; }

        [Required(ErrorMessage = "Image URL is required")]
        public string ImageUrl { get; set; }

        [Required(ErrorMessage = "Date added is required")]
        public DateTime DateAdded { get; set; }
    }

    public class AddOrderModel
    {
        public int UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderDetailModel> OrderDetails { get; set; }
    }

    public class OrderDetailModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }


    public class UpdateProductModel
    {
        [Required(ErrorMessage = "Product name is required")]
        public string ProductName { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a non-negative value")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be a non-negative integer")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Category ID is required")]
        public string CategoryName { get; set; }  // Added CategoryId property

        [Required(ErrorMessage = "Brand is required")]
        public string Brand { get; set; }

        [Required(ErrorMessage = "Image URL is required")]
        [Url(ErrorMessage = "Invalid URL format")]
        public string ImageUrl { get; set; }

        [Required(ErrorMessage = "Date added is required")]
        public DateTime DateAdded { get; set; }
    }

    public class CategoryModel
    {
        [Required(ErrorMessage = "Category name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }
    }


    public class BuyItemModel
    {
        [Required(ErrorMessage = "Id is required")]
        public required int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a non-negative value")]
        public required decimal Price { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive integer")]
        public required int Quantity { get; set; }
    }

    public class ProductPriceUpdateModel
    {
        [Required(ErrorMessage = "Id is required")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Old price is required")]
        public decimal OldPrice { get; set; }

        [Required(ErrorMessage = "New price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "New price must be a non-negative value")]
        public decimal NewPrice { get; set; }
    }

}
