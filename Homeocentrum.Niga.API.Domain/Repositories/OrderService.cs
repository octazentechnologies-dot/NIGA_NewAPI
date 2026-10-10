#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Master;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http;

namespace Homeocentrum.Niga.API.Domain.Business.Implementation
{
    public class OrderService : IOrderService
    {
        private readonly string _keyId;

        private readonly string _keySecret;

        public OrderService(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _keyId = configuration["Razorpay:KeyId"] ?? "";
            _keySecret = configuration["Razorpay:KeySecret"] ?? "";
        }

        public async Task<string> GenerateOrderAsync(OrderModel orderModel)
        {
            if (string.IsNullOrEmpty(_keyId) || string.IsNullOrEmpty(_keySecret))
                throw new InvalidOperationException("Razorpay:KeyId and Razorpay:KeySecret are not configured.");
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(_keyId + ":" + _keySecret)));

                    var content = new StringContent(JsonConvert.SerializeObject(new
                    {
                        amount = orderModel.Amount * 100, // Example amount (in paisa)
                        currency =orderModel.Currency, // Example currency
                        receipt =orderModel.Receipt, // Example receipt
                        payment_capture = orderModel.PaymentCapture // Auto capture payment
                    }), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("https://api.razorpay.com/v1/orders", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        dynamic data = JObject.Parse(result);
                        return data.id;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                // Log or handle the exception
                throw;
            }
        }
    }
}
