using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BusinessLogicLayer.Models;

namespace IntegrationLayer.Services
{
    /// <summary>
    /// Сервіс для отримання курсів валют з Monobank API
    /// </summary>
    public class MonobankService
    {
        private readonly HttpClient _httpClient;
        private const string API_URL = "https://api.monobank.ua/bank/currency";
        private List<CurrencyRate> _cachedRates;
        private DateTime _lastUpdate;
        private const int CACHE_MINUTES = 5; // Кеш на 5 хвилин

        public MonobankService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MatePost/1.0");
            _cachedRates = new List<CurrencyRate>();
            _lastUpdate = DateTime.MinValue;
        }

        /// <summary>
        /// Отримує курс EUR до UAH з кешуванням
        /// </summary>
        public async Task<CurrencyResult> GetEurToUahRateAsync()
        {
            try
            {
                // Перевіряємо чи потрібно оновити кеш
                if ((DateTime.Now - _lastUpdate).TotalMinutes > CACHE_MINUTES || _cachedRates.Count == 0)
                {
                    await UpdateCurrencyRatesAsync();
                }

                // EUR код = 978, UAH код = 980
                var eurRate = _cachedRates.FirstOrDefault(r =>
                    r.CurrencyCodeA == 978 && r.CurrencyCodeB == 980);

                if (eurRate != null)
                {
                    decimal rate = eurRate.RateSell > 0 ? eurRate.RateSell : eurRate.RateCross;
                    return new CurrencyResult
                    {
                        Success = true,
                        Rate = rate,
                        Message = $"Курс оновлено: {_lastUpdate:HH:mm:ss}",
                        LastUpdate = _lastUpdate
                    };
                }

                // Якщо не вдалось отримати, повертаємо дефолтне значення
                return new CurrencyResult
                {
                    Success = false,
                    Rate = DeliveryConfiguration.EuroToUahRate,
                    Message = "Не вдалося отримати курс з API. Використовується дефолтне значення.",
                    LastUpdate = _lastUpdate
                };
            }
            catch (Exception ex)
            {
                return new CurrencyResult
                {
                    Success = false,
                    Rate = DeliveryConfiguration.EuroToUahRate,
                    Message = $"Помилка: {ex.Message}. Використовується дефолтне значення.",
                    LastUpdate = _lastUpdate
                };
            }
        }

        /// <summary>
        /// Отримує всі доступні курси валют
        /// </summary>
        public async Task<AllRatesResult> GetAllRatesAsync()
        {
            try
            {
                await UpdateCurrencyRatesAsync();

                var currencyNames = new Dictionary<int, string>
                {
                    { 978, "EUR" },
                    { 840, "USD" },
                    { 985, "PLN" },
                    { 826, "GBP" },
                    { 643, "RUB" },
                    { 980, "UAH" }
                };

                var displayRates = new List<DisplayRate>();

                foreach (var rate in _cachedRates.Where(r => r.RateSell > 0))
                {
                    var currencyA = currencyNames.ContainsKey(rate.CurrencyCodeA)
                        ? currencyNames[rate.CurrencyCodeA]
                        : rate.CurrencyCodeA.ToString();

                    var currencyB = currencyNames.ContainsKey(rate.CurrencyCodeB)
                        ? currencyNames[rate.CurrencyCodeB]
                        : rate.CurrencyCodeB.ToString();

                    displayRates.Add(new DisplayRate
                    {
                        CurrencyPair = $"{currencyA}/{currencyB}",
                        RateBuy = rate.RateBuy,
                        RateSell = rate.RateSell
                    });
                }

                return new AllRatesResult
                {
                    Success = true,
                    Rates = displayRates,
                    LastUpdate = _lastUpdate,
                    Message = "Курси успішно завантажено"
                };
            }
            catch (Exception ex)
            {
                return new AllRatesResult
                {
                    Success = false,
                    Rates = new List<DisplayRate>(),
                    LastUpdate = _lastUpdate,
                    Message = $"Помилка отримання курсів: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Оновлює кеш курсів валют
        /// </summary>
        private async Task UpdateCurrencyRatesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(API_URL);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var rates = JsonSerializer.Deserialize<List<CurrencyRate>>(json);

                    if (rates != null && rates.Count > 0)
                    {
                        _cachedRates = rates;
                        _lastUpdate = DateTime.Now;
                    }
                }
            }
            catch (Exception)
            {
                // При помилці залишаємо старий кеш
                if (_cachedRates.Count == 0)
                {
                    // Якщо кеш порожній, ініціалізуємо дефолтними значеннями
                    _cachedRates = new List<CurrencyRate>
                    {
                        new CurrencyRate
                        {
                            CurrencyCodeA = 978,
                            CurrencyCodeB = 980,
                            RateSell = DeliveryConfiguration.EuroToUahRate,
                            RateBuy = DeliveryConfiguration.EuroToUahRate,
                            RateCross = DeliveryConfiguration.EuroToUahRate
                        }
                    };
                }
            }
        }
    }

    /// <summary>
    /// Модель курсу валюти з Monobank API
    /// </summary>
    public class CurrencyRate
    {
        [JsonPropertyName("currencyCodeA")]
        public int CurrencyCodeA { get; set; }

        [JsonPropertyName("currencyCodeB")]
        public int CurrencyCodeB { get; set; }

        [JsonPropertyName("date")]
        public long Date { get; set; }

        [JsonPropertyName("rateSell")]
        public decimal RateSell { get; set; }

        [JsonPropertyName("rateBuy")]
        public decimal RateBuy { get; set; }

        [JsonPropertyName("rateCross")]
        public decimal RateCross { get; set; }
    }

    /// <summary>
    /// Результат отримання курсу EUR/UAH
    /// </summary>
    public class CurrencyResult
    {
        public bool Success { get; set; }
        public decimal Rate { get; set; }
        public string Message { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    /// <summary>
    /// Результат отримання всіх курсів
    /// </summary>
    public class AllRatesResult
    {
        public bool Success { get; set; }
        public List<DisplayRate> Rates { get; set; }
        public DateTime LastUpdate { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Курс для відображення
    /// </summary>
    public class DisplayRate
    {
        public string CurrencyPair { get; set; }
        public decimal RateBuy { get; set; }
        public decimal RateSell { get; set; }
    }
}