namespace RestaurantPOS.Core.Enums
{
    public enum OrderDetailStatus
    {
        Pending,    // 아직 주방에 전송되지 않은 상태
        Confirmed,  // 주방에 전송된 상태
        Cancelled   // 취소된 상태
    }
}