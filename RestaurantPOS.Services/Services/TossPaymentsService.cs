using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestaurantPOS.Core.DTOs.TossPayments;
using RestaurantPOS.Core.Interfaces;
using Serilog;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantPOS.Services.Services
{
    public class TossPaymentsService : ITossPaymentsService
    {
        private readonly HttpClient _httpClient;
        private readonly TossPaymentsConfiguration _configuration;
        private readonly ILogger _logger;

        public TossPaymentsService(IConfiguration configuration, ILogger logger)
        {
            _logger = logger;
            
            _configuration = new TossPaymentsConfiguration();
            
            try
            {
                if (configuration != null)
                {
                    var section = configuration.GetSection("TossPayments");
                    if (section != null && section.Exists())
                    {
                        section.Bind(_configuration);
                    }
                    else
                    {
                        _logger.Warning("TossPayments configuration section not found. Using default values.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to bind TossPayments configuration. Using default values.");
            }
            
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_configuration.BaseUrl ?? "https://api.tosspayments.com")
            };
            
            // Basic Authentication 설정
            if (!string.IsNullOrEmpty(_configuration.SecretKey))
            {
                var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_configuration.SecretKey}:"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
            }
            else
            {
                _logger.Warning("TossPayments SecretKey is not configured.");
            }
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("Toss-Version", "2022-11-16");
        }

        public async Task<PaymentResponseDto> ConfirmPaymentAsync(string paymentKey, string orderId, decimal amount)
        {
            try
            {
                _logger.Information("결제 승인 요청 시작 - PaymentKey: {PaymentKey}, OrderId: {OrderId}, Amount: {Amount}", 
                    paymentKey, orderId, amount);

                var request = new PaymentConfirmRequestDto
                {
                    PaymentKey = paymentKey,
                    OrderId = orderId,
                    Amount = (int)Math.Round(amount)
                };

                var json = JsonConvert.SerializeObject(request);
                _logger.Debug("결제 승인 요청 JSON: {Json}", json);
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // 멱등성 키로 orderId 사용 - 중복 결제 방지
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/payments/confirm")
                {
                    Content = content
                };
                httpRequest.Headers.Add("Idempotency-Key", orderId);

                // 디버깅을 위한 추가 로그
                _logger.Debug("요청 URL: {Url}", _httpClient.BaseAddress + "v1/payments/confirm");
                _logger.Debug("요청 헤더 - Authorization: {Auth}", _httpClient.DefaultRequestHeaders.Authorization?.ToString() ?? "없음");
                _logger.Debug("요청 헤더 - Idempotency-Key: {Key}", orderId);

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger.Debug("결제 승인 응답 - Status: {Status}, Content: {Content}", 
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var paymentResponse = JsonConvert.DeserializeObject<PaymentResponseDto>(responseContent);
                    _logger.Information("결제 승인 성공 - PaymentKey: {PaymentKey}, Status: {Status}", 
                        paymentKey, paymentResponse.Status);
                    return paymentResponse;
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    var errorCode = errorResponse?.code?.ToString() ?? "UNKNOWN_ERROR";
                    var errorMessage = errorResponse?.message?.ToString() ?? "알 수 없는 오류가 발생했습니다.";
                    
                    _logger.Error("결제 승인 실패 - Code: {Code}, Message: {Message}", errorCode, errorMessage);
                    throw new Exception($"결제 승인 실패: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "결제 승인 중 오류 발생");
                throw;
            }
        }

        public async Task<PaymentResponseDto> GetPaymentAsync(string paymentKey)
        {
            try
            {
                _logger.Information("결제 정보 조회 - PaymentKey: {PaymentKey}", paymentKey);

                var response = await _httpClient.GetAsync($"/v1/payments/{paymentKey}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var paymentResponse = JsonConvert.DeserializeObject<PaymentResponseDto>(responseContent);
                    _logger.Information("결제 정보 조회 성공 - PaymentKey: {PaymentKey}, Status: {Status}", 
                        paymentKey, paymentResponse.Status);
                    return paymentResponse;
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    var errorCode = errorResponse?.code?.ToString() ?? "UNKNOWN_ERROR";
                    var errorMessage = errorResponse?.message?.ToString() ?? "알 수 없는 오류가 발생했습니다.";
                    
                    _logger.Error("결제 정보 조회 실패 - Code: {Code}, Message: {Message}", errorCode, errorMessage);
                    throw new Exception($"결제 정보 조회 실패: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "결제 정보 조회 중 오류 발생");
                throw;
            }
        }

        public async Task<PaymentResponseDto> CancelPaymentAsync(string paymentKey, string cancelReason, decimal? cancelAmount = null)
        {
            try
            {
                _logger.Information("결제 취소 요청 - PaymentKey: {PaymentKey}, Reason: {Reason}, Amount: {Amount}", 
                    paymentKey, cancelReason, cancelAmount);

                var requestBody = new
                {
                    cancelReason = cancelReason,
                    cancelAmount = cancelAmount.HasValue ? (int?)Math.Round(cancelAmount.Value) : null
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/v1/payments/{paymentKey}/cancel", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var paymentResponse = JsonConvert.DeserializeObject<PaymentResponseDto>(responseContent);
                    _logger.Information("결제 취소 성공 - PaymentKey: {PaymentKey}, Status: {Status}", 
                        paymentKey, paymentResponse.Status);
                    return paymentResponse;
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    var errorCode = errorResponse?.code?.ToString() ?? "UNKNOWN_ERROR";
                    var errorMessage = errorResponse?.message?.ToString() ?? "알 수 없는 오류가 발생했습니다.";
                    
                    _logger.Error("결제 취소 실패 - Code: {Code}, Message: {Message}", errorCode, errorMessage);
                    throw new Exception($"결제 취소 실패: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "결제 취소 중 오류 발생");
                throw;
            }
        }

        public string GetClientKey()
        {
            return _configuration.ClientKey;
        }

        public string GetSecretKey()
        {
            return _configuration.SecretKey;
        }

        public bool IsTestMode()
        {
            return _configuration.IsTestMode;
        }
    }
}