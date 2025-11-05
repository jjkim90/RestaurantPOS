namespace RestaurantPOS.Core.DTOs.TossPayments
{
    public class TossPaymentsConfiguration
    {
        public string ClientKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public bool IsTestMode { get; set; } = true;
        public string BaseUrl { get; set; } = "https://api.tosspayments.com";  // 테스트/운영 모두 동일한 URL 사용
    }
}