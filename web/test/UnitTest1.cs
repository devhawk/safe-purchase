using System;
using Microsoft.Extensions.Logging.Abstractions;
using SafePuchaseWeb.Controllers;
using SafePuchaseWeb.Models;
using Xunit;

namespace test
{
    public class UnitTest1
    {
        [Fact]
        public async void Test1()
        {
            var controller = new HomeController(NullLogger<HomeController>.Instance);
            var model = new CreateSaleViewModel()
            {
                Description = "widget",
                Price = 50,
                SaleId = Guid.Parse("6d0c626f-4fd6-432a-ba21-d0600b1fecd3"),
            };

            await controller.CreateSale(model);
        }
    }
}
