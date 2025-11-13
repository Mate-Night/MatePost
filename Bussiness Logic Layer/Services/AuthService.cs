using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessLogicLayer.Models;

namespace BusinessLogicLayer.Services
{
    /// <summary>
    /// Сервіс для роботи з Security API (аутентифікація та управління користувачами)
    /// </summary>
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private string _currentToken;

        public AuthService(string baseUrl = "https://localhost:7030")
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl)
            };
            // Дозволяємо самопідписані сертифікати для HTTPS
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(_baseUrl)
            };
        }

        /// <summary>
        /// Реєстрація нового користувача (ТІЛЬКИ для адміна)
        /// </summary>
        public async Task<OperationResult> RegisterAsync(string adminToken, string username, string password, string role)
        {
            try
            {
                var requestData = new
                {
                    username = username,
                    password = password,
                    role = role
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/register")
                {
                    Content = content
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

                var response = await _httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return OperationResult.Ok("Користувача зареєстровано успішно!");
                }

                return OperationResult.Fail($"Помилка реєстрації: {responseText}");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Помилка з'єднання: {ex.Message}");
            }
        }

        /// <summary>
        /// Логін користувача (отримання токена)
        /// </summary>
        public async Task<OperationResult> LoginAsync(string username, string password)
        {
            try
            {
                var requestData = new
                {
                    username = username,
                    password = password
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/users/login", content);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<LoginResponse>(responseText);
                    _currentToken = result.token;

                    return OperationResult.Ok(new
                    {
                        Token = result.token,
                        Role = result.role
                    });
                }

                return OperationResult.Fail($"Помилка логіну: {responseText}");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Помилка з'єднання: {ex.Message}");
            }
        }

        /// <summary>
        /// Отримання списку всіх користувачів (потрібен токен адміна)
        /// </summary>
        public async Task<OperationResult> GetUsersAsync(string adminToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

                var response = await _httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // API повертає Task<UsersResponse>[], розпаковуємо
                    var usersJson = JsonSerializer.Deserialize<JsonElement>(responseText);
                    var users = new List<UserInfo>();

                    foreach (var userElement in usersJson.EnumerateArray())
                    {
                        var resultProp = userElement.GetProperty("result");
                        users.Add(new UserInfo
                        {
                            username = resultProp.GetProperty("userName").GetString(),
                            role = resultProp.GetProperty("role").GetString()
                        });
                    }

                    return OperationResult.Ok(users.ToArray());
                }

                return OperationResult.Fail($"Помилка отримання користувачів: {responseText}");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Помилка з'єднання: {ex.Message}");
            }
        }

        /// <summary>
        /// Зміна ролі користувача (потрібен токен адміна)
        /// </summary>
        public async Task<OperationResult> ChangeUserRoleAsync(string adminToken, string username, string newRole)
        {
            try
            {
                var requestData = new
                {
                    username = username,
                    role = newRole
                };
                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Put, "/api/users/role/set")
                {
                    Content = content
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

                var response = await _httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return OperationResult.Ok("Роль змінено успішно!");
                }

                return OperationResult.Fail($"Помилка зміни ролі: {responseText}");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Помилка з'єднання: {ex.Message}");
            }
        }

        /// <summary>
        /// Зміна власного пароля (потрібен власний токен)
        /// </summary>
        public async Task<OperationResult> ChangePasswordAsync(string userToken, string oldPassword, string newPassword)
        {
            try
            {
                var requestData = new
                {
                    oldPassword = oldPassword,
                    newPassword = newPassword
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Put, "/api/users/password/change")
                {
                    Content = content
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                var response = await _httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return OperationResult.Ok("Пароль змінено успішно!");
                }

                return OperationResult.Fail($"Помилка зміни пароля: {responseText}");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Помилка з'єднання: {ex.Message}");
            }
        }

        /// <summary>
        /// Отримати поточний токен
        /// </summary>
        public string GetCurrentToken() => _currentToken;

        /// <summary>
        /// Встановити токен вручну
        /// </summary>
        public void SetToken(string token) => _currentToken = token;
    }

    // ===== DTO класи для роботи з API =====

    public class LoginResponse
    {
        public string token { get; set; }
        public string role { get; set; }
    }

    public class UserInfo
    {
        public string username { get; set; }
        public string role { get; set; }
    }
}