using Newtonsoft.Json;

namespace RestaurantPOS.Core.DTOs.TossPayments
{
    public class PaymentConfirmRequestDto
    {
        [JsonProperty("paymentKey")]
        public string PaymentKey { get; set; }
        
        [JsonProperty("orderId")]
        public string OrderId { get; set; }
        
        [JsonProperty("amount")]
        public int Amount { get; set; }
    }
}