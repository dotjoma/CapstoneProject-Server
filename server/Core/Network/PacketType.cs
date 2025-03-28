using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Core.Network
{
    public enum PacketType
    {
        None = 0,

        // Auth Packet
        Register = 1,
        RegisterResponse = 2,
        Login = 3,
        LoginResponse = 4,
        Logout = 5,

        // Product Packet
        CreateProduct = 6,
        CreateProductResponse = 7,
        UpdateProduct = 8,
        UpdateProductResponse = 9,
        DeleteProduct = 10,
        DeleteProductResponse = 11,
        GetProduct = 12,
        GetProductResponse = 13,

        // Category Packet
        GetCategory = 14,
        GetCategoryResponse = 15,
        GetSubCategory = 16,
        GetSubCategoryResponse = 17,
        CreateCategory = 18,
        CreateCategoryResponse = 19,
        CreateSubCategory = 20,
        CreateSubCategoryResponse = 21,

        // Unit Packet
        GetUnit = 22,
        GetUnitResponse = 23,
        CreateUnit = 24,
        CreateUnitResponse = 25,

        GetAllSubcategory = 26,
        GetAllSubcategoryResponse = 27,

        CreateDiscount = 28,
        CreateDiscountResponse = 29,
        GetDiscount = 30,
        GetDiscountResponse = 31,

        // Transaction Packet
        GenerateTransactionNumbers = 32,
        GenerateTransactionNumbersResponse = 33,
        GetNextTransactionId = 34,
        GetNextTransactionIdResponse = 35,
        GetNextOrderNumber = 36,
        GetNextOrderNumberResponse = 37,
        SaveTransaction = 38,
        SaveTransactionResponse = 39,
        RemoveTransaction = 40,
        RemoveTransactionResponse = 41,
        ProcessTransaction = 42,
        ProcessTransactionResponse = 43,

        // SalesReport Packet
        GetSalesReport = 44,
        GetSalesReportResponse = 45,
    }
}
