namespace RestaurantPOS.Core.DTOs.TossPayments
{
    public class PaymentRequestDto
    {
        public string Method { get; set; } = "카드";
        public int Amount { get; set; }
        public string OrderId { get; set; }
        public string OrderName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerMobilePhone { get; set; }
        public bool UseEscrow { get; set; } = false;
        public string SuccessUrl { get; set; }
        public string FailUrl { get; set; }
        public string WindowTarget { get; set; } = "iframe";
        public string FlowMode { get; set; } = "DEFAULT";
    }
}