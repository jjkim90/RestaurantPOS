using System;

namespace RestaurantPOS.Core.DTOs.TossPayments
{
    public class PaymentResponseDto
    {
        public string PaymentKey { get; set; }
        public string Type { get; set; }
        public string OrderId { get; set; }
        public string OrderName { get; set; }
        public string MId { get; set; }
        public string Currency { get; set; }
        public string Method { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public string Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public bool UseEscrow { get; set; }
        public string LastTransactionKey { get; set; }
        public decimal SuppliedAmount { get; set; }
        public decimal Vat { get; set; }
        public bool CultureExpense { get; set; }
        public decimal TaxFreeAmount { get; set; }
        public decimal TaxExemptionAmount { get; set; }
        public CardDto Card { get; set; }
        public VirtualAccountDto VirtualAccount { get; set; }
        public string Secret { get; set; }
        public MobilePhoneDto MobilePhone { get; set; }
        public GiftCertificateDto GiftCertificate { get; set; }
        public TransferDto Transfer { get; set; }
        public ReceiptDto Receipt { get; set; }
        public CheckoutDto Checkout { get; set; }
        public EasyPayDto EasyPay { get; set; }
        public string Country { get; set; }
        public FailureDto Failure { get; set; }
        public CashReceiptDto CashReceipt { get; set; }
        public CashReceiptDto[] CashReceipts { get; set; }
        public DiscountDto Discount { get; set; }
    }

    public class CardDto
    {
        public decimal Amount { get; set; }
        public string IssuerCode { get; set; }
        public string AcquirerCode { get; set; }
        public string Number { get; set; }
        public int InstallmentPlanMonths { get; set; }
        public string ApproveNo { get; set; }
        public bool UseCardPoint { get; set; }
        public string CardType { get; set; }
        public string OwnerType { get; set; }
        public string AcquireStatus { get; set; }
        public bool IsInterestFree { get; set; }
        public string InterestPayer { get; set; }
    }

    public class VirtualAccountDto
    {
        public string AccountType { get; set; }
        public string AccountNumber { get; set; }
        public string BankCode { get; set; }
        public string CustomerName { get; set; }
        public DateTime DueDate { get; set; }
        public string RefundStatus { get; set; }
        public bool Expired { get; set; }
        public string SettlementStatus { get; set; }
        public RefundReceiveAccountDto RefundReceiveAccount { get; set; }
    }

    public class RefundReceiveAccountDto
    {
        public string BankCode { get; set; }
        public string AccountNumber { get; set; }
        public string HolderName { get; set; }
    }

    public class MobilePhoneDto
    {
        public string CustomerMobilePhone { get; set; }
        public string SettlementStatus { get; set; }
        public string ReceiptUrl { get; set; }
    }

    public class GiftCertificateDto
    {
        public string ApproveNo { get; set; }
        public string SettlementStatus { get; set; }
    }

    public class TransferDto
    {
        public string BankCode { get; set; }
        public string SettlementStatus { get; set; }
    }

    public class ReceiptDto
    {
        public string Url { get; set; }
    }

    public class CheckoutDto
    {
        public string Url { get; set; }
    }

    public class EasyPayDto
    {
        public string Provider { get; set; }
        public decimal Amount { get; set; }
        public decimal DiscountAmount { get; set; }
    }

    public class FailureDto
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }

    public class CashReceiptDto
    {
        public string Type { get; set; }
        public string ReceiptKey { get; set; }
        public string IssueNumber { get; set; }
        public string ReceiptUrl { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxFreeAmount { get; set; }
    }

    public class DiscountDto
    {
        public decimal Amount { get; set; }
    }
}