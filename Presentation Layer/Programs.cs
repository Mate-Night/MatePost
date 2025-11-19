using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using BusinessLogicLayer.Models;
using BusinessLogicLayer.Services;
using PersistenceLayer;
using IntegrationLayer.Services;
using IntegrationLayer.Database;
using Microsoft.EntityFrameworkCore;

namespace PresentationLayer
{
    class Program
    {
        // Сервіси Business Logic Layer
        private static ClientService _clientService = null!;
        private static ParcelService _parcelService = null!;
        private static OperatorService _operatorService = null!;
        private static DeliveryPointService _deliveryPointService = null!;
        private static CalculationService _calculationService = null!;
        private static StatisticsService _statisticsService = null!;

        // Сервіси Integration Layer
        private static AuthService _authService = null!;
        private static MonobankService _monobankService = null!;
        private static DatabaseService _databaseService = null!;

        // Persistence Layer
        private static JsonDataStore _dataStore = null!;

        // Аутентифікація
        private static string _currentUserToken = null;
        private static string _currentUserRole = null;
        private static string _currentUsername = null;

        // Режим роботи з даними
        private static bool _useDatabaseMode = true;

        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;

            await InitializeServicesAsync();

            if (!AuthenticateUser())
            {
                Console.WriteLine("Не вдалось увійти в систему.");
                return;
            }

            await LoadDataAsync();
            await UpdateCurrencyRatesAsync();

            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("╔══════════════════════════════════════╗");
                Console.WriteLine("║     MATEPOST - Поштова Система       ║");
                Console.WriteLine("╚══════════════════════════════════════╝");
                Console.WriteLine($"Користувач: {_currentUsername} ({_currentUserRole})");
                Console.WriteLine($"Режим даних: {(_useDatabaseMode ? "База даних (SQLite)" : "JSON файли")}");
                Console.WriteLine($"Курс EUR/UAH: {_calculationService.GetEuroRate():F2} грн");
                Console.WriteLine();

                switch (_currentUserRole)
                {
                    case "Admin":
                        exit = await ShowAdminMenu();
                        break;
                    case "Operator":
                        exit = await ShowOperatorRoleMenu();
                        break;
                    case "Client":
                        exit = await ShowClientRoleMenu();
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Невідома роль: {_currentUserRole}");
                        Console.WriteLine("Зверніться до адміністратора для виправлення.");
                        Console.ResetColor();
                        Console.ReadKey();
                        exit = true;
                        break;
                }
            }
            await SaveDataAsync();
        }

        static async Task<bool> ShowAdminMenu()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("АДМІНІСТРАТИВНА ПАНЕЛЬ");
            Console.ResetColor();

            Console.WriteLine("\nA. Управління користувачами");
            Console.WriteLine("1. Управління клієнтами");
            Console.WriteLine("2. Управління посилками");
            Console.WriteLine("3. Управління операторами");
            Console.WriteLine("4. Управління точками доставки");
            Console.WriteLine("5. Повна статистика");
            Console.WriteLine("6. Оновити курси валют");
            Console.WriteLine("7. Зберегти дані");
            Console.WriteLine("8. Перемкнути режим (БД ↔ JSON)");
            Console.WriteLine("0. Вихід");
            Console.Write("\nОберіть опцію: ");

            string choice = Console.ReadLine() ?? "";

            switch (choice.ToLower())
            {
                case "a": UserManagementMenu(); break;
                case "1": ClientMenu(); break;
                case "2": ParcelMenu(); break;
                case "3": OperatorMenu(); break;
                case "4": DeliveryPointMenu(); break;
                case "5": ShowStatistics(); break;
                case "6": await ShowAllCurrencyRatesAsync(); break;
                case "7": await SaveDataAsync(); Console.WriteLine("\n✓ Дані збережено!"); Console.ReadKey(); break;
                case "8": await ToggleDataModeAsync(); break;
                case "0": return true;
            }

            return false;
        }

        static async Task<bool> ShowOperatorRoleMenu()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("ПАНЕЛЬ ОПЕРАТОРА");
            Console.ResetColor();

            // Знаходимо оператора
            var op = _operatorService.GetAll().FirstOrDefault(o =>
                o.Name.ToLower().Contains(_currentUsername.ToLower()));

            if (op != null)
            {
                Console.WriteLine($"\n {op.Name}");
                Console.WriteLine($" Оброблено: {op.ProcessedParcels} посилок | Ефективність: {op.Efficiency:F1}%");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n Профіль оператора не знайдено в системі!");
                Console.WriteLine("Зверніться до адміністратора для створення профілю.");
                Console.ResetColor();
            }

            Console.WriteLine("\n1. Прийняти посилку");
            Console.WriteLine("2. Змінити статус посилки");
            Console.WriteLine("3. Мої оброблені посилки");
            Console.WriteLine("4. Пошук посилки за трекінгом");
            Console.WriteLine("5. Посилки що очікують обробки");
            Console.WriteLine("0. Вихід");
            Console.Write("\nОберіть опцію: ");

            string choice = Console.ReadLine() ?? "";

            switch (choice)
            {
                case "1": AcceptParcelAsOperator(); break;
                case "2": ChangeParcelStatusAsOperator(); break;
                case "3": ShowMyProcessedParcels(); break;
                case "4": TrackParcel(); break;
                case "5": ShowPendingParcels(); break;
                case "0": return true;
            }

            return false;
        }

        static void AcceptParcelAsOperator()
        {
            Console.Clear();
            Console.WriteLine("═══ ПРИЙНЯТИ ПОСИЛКУ ═══\n");

            var op = _operatorService.GetAll().FirstOrDefault(o =>
                o.Name.ToLower().Contains(_currentUsername.ToLower()));

            if (op == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Ваш профіль оператора не знайдено!");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }

            Console.Write("Трекінг-номер: ");
            string trackingNumber = Console.ReadLine() ?? "";

            var parcel = _parcelService.GetByTrackingNumber(trackingNumber);
            if (parcel == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" Посилку не знайдено!");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }

            if (parcel.CurrentStatus != ParcelStatus.AwaitingShipment)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($" Посилка вже має статус: {GetStatusName(parcel.CurrentStatus)}");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }

            var sender = _clientService.GetById(parcel.SenderId);
            var receiver = _clientService.GetById(parcel.ReceiverId);

            Console.WriteLine($"\n Посилка: {parcel.TrackingNumber}");
            Console.WriteLine($" Від: {sender?.FullName ?? "N/A"}");
            Console.WriteLine($" До: {receiver?.FullName ?? "N/A"}");
            Console.WriteLine($" Вага: {parcel.Weight} кг");
            Console.WriteLine($" Оціночна вартість: {parcel.DeclaredValue} грн");

            if (parcel.RequiresOperatorConfirmation())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n УВАГА: Потребує підтвердження (вартість > 5000 грн)");
                Console.ResetColor();
            }

            if (parcel.IsPriorityProcessing)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⭐ Пріоритетна обробка (клієнт - Легенда)");
                Console.ResetColor();
            }

            Console.Write("\n Підтвердити прийом? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                var result = _parcelService.ChangeStatus(trackingNumber,
                    ParcelStatus.AcceptedByOperator,
                    $"Прийнято оператором {op.Name}",
                    op.Id);

                if (result.Success)
                {
                    op.IncrementProcessed();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n Посилку прийнято успішно!");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n Помилка: {result.Data}");
                    Console.ResetColor();
                }
            }

            Console.ReadKey();
        }

        static void ChangeParcelStatusAsOperator()
        {
            Console.Clear();
            Console.WriteLine("═══ ЗМІНИТИ СТАТУС ПОСИЛКИ ═══\n");

            var op = _operatorService.GetAll().FirstOrDefault(o =>
                o.Name.ToLower().Contains(_currentUsername.ToLower()));

            if (op == null)
            {
                Console.WriteLine(" Ваш профіль оператора не знайдено!");
                Console.ReadKey();
                return;
            }

            Console.Write("Трекінг-номер: ");
            string trackingNumber = Console.ReadLine() ?? "";

            var parcel = _parcelService.GetByTrackingNumber(trackingNumber);
            if (parcel == null)
            {
                Console.WriteLine(" Посилку не знайдено!");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nПоточний статус: {GetStatusName(parcel.CurrentStatus)}");
            Console.WriteLine("\nНовий статус:");
            Console.WriteLine("1. Прийнято оператором");
            Console.WriteLine("2. В дорозі");
            Console.WriteLine("3. На складі в місті призначення");
            Console.WriteLine("4. Доставлено");
            Console.Write("\nОберіть: ");

            ParcelStatus newStatus = Console.ReadLine() switch
            {
                "1" => ParcelStatus.AcceptedByOperator,
                "2" => ParcelStatus.InTransit,
                "3" => ParcelStatus.AtWarehouse,
                "4" => ParcelStatus.Delivered,
                _ => parcel.CurrentStatus
            };

            if (newStatus == parcel.CurrentStatus)
            {
                Console.WriteLine("Статус не змінено.");
                Console.ReadKey();
                return;
            }

            Console.Write("\nПримітка (Enter - пропустити): ");
            string note = Console.ReadLine() ?? "";

            var result = _parcelService.ChangeStatus(trackingNumber, newStatus,
                $"{note} (оператор: {op.Name})", op.Id);

            if (result.Success)
            {
                if (newStatus == ParcelStatus.AcceptedByOperator)
                    op.IncrementProcessed();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n Статус оновлено!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n {result.Data}");
                Console.ResetColor();
            }

            Console.ReadKey();
        }

        static void ShowMyProcessedParcels()
        {
            Console.Clear();
            Console.WriteLine("═══ МОЇ ОБРОБЛЕНІ ПОСИЛКИ ═══\n");

            var op = _operatorService.GetAll().FirstOrDefault(o =>
                o.Name.ToLower().Contains(_currentUsername.ToLower()));

            if (op == null)
            {
                Console.WriteLine(" Оператора не знайдено!");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($" Оператор: {op.Name}");
            Console.WriteLine($" Всього оброблено: {op.ProcessedParcels} посилок");
            Console.WriteLine($" Ефективність: {op.Efficiency:F1}%\n");

            var parcels = _parcelService.GetAll()
                .Where(p => p.StatusHistory.Any(s =>
                    s.Note != null && s.Note.Contains(op.Name)))
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            if (parcels.Count == 0)
            {
                Console.WriteLine("Ви ще не обробили жодної посилки.");
            }
            else
            {
                Console.WriteLine($"Знайдено: {parcels.Count} посилок\n");
                foreach (var parcel in parcels.Take(10))
                {
                    DisplayParcelInfo(parcel);
                }

                if (parcels.Count > 10)
                    Console.WriteLine($"... та ще {parcels.Count - 10} посилок");
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ShowPendingParcels()
        {
            Console.Clear();
            Console.WriteLine("═══ ПОСИЛКИ ЩО ОЧІКУЮТЬ ОБРОБКИ ═══\n");

            var pendingParcels = _parcelService.GetAll()
                .Where(p => p.CurrentStatus == ParcelStatus.AwaitingShipment)
                .OrderBy(p => p.IsPriorityProcessing ? 0 : 1)
                .ThenBy(p => p.CreatedAt)
                .ToList();

            if (pendingParcels.Count == 0)
            {
                Console.WriteLine(" Немає посилок що очікують обробки!");
            }
            else
            {
                Console.WriteLine($" Всього: {pendingParcels.Count} посилок\n");

                foreach (var parcel in pendingParcels)
                {
                    if (parcel.IsPriorityProcessing)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(" ПРІОРИТЕТНА");
                        Console.ResetColor();
                    }

                    DisplayParcelInfo(parcel);
                }
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static async Task<bool> ShowClientRoleMenu()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" ПАНЕЛЬ КЛІЄНТА");
            Console.ResetColor();

            // Знаходимо клієнта за email або іменем
            var client = _clientService.GetAll().FirstOrDefault(c =>
                c.Email.ToLower() == _currentUsername.ToLower() ||
                c.FullName.ToLower().Contains(_currentUsername.ToLower()));

            if (client != null)
            {
                int parcelCount = _clientService.GetClientParcelCount(client.Id);

                Console.WriteLine($"\n {client.FullName}");
                Console.ForegroundColor = GetLoyaltyColor(client.Status);
                Console.WriteLine($" {GetLoyaltyStatusName(client.Status)} |  Знижка: {client.GetDiscount() * 100}%");
                Console.ResetColor();
                Console.WriteLine($" Відправлено: {parcelCount} посилок");

                if (client.CanUseDiscount())
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(" Знижка доступна цього місяця");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine(" Знижка використана");
                    Console.ResetColor();
                }

                if (client.IsLegend() && client.CanUseFreeDelivery())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(" Безкоштовна доставка доступна!");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n Профіль клієнта не знайдено в системі!");
                Console.WriteLine("Зверніться до адміністратора для створення профілю.");
                Console.ResetColor();
            }

            Console.WriteLine("\n1. Створити нову посилку");
            Console.WriteLine("2. Мої посилки");
            Console.WriteLine("3. Відстежити посилку");
            Console.WriteLine("4. Мій профіль та статус");
            Console.WriteLine("5. Розрахувати вартість доставки");
            Console.WriteLine("0. Вихід");
            Console.Write("\nОберіть опцію: ");

            string choice = Console.ReadLine() ?? "";

            switch (choice)
            {
                case "1": CreateParcelAsClient(); break;
                case "2": ShowMyParcels(); break;
                case "3": TrackParcel(); break;
                case "4": ShowMyProfile(); break;
                case "5": CalculateDeliveryCostForClient(); break;
                case "0": return true;
            }

            return false;
        }

        // ============== ФУНКЦІЇ ДЛЯ КЛІЄНТА ==============

        static void CreateParcelAsClient()
        {
            Console.Clear();
            Console.WriteLine("═══ СТВОРИТИ ПОСИЛКУ ═══\n");

            var sender = _clientService.GetAll().FirstOrDefault(c =>
                c.Email.ToLower() == _currentUsername.ToLower() ||
                c.FullName.ToLower().Contains(_currentUsername.ToLower()));

            if (sender == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" Ваш профіль клієнта не знайдено!");
                Console.WriteLine("Зверніться до адміністратора.");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }

            Console.WriteLine($" Відправник: {sender.FullName}");
            Console.ForegroundColor = GetLoyaltyColor(sender.Status);
            Console.WriteLine($" {GetLoyaltyStatusName(sender.Status)} (знижка {sender.GetDiscount() * 100}%)");
            Console.ResetColor();
            Console.WriteLine();

            // Показуємо список можливих одержувачів
            Console.WriteLine("Доступні одержувачі:");
            var receivers = _clientService.GetAll().Where(c => c.Id != sender.Id).ToList();

            if (receivers.Count == 0)
            {
                Console.WriteLine(" Немає зареєстрованих одержувачів!");
                Console.ReadKey();
                return;
            }

            for (int i = 0; i < Math.Min(receivers.Count, 10); i++)
            {
                Console.WriteLine($"{receivers[i].Id}. {receivers[i].FullName} - {receivers[i].Address}");
            }

            Console.Write("\nID одержувача: ");
            if (!int.TryParse(Console.ReadLine(), out int receiverId))
            {
                Console.WriteLine(" Невірний ID!");
                Console.ReadKey();
                return;
            }

            var receiver = _clientService.GetById(receiverId);
            if (receiver == null)
            {
                Console.WriteLine(" Одержувача не знайдено!");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($" Одержувач: {receiver.FullName}");
            Console.WriteLine();

            Console.WriteLine("Тип посилки:");
            Console.WriteLine("1. Локальна (Україна)");
            Console.WriteLine("2. Міжнародна");
            Console.Write("Оберіть: ");
            ParcelType type = Console.ReadLine() == "2" ? ParcelType.International : ParcelType.Local;

            Console.WriteLine("\nТип вмісту:");
            Console.WriteLine("1. Документи");
            Console.WriteLine("2. Звичайна посилка");
            Console.WriteLine("3. Крихкий вміст (+30 грн)");
            Console.Write("Оберіть: ");
            ContentType contentType = Console.ReadLine() switch
            {
                "3" => ContentType.Fragile,
                "1" => ContentType.Document,
                _ => ContentType.Package
            };

            Console.Write("\nВага (кг): ");
            if (!double.TryParse(Console.ReadLine(), out double weight) || weight <= 0)
            {
                Console.WriteLine(" Невірна вага!");
                Console.ReadKey();
                return;
            }

            Console.Write("Оціночна вартість (грн): ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal declaredValue) || declaredValue < 0)
            {
                Console.WriteLine(" Невірна вартість!");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("\nКур'єрська служба:");
            Console.WriteLine("1. Укрпошта");
            Console.WriteLine("2. Нова Пошта");
            Console.WriteLine("3. Meest Express");
            Console.Write("Оберіть: ");
            CourierService service = Console.ReadLine() switch
            {
                "2" => CourierService.NovaPoshta,
                "3" => CourierService.MeestExpress,
                _ => CourierService.Ukrposhta
            };

            Console.WriteLine("\nТип доставки:");
            Console.WriteLine("1. Відділення (безкоштовно)");
            Console.WriteLine("2. Поштомат (+20 грн)");
            Console.WriteLine("3. Адресна доставка (+50 грн)");
            Console.WriteLine("4. Таксі (+150 грн)");
            Console.Write("Оберіть: ");
            DeliveryType deliveryType = Console.ReadLine() switch
            {
                "2" => DeliveryType.Parcelbox,
                "3" => DeliveryType.Address,
                "4" => DeliveryType.Taxi,
                _ => DeliveryType.Office
            };

            string receiverCountry = "Ukraine";
            if (type == ParcelType.International)
            {
                Console.Write("\nКраїна одержувача: ");
                receiverCountry = Console.ReadLine() ?? "Ukraine";
            }

            Console.Write("\nСтрахувати посилку? (y/n): ");
            bool isInsured = Console.ReadLine()?.ToLower() == "y";
            decimal insuranceValue = 0;

            if (isInsured)
            {
                Console.Write($"Страхова вартість (грн, макс {declaredValue}): ");
                if (decimal.TryParse(Console.ReadLine(), out decimal insValue))
                {
                    insuranceValue = Math.Min(insValue, declaredValue);
                }
            }

            bool useFreeDelivery = false;
            if (sender.IsLegend() && sender.CanUseFreeDelivery())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n Ви - Легенда Доставки!");
                Console.Write("Використати безкоштовну доставку (1 раз на рік)? (y/n): ");
                Console.ResetColor();

                if (Console.ReadLine()?.ToLower() == "y")
                {
                    useFreeDelivery = true;
                    sender.UseFreeDelivery();
                }
            }

            var result = _parcelService.Create(sender.Id, receiverId, type, contentType,
                weight, declaredValue, service, deliveryType, receiverCountry,
                isInsured, insuranceValue, useFreeDelivery);

            if (result.Success)
            {
                var parcel = (Parcel)result.Data;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n Посилку створено!");
                Console.WriteLine($" Трекінг-номер: {parcel.TrackingNumber}");
                Console.WriteLine($"  Орієнтовний термін: {parcel.EstimatedDeliveryDays} днів");
                Console.ResetColor();

                // Розрахунок вартості
                decimal cost = _calculationService.CalculateDeliveryCost(parcel);
                decimal tax = _calculationService.CalculateImportTax(parcel);
                decimal total = cost + tax;

                Console.WriteLine($"\n Вартість доставки: {cost:F2} грн");
                if (tax > 0)
                    Console.WriteLine($" Податок (>150 EUR): {tax:F2} грн");
                Console.WriteLine($" Разом: {total:F2} грн");

                if (sender.CanUseDiscount() && !useFreeDelivery)
                {
                    Console.WriteLine($"\n Ваша знижка {sender.GetDiscount() * 100}% доступна при оплаті!");
                    decimal discounted = _calculationService.ApplyDiscount(total, sender, true);
                    Console.WriteLine($"   Зі знижкою: {discounted:F2} грн (заощадите {total - discounted:F2} грн)");
                }

                if (useFreeDelivery)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n Безкоштовна доставка застосована!");
                    Console.ResetColor();
                }

                if (parcel.IsPriorityProcessing)
                {
                    Console.WriteLine("\nПріоритетна обробка активована!");
                }

                if (parcel.RequiresOperatorConfirmation())
                {
                    Console.WriteLine("\n Потребує підтвердження оператора (вартість >5000 грн)");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n Помилка: {result.Data}");
                Console.ResetColor();
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ShowMyParcels()
        {
            Console.Clear();
            Console.WriteLine("═══ МОЇ ПОСИЛКИ ═══\n");

            var client = _clientService.GetAll().FirstOrDefault(c =>
                c.Email.ToLower() == _currentUsername.ToLower() ||
                c.FullName.ToLower().Contains(_currentUsername.ToLower()));

            if (client == null)
            {
                Console.WriteLine(" Профіль не знайдено!");
                Console.ReadKey();
                return;
            }

            var myParcels = _parcelService.GetAll()
                .Where(p => p.SenderId == client.Id || p.ReceiverId == client.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            Console.WriteLine($" Всього посилок: {myParcels.Count}");
            Console.WriteLine($"    Відправлені: {myParcels.Count(p => p.SenderId == client.Id)}");
            Console.WriteLine($"    Отримані: {myParcels.Count(p => p.ReceiverId == client.Id)}\n");

            if (myParcels.Count == 0)
            {
                Console.WriteLine("У вас поки що немає посилок.");
            }
            else
            {
                foreach (var parcel in myParcels)
                {
                    Console.ForegroundColor = parcel.SenderId == client.Id ? ConsoleColor.Yellow : ConsoleColor.Cyan;
                    Console.WriteLine($"═══ {(parcel.SenderId == client.Id ? "📤 ВІДПРАВЛЕНО" : "📥 ОТРИМАНО")} ═══");
                    Console.ResetColor();
                    DisplayParcelInfo(parcel);
                }
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ShowMyProfile()
        {
            Console.Clear();
            Console.WriteLine("═══ МІЙ ПРОФІЛЬ ═══\n");

            var client = _clientService.GetAll().FirstOrDefault(c =>
                c.Email.ToLower() == _currentUsername.ToLower() ||
                c.FullName.ToLower().Contains(_currentUsername.ToLower()));

            if (client == null)
            {
                Console.WriteLine(" Профіль не знайдено!");
                Console.ReadKey();
                return;
            }

            int parcelCount = _clientService.GetClientParcelCount(client.Id);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"╔══════════════════════════════════════════════╗");
            Console.WriteLine($"║           {client.FullName,-43}              ║");
            Console.WriteLine($"╚══════════════════════════════════════════════╝");
            Console.ResetColor();

            Console.WriteLine($"\nEmail: {client.Email}");
            Console.WriteLine($" Телефон: {client.Phone}");
            Console.WriteLine($" Адреса: {client.Address}");
            Console.WriteLine($" Тип: {(client.Type == ClientType.Individual ? "Фізична особа" : "Організація")}");

            Console.ForegroundColor = GetLoyaltyColor(client.Status);
            Console.WriteLine($"\n СТАТУС ЛОЯЛЬНОСТІ: {GetLoyaltyStatusName(client.Status)}");
            Console.ResetColor();

            Console.WriteLine($"\n Відправлено посилок: {parcelCount}");
            Console.WriteLine($" Поточна знижка: {client.GetDiscount() * 100}%");

            if (client.CanUseDiscount())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(" Знижка доступна (використовується раз на місяць)");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($" Знижка вже використана");
                if (client.LastDiscountUsed.HasValue)
                {
                    var daysLeft = 30 - (DateTime.Now - client.LastDiscountUsed.Value).Days;
                    Console.WriteLine($"   Доступна через {daysLeft} днів");
                }
                Console.ResetColor();
            }

            if (client.IsLegend())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n БОНУСИ ЛЕГЕНДИ ДОСТАВКИ:");
                Console.WriteLine("   Пріоритетна обробка посилок");
                Console.WriteLine($"  Безкоштовна доставка: {(client.CanUseFreeDelivery() ? " Доступна (1 раз на рік)" : " Використана")}");
                Console.WriteLine("   Святкова знижка 35% (20.12 - 07.01)");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"\n ПРОГРЕС ДО НАСТУПНОГО СТАТУСУ:");
                var (nextStatus, needed) = client.Status switch
                {
                    LoyaltyStatus.Beginner => ("Активний клієнт", DeliveryConfiguration.ActiveStatusThreshold - parcelCount),
                    LoyaltyStatus.Active => ("Поштовий профі", DeliveryConfiguration.ProStatusThreshold - parcelCount),
                    LoyaltyStatus.Pro => ("Легенда доставки", DeliveryConfiguration.LegendStatusThreshold - parcelCount),
                    _ => ("Максимальний статус", 0)
                };

                if (needed > 0)
                {
                    Console.WriteLine($" До статусу \"{nextStatus}\": потрібно ще {needed} посилок");

                    int progress = (int)((parcelCount / (double)(parcelCount + needed)) * 20);
                    Console.Write(" [");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(new string('█', progress));
                    Console.ResetColor();
                    Console.Write(new string('░', 20 - progress));
                    Console.WriteLine("]");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(" Максимальний статус досягнуто!");
                    Console.ResetColor();
                }
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void CalculateDeliveryCostForClient()
        {
            Console.Clear();
            Console.WriteLine("═══ РОЗРАХУВАТИ ВАРТІСТЬ ДОСТАВКИ ═══\n");

            var client = _clientService.GetAll().FirstOrDefault(c =>
                c.Email.ToLower() == _currentUsername.ToLower() ||
                c.FullName.ToLower().Contains(_currentUsername.ToLower()));

            if (client == null)
            {
                Console.WriteLine(" Профіль не знайдено!");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Ця функція показує приблизну вартість.");
            Console.WriteLine("Для створення посилки оберіть пункт 1 у меню.\n");

            Console.Write("Вага посилки (кг): ");
            if (!double.TryParse(Console.ReadLine(), out double weight))
            {
                Console.WriteLine("Невірна вага!");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("\n1. Локальна доставка");
            Console.WriteLine("2. Міжнародна доставка");
            bool isInternational = Console.ReadLine() == "2";

            decimal baseCost = isInternational ?
                DeliveryConfiguration.InternationalBaseCost :
                DeliveryConfiguration.LocalBaseCost;

            baseCost += (decimal)weight * DeliveryConfiguration.PricePerKg;

            Console.WriteLine($"\n Базова вартість: {baseCost:F2} грн");
            Console.WriteLine($" Ваша знижка: {client.GetDiscount() * 100}%");

            decimal finalCost = baseCost * (1 - client.GetDiscount());
            Console.WriteLine($" Зі знижкою: {finalCost:F2} грн");
            Console.WriteLine($"\n Заощаджено: {baseCost - finalCost:F2} грн");

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }
        // ============== ІНІЦІАЛІЗАЦІЯ ==============

        private static ApplicationDbContext _dbContext = null!;

        static async System.Threading.Tasks.Task InitializeServicesAsync()
        {
            // Business Logic Layer
            _clientService = new ClientService();
            _parcelService = new ParcelService(_clientService);
            _operatorService = new OperatorService();
            _deliveryPointService = new DeliveryPointService();
            _calculationService = new CalculationService();
            _statisticsService = new StatisticsService(_parcelService, _operatorService);

            // Integration Layer
            _authService = new AuthService("https://localhost:7030");
            _monobankService = new MonobankService();
            _dataStore = new JsonDataStore();

            if (_useDatabaseMode)
            {
                try
                {
                    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseSqlite("Data Source=matepost.db")
                        .Options;

                    _dbContext = new ApplicationDbContext(options);

                    // Перевіряємо чи БД існує та створюємо таблиці якщо потрібно
                    await _dbContext.Database.EnsureCreatedAsync();

                    _databaseService = new DatabaseService(_dbContext);

                    Console.WriteLine(" База даних підключена успішно!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" БД помилка: {ex.Message}");
                    Console.WriteLine("Перемикаємось на JSON режим...");
                    _useDatabaseMode = false;
                }
            }
        }


        // ============== MONOBANK API ==============

        static async System.Threading.Tasks.Task UpdateCurrencyRatesAsync()
        {
            try
            {
                Console.Write("\nОновлення курсів валют...");
                var result = await _monobankService.GetEurToUahRateAsync();

                if (result.Success)
                {
                    _calculationService.SetEuroRate(result.Rate);
                    Console.WriteLine($" EUR/UAH: {result.Rate:F2} грн");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($" {result.Message}");
                    Console.ResetColor();
                    _calculationService.SetEuroRate(result.Rate);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n Помилка: {ex.Message}");
            }
        }

        static async System.Threading.Tasks.Task ShowAllCurrencyRatesAsync()
        {
            Console.Clear();
            Console.WriteLine("═══ КУРСИ ВАЛЮТ MONOBANK ═══\n");
            Console.Write(" Завантаження курсів...");

            var result = await _monobankService.GetAllRatesAsync();

            if (!result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n {result.Message}");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }
            Console.WriteLine($"\n Оновлено: {result.LastUpdate:dd.MM.yyyy HH:mm:ss}\n");

            foreach (var rate in result.Rates)
            {
                Console.WriteLine($"{rate.CurrencyPair}:");
                Console.WriteLine($"  Купівля:  {rate.RateBuy:F2}");
                Console.WriteLine($"  Продаж:   {rate.RateSell:F2}");
                Console.WriteLine();
            }

            await UpdateCurrencyRatesAsync();

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        // ============== РОБОТА З ДАНИМИ ==============

        static async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                if (_useDatabaseMode && _databaseService != null)
                {
                    Console.Write(" Завантаження з БД...");
                    var clients = await _databaseService.GetAllClientsAsync();
                    var parcels = await _databaseService.GetAllParcelsAsync();
                    var operators = await _databaseService.GetAllOperatorsAsync();
                    var points = await _databaseService.GetAllDeliveryPointsAsync();

                    _clientService.LoadClients(clients);
                    _parcelService.LoadParcels(parcels);
                    _operatorService.LoadOperators(operators);
                    _deliveryPointService.LoadDeliveryPoints(points);

                    Console.WriteLine($" ({clients.Count} клієнтів, {parcels.Count} посилок)");
                }
                else
                {
                    Console.Write(" Завантаження з JSON...");
                    _clientService.LoadClients(_dataStore.LoadClients());
                    _parcelService.LoadParcels(_dataStore.LoadParcels());
                    _operatorService.LoadOperators(_dataStore.LoadOperators());
                    _deliveryPointService.LoadDeliveryPoints(_dataStore.LoadDeliveryPoints());

                    Console.WriteLine($" ({_clientService.GetAll().Count} клієнтів)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n Помилка: {ex.Message}");
                if (_useDatabaseMode)
                {
                    Console.WriteLine("Перемикаємось на JSON режим...");
                    _useDatabaseMode = false;
                    await LoadDataAsync();
                }
            }
        }

        static async System.Threading.Tasks.Task SaveDataAsync()
        {
            try
            {
                if (_useDatabaseMode && _databaseService != null && _dbContext != null)
                {
                    Console.Write("Збереження в базу даних...");

                    // ⭐ ВИПРАВЛЕННЯ: Відключаємо відстеження змін перед оновленням
                    foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
                    {
                        entry.State = EntityState.Detached;
                    }

                    // Зберігаємо клієнтів
                    foreach (var client in _clientService.GetAll())
                    {
                        var existing = await _dbContext.Clients
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.Id == client.Id);

                        if (existing == null)
                        {
                            _dbContext.Clients.Add(client);
                        }
                        else
                        {
                            _dbContext.Clients.Update(client);
                        }
                    }

                    // Зберігаємо посилки
                    foreach (var parcel in _parcelService.GetAll())
                    {
                        var existing = await _dbContext.Parcels
                            .AsNoTracking()
                            .Include(p => p.StatusHistory)
                            .Include(p => p.Notifications)
                            .FirstOrDefaultAsync(p => p.TrackingNumber == parcel.TrackingNumber);

                        if (existing == null)
                        {
                            _dbContext.Parcels.Add(parcel);
                        }
                        else
                        {
                            // Видаляємо старі записи історії та повідомлень
                            var oldStatuses = await _dbContext.StatusChanges
                                .Where(s => EF.Property<string>(s, "ParcelTrackingNumber") == parcel.TrackingNumber)
                                .ToListAsync();
                            _dbContext.StatusChanges.RemoveRange(oldStatuses);

                            var oldNotifications = await _dbContext.Notifications
                                .Where(n => EF.Property<string>(n, "ParcelTrackingNumber") == parcel.TrackingNumber)
                                .ToListAsync();
                            _dbContext.Notifications.RemoveRange(oldNotifications);

                            // Оновлюємо посилку
                            _dbContext.Parcels.Update(parcel);
                        }
                    }

                    // Зберігаємо операторів
                    foreach (var op in _operatorService.GetAll())
                    {
                        var existing = await _dbContext.Operators
                            .AsNoTracking()
                            .FirstOrDefaultAsync(o => o.Id == op.Id);

                        if (existing == null)
                        {
                            _dbContext.Operators.Add(op);
                        }
                        else
                        {
                            _dbContext.Operators.Update(op);
                        }
                    }

                    // Зберігаємо точки доставки
                    foreach (var point in _deliveryPointService.GetAll())
                    {
                        var existing = await _dbContext.DeliveryPoints
                            .AsNoTracking()
                            .FirstOrDefaultAsync(d => d.Id == point.Id);

                        if (existing == null)
                        {
                            _dbContext.DeliveryPoints.Add(point);
                        }
                        else
                        {
                            _dbContext.DeliveryPoints.Update(point);
                        }
                    }

                    await _dbContext.SaveChangesAsync();
                    Console.WriteLine(" ✓");
                }
                else
                {
                    Console.Write(" Збереження в JSON файли...");
                    _dataStore.SaveClients(_clientService.GetAll());
                    _dataStore.SaveParcels(_parcelService.GetAll());
                    _dataStore.SaveOperators(_operatorService.GetAll());
                    _dataStore.SaveDeliveryPoints(_deliveryPointService.GetAll());
                    Console.WriteLine(" ✓");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n Помилка збереження: {ex.Message}");
                Console.WriteLine($"Деталі: {ex.InnerException?.Message}");
            }
        }

        static async System.Threading.Tasks.Task ToggleDataModeAsync()
        {
            Console.Clear();
            Console.WriteLine("═══ ПЕРЕМИКАННЯ РЕЖИМУ ДАНИХ ═══\n");
            Console.WriteLine($"Поточний режим: {(_useDatabaseMode ? "База даних" : "JSON файли")}");
            Console.WriteLine();
            Console.WriteLine("1. Перемкнути на " + (_useDatabaseMode ? "JSON файли" : "Базу даних"));
            Console.WriteLine("2. Міграція: JSON → База даних");
            Console.WriteLine("3. Експорт: База даних → JSON");
            Console.WriteLine("0. Назад");
            Console.Write("\nОберіть опцію: ");

            string choice = Console.ReadLine() ?? "";

            switch (choice)
            {
                case "1":
                    await SaveDataAsync();
                    _useDatabaseMode = !_useDatabaseMode;
                    await LoadDataAsync();
                    Console.WriteLine("\n Режим змінено!");
                    Console.ReadKey();
                    break;

                case "2":
                    await MigrateJsonToDatabaseAsync();
                    break;

                case "3":
                    await ExportDatabaseToJsonAsync();
                    break;
            }
        }

        static async System.Threading.Tasks.Task MigrateJsonToDatabaseAsync()
        {
            Console.WriteLine("\n Міграція JSON → База даних...");

            try
            {
                var clients = _dataStore.LoadClients();
                var parcels = _dataStore.LoadParcels();
                var operators = _dataStore.LoadOperators();
                var points = _dataStore.LoadDeliveryPoints();

                await _databaseService.MigrateFromJsonAsync(clients, parcels, operators, points);

                Console.WriteLine($" Мігровано: {clients.Count} клієнтів, {parcels.Count} посилок");
                Console.WriteLine(" Дані успішно перенесені в базу даних!");

                _useDatabaseMode = true;
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Помилка міграції: {ex.Message}");
            }

            Console.ReadKey();
        }

        static async System.Threading.Tasks.Task ExportDatabaseToJsonAsync()
        {
            Console.WriteLine("\n Експорт База даних → JSON...");

            try
            {
                var clients = await _databaseService.GetAllClientsAsync();
                var parcels = await _databaseService.GetAllParcelsAsync();
                var operators = await _databaseService.GetAllOperatorsAsync();
                var points = await _databaseService.GetAllDeliveryPointsAsync();

                _dataStore.SaveClients(clients);
                _dataStore.SaveParcels(parcels);
                _dataStore.SaveOperators(operators);
                _dataStore.SaveDeliveryPoints(points);

                Console.WriteLine($" Експортовано: {clients.Count} клієнтів, {parcels.Count} посилок");
                Console.WriteLine(" Дані успішно збережені в JSON файли!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Помилка експорту: {ex.Message}");
            }

            Console.ReadKey();
        }

        // ============== АУТЕНТИФІКАЦІЯ ==============

        static bool AuthenticateUser()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("╔══════════════════════════════════════╗");
                Console.WriteLine("║       MATEPOST - ВХІД В СИСТЕМУ      ║");
                Console.WriteLine("╚══════════════════════════════════════╝");
                Console.WriteLine();
                Console.WriteLine("1. Логін");
                Console.WriteLine("0. Вихід");
                Console.Write("\nОберіть опцію: ");

                string choice = Console.ReadLine() ?? "";

                switch (choice)
                {
                    case "1":
                        if (Login())
                            return true;
                        break;
                    case "0":
                        return false;
                }
            }
        }

        static bool Login()
        {
            Console.Clear();
            Console.WriteLine("═══ ЛОГІН ═══\n");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Дефолтний адмін:");
            Console.WriteLine("  Логін: admin");
            Console.WriteLine("  Пароль: Admin_password1");
            Console.ResetColor();
            Console.WriteLine();

            Console.Write("Логін: ");
            string username = Console.ReadLine() ?? "";

            Console.Write("Пароль: ");
            string password = ReadPassword();

            Console.WriteLine();
            Console.Write(" Підключення до Security API...");

            try
            {
                var result = _authService.LoginAsync(username, password).Result;

                if (result.Success)
                {
                    var authData = (AuthData)result.Data;

                    _currentUserToken = authData.Token; 
                    _currentUserRole = authData.Role; 
                    _currentUsername = username;

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n✓ Вхід виконано!");
                    Console.WriteLine($"  Роль: {_currentUserRole}");
                    Console.ResetColor();
                    Console.WriteLine("\nНатисніть будь-яку клавішу...");
                    Console.ReadKey();
                    return true;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n✗ Помилка входу: {result.Data}");
                    Console.ResetColor();
                    Console.ReadKey();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n Помилка з'єднання з Security API!");
                Console.WriteLine($"  {ex.Message}");
                Console.WriteLine("\nПереконайтесь що Security API запущений:");
                Console.WriteLine("  cd Security && dotnet run");
                Console.ResetColor();
                Console.ReadKey();
                return false;
            }
        }

        static string ReadPassword()
        {
            string password = "";
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password.Substring(0, password.Length - 1);
                    Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return password;
        }

        // ============== УПРАВЛІННЯ КОРИСТУВАЧАМИ ==============

        static void UserManagementMenu()
        {
            if (_currentUserRole != "Admin")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n Доступ заборонено! Тільки для адміністраторів.");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }

            while (true)
            {
                Console.Clear();
                Console.WriteLine("═══ УПРАВЛІННЯ КОРИСТУВАЧАМИ ═══\n");
                Console.WriteLine("1. Список користувачів");
                Console.WriteLine("2. Зареєструвати нового користувача");
                Console.WriteLine("3. Змінити роль користувача");
                Console.WriteLine("4. Змінити свій пароль");
                Console.WriteLine("0. Назад");
                Console.Write("\nОберіть опцію: ");

                string choice = Console.ReadLine() ?? "";

                switch (choice)
                {
                    case "1":
                        ListUsers();
                        break;
                    case "2":
                        RegisterNewUser();
                        break;
                    case "3":
                        ChangeUserRole();
                        break;
                    case "4":
                        ChangePassword();
                        break;
                    case "0":
                        return;
                }
            }
        }

        static void ListUsers()
        {
            Console.Clear();
            Console.WriteLine("═══ СПИСОК КОРИСТУВАЧІВ ═══\n");

            try
            {
                var result = _authService.GetUsersAsync(_currentUserToken).Result;

                if (result.Success)
                {
                    var users = (UserInfo[])result.Data;

                    if (users.Length == 0)
                    {
                        Console.WriteLine("Користувачів не знайдено.");
                    }
                    else
                    {
                        Console.WriteLine($"{"Логін",-20} {"Роль",-15}");
                        Console.WriteLine(new string('─', 40));

                        foreach (var user in users)
                        {
                            Console.Write($"{user.username,-20} ");

                            Console.ForegroundColor = user.role switch
                            {
                                "Admin" => ConsoleColor.Red,
                                "Manager" => ConsoleColor.Yellow,
                                "Operator" => ConsoleColor.Cyan,
                                "Client" => ConsoleColor.Green,
                                _ => ConsoleColor.White
                            };
                            Console.WriteLine($"{user.role,-15}");
                            Console.ResetColor();
                        }
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($" Помилка: {result.Data}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($" Помилка: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void RegisterNewUser()
        {
            Console.Clear();
            Console.WriteLine("═══ РЕЄСТРАЦІЯ КОРИСТУВАЧА ═══\n");

            Console.Write("Логін: ");
            string username = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(username))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" Логін не може бути порожнім!");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }

            Console.Write("Пароль: ");
            string password = ReadPassword();

            if (string.IsNullOrWhiteSpace(password))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" Пароль не може бути порожнім!");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }

            Console.WriteLine("\nРоль:");
            Console.WriteLine("1. Admin - повний доступ до системи");
            Console.WriteLine("2. Operator - обробка та зміна статусів посилок");
            Console.WriteLine("3. Client - створення посилок, перегляд профілю");
            Console.Write("Оберіть: ");

            // ⭐ ВИПРАВЛЕННЯ: Тільки 3 ролі
            string role = Console.ReadLine() switch
            {
                "1" => "Admin",
                "2" => "Operator",
                _ => "Client"
            };

            Console.WriteLine();
            Console.WriteLine($" Підсумок:");
            Console.WriteLine($"   Логін: {username}");
            Console.WriteLine($"   Роль: {role}");
            Console.Write("\n Створити користувача? (y/n): ");

            if (Console.ReadLine()?.ToLower() != "y")
            {
                Console.WriteLine("Операцію скасовано.");
                Console.ReadKey();
                return;
            }

            Console.Write("⏳ Створення користувача...");

            try
            {
                var result = _authService.RegisterAsync(_currentUserToken, username, password, role).Result;

                if (result.Success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n {result.Data}");
                    Console.WriteLine($"\n Дані для входу:");
                    Console.WriteLine($"   Логін: {username}");
                    Console.WriteLine($"   Роль: {role}");
                    Console.ResetColor();

                    // Підказка про створення профілю
                    if (role == "Client")
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\n Не забудьте створити профіль клієнта!");
                        Console.WriteLine("   Меню → 1. Управління клієнтами → 1. Додати клієнта");
                        Console.WriteLine($"   Email має співпадати з логіном: {username}");
                        Console.ResetColor();
                    }
                    else if (role == "Operator")
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\n💡 Не забудьте створити профіль оператора!");
                        Console.WriteLine("   Меню → 3. Управління операторами → 1. Додати оператора");
                        Console.WriteLine($"   Ім'я має містити логін: {username}");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n {result.Data}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n Помилка: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ChangeUserRole()
        {
            Console.Clear();
            Console.WriteLine("═══ ЗМІНА РОЛІ КОРИСТУВАЧА ═══\n");

            Console.Write("Логін користувача: ");
            string username = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(username))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" Логін не може бути порожнім!");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }

            Console.WriteLine("\nНова роль:");
            Console.WriteLine("1. Admin - повний доступ");
            Console.WriteLine("2. Operator - обробка посилок");
            Console.WriteLine("3. Client - створення посилок");
            Console.Write("Оберіть: ");

            string role = Console.ReadLine() switch
            {
                "1" => "Admin",
                "2" => "Operator",
                "3" => "Client",
                _ => "Client" 
            };

            Console.WriteLine();
            Console.Write($" Змінити роль користувача '{username}' на '{role}'? (y/n): ");

            if (Console.ReadLine()?.ToLower() != "y")
            {
                Console.WriteLine("Операцію скасовано.");
                Console.ReadKey();
                return;
            }

            Console.Write("⏳ Зміна ролі...");

            try
            {
                var result = _authService.ChangeUserRoleAsync(_currentUserToken, username, role).Result;

                if (result.Success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n {result.Data}");
                    Console.WriteLine($"Користувач '{username}' тепер має роль: {role}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n {result.Data}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n Помилка: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ChangePassword()
        {
            Console.Clear();
            Console.WriteLine("═══ ЗМІНА ПАРОЛЯ ═══\n");

            Console.Write("Старий пароль: ");
            string oldPassword = ReadPassword();

            Console.Write("Новий пароль: ");
            string newPassword = ReadPassword();

            Console.Write("Підтвердіть новий пароль: ");
            string confirmPassword = ReadPassword();

            if (newPassword != confirmPassword)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n Паролі не співпадають!");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }

            Console.WriteLine();
            Console.Write(" Зміна пароля...");

            try
            {
                var result = _authService.ChangePasswordAsync(_currentUserToken, oldPassword, newPassword).Result;

                if (result.Success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n {result.Data}");
                    Console.WriteLine("\nВи будете автоматично вийшли з системи.");
                    Console.WriteLine("Увійдіть знову з новим паролем.");
                    Console.ResetColor();
                    Console.ReadKey();

                    Environment.Exit(0);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n {result.Data}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n Помилка: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        // ============== МЕНЮ КЛІЄНТІВ ==============

        static void ClientMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("═══ УПРАВЛІННЯ КЛІЄНТАМИ ═══\n");
                Console.WriteLine("1. Додати клієнта");
                Console.WriteLine("2. Переглянути всіх клієнтів");
                Console.WriteLine("3. Знайти клієнта");
                Console.WriteLine("4. Оновити дані клієнта");
                Console.WriteLine("5. Видалити клієнта");
                Console.WriteLine("0. Назад");
                Console.Write("\nОберіть опцію: ");

                string choice = Console.ReadLine() ?? "";

                switch (choice)
                {
                    case "1":
                        AddClient();
                        break;
                    case "2":
                        ViewAllClients();
                        break;
                    case "3":
                        SearchClient();
                        break;
                    case "4":
                        UpdateClient();
                        break;
                    case "5":
                        DeleteClient();
                        break;
                    case "0":
                        return;
                }
            }
        }

        static void AddClient()
        {
            Console.Clear();
            Console.WriteLine("═══ ДОДАТИ КЛІЄНТА ═══\n");

            Console.Write("ПІБ: ");
            string fullName = Console.ReadLine() ?? "";

            Console.Write("Телефон: ");
            string phone = Console.ReadLine() ?? "";

            Console.Write("Email: ");
            string email = Console.ReadLine() ?? "";

            Console.Write("Адреса: ");
            string address = Console.ReadLine() ?? "";

            Console.WriteLine("\nТип клієнта:");
            Console.WriteLine("1. Фізична особа");
            Console.WriteLine("2. Організація");
            Console.Write("Оберіть: ");

            ClientType type = Console.ReadLine() == "2" ? ClientType.Organization : ClientType.Individual;

            var result = _clientService.Add(fullName, phone, email, address, type);

            if (result.Success)
            {
                var client = (Client)result.Data;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n Клієнта додано! ID: {client.Id}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n Помилка: {result.Data}");
                Console.ResetColor();
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ViewAllClients()
        {
            Console.Clear();
            Console.WriteLine("═══ ВСІ КЛІЄНТИ ═══\n");

            var clients = _clientService.GetAll();

            if (clients.Count == 0)
            {
                Console.WriteLine("Клієнтів не знайдено.");
            }
            else
            {
                foreach (var client in clients)
                {
                    int parcelCount = _clientService.GetClientParcelCount(client.Id);

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"╔══════════════════════════════════════════════╗");
                    Console.WriteLine($"║   ID: {client.Id,-5} | {client.FullName,-35} ║");
                    Console.WriteLine($"╚══════════════════════════════════════════════╝");
                    Console.ResetColor();

                    Console.WriteLine($"  Телефон: {client.Phone}");
                    Console.WriteLine($"  Email: {client.Email}");
                    Console.WriteLine($"  Адреса: {client.Address}");
                    Console.WriteLine($"  Тип: {(client.Type == ClientType.Individual ? "Фізична особа" : "Організація")}");

                    Console.ForegroundColor = GetLoyaltyColor(client.Status);
                    Console.WriteLine($"   ⭐ Статус: {GetLoyaltyStatusName(client.Status)}");
                    Console.ResetColor();

                    Console.WriteLine($"  Посилок відправлено: {parcelCount}");
                    Console.WriteLine($"  Знижка: {client.GetDiscount() * 100}%");
                    Console.WriteLine($"  Знижка доступна: {(client.CanUseDiscount() ? "Так" : "Ні")}");

                    if (client.IsLegend())
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($" Безкоштовна доставка: {(client.CanUseFreeDelivery() ? "Доступна" : "Використана")}");
                        Console.ResetColor();
                    }

                    Console.WriteLine();
                }
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static ConsoleColor GetLoyaltyColor(LoyaltyStatus status)
        {
            return status switch
            {
                LoyaltyStatus.Beginner => ConsoleColor.Gray,
                LoyaltyStatus.Active => ConsoleColor.Green,
                LoyaltyStatus.Pro => ConsoleColor.Blue,
                LoyaltyStatus.Legend => ConsoleColor.Yellow,
                _ => ConsoleColor.White
            };
        }

        static string GetLoyaltyStatusName(LoyaltyStatus status)
        {
            return status switch
            {
                LoyaltyStatus.Beginner => "Початківець",
                LoyaltyStatus.Active => "Активний клієнт",
                LoyaltyStatus.Pro => "Поштовий профі",
                LoyaltyStatus.Legend => "Легенда доставки",
                _ => "Невідомо"
            };
        }

        static void SearchClient()
        {
            Console.Clear();
            Console.WriteLine("═══ ПОШУК КЛІЄНТА ═══\n");
            Console.Write("Введіть пошуковий запит (ПІБ/телефон/email/адреса): ");
            string query = Console.ReadLine() ?? "";

            var clients = _clientService.Search(query);

            Console.WriteLine($"\n Знайдено клієнтів: {clients.Count}\n");

            if (clients.Count == 0)
            {
                Console.WriteLine("Нічого не знайдено.");
            }
            else
            {
                foreach (var client in clients)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"ID: {client.Id} | {client.FullName}");
                    Console.ResetColor();
                    Console.WriteLine($"  {client.Phone} | {client.Email}");
                    Console.WriteLine($"  {GetLoyaltyStatusName(client.Status)}");
                    Console.WriteLine();
                }
            }

            Console.WriteLine("Натисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void UpdateClient()
        {
            Console.Clear();
            Console.WriteLine("═══ ОНОВИТИ ДАНІ КЛІЄНТА ═══\n");
            Console.Write("Введіть ID клієнта: ");

            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine(" Невірний ID!");
                Console.ReadKey();
                return;
            }

            var client = _clientService.GetById(id);
            if (client == null)
            {
                Console.WriteLine(" Клієнта не знайдено!");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nПоточні дані:");
            Console.WriteLine($"ПІБ: {client.FullName}");
            Console.WriteLine($"Телефон: {client.Phone}");
            Console.WriteLine($"Email: {client.Email}");
            Console.WriteLine($"Адреса: {client.Address}");

            Console.Write("\nНове ПІБ (Enter - залишити без змін): ");
            string newName = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newName))
                client.FullName = newName;

            Console.Write("Новий телефон (Enter - залишити без змін): ");
            string newPhone = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newPhone))
                client.Phone = newPhone;

            Console.Write("Новий email (Enter - залишити без змін): ");
            string newEmail = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newEmail))
                client.Email = newEmail;

            Console.Write("Нова адреса (Enter - залишити без змін): ");
            string newAddress = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newAddress))
                client.Address = newAddress;

            var result = _clientService.Update(client);

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n Дані клієнта оновлено!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n Помилка: {result.Data}");
                Console.ResetColor();
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void DeleteClient()
        {
            Console.Clear();
            Console.WriteLine("═══ ВИДАЛИТИ КЛІЄНТА ═══\n");
            Console.Write("Введіть ID клієнта для видалення: ");

            if (int.TryParse(Console.ReadLine(), out int id))
            {
                var client = _clientService.GetById(id);
                if (client != null)
                {
                    Console.WriteLine($"\nКлієнт: {client.FullName}");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("\n Ви впевнені? (y/n): ");
                    Console.ResetColor();

                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        var result = _clientService.Delete(id);

                        if (result.Success)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(" Клієнта видалено!");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(" Помилка при видаленні!");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Операцію скасовано.");
                    }
                }
                else
                {
                    Console.WriteLine(" Клієнта не знайдено!");
                }
            }
            else
            {
                Console.WriteLine(" Невірний ID!");
            }

            Console.ReadKey();
        }

        // ============== МЕНЮ ПОСИЛОК ==============

        static void ParcelMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("═══ УПРАВЛІННЯ ПОСИЛКАМИ ═══\n");
                Console.WriteLine("1. Створити посилку");
                Console.WriteLine("2. Переглянути всі посилки");
                Console.WriteLine("3. Розширений пошук");
                Console.WriteLine("4. Відстежити посилку");
                Console.WriteLine("5. Змінити статус");
                Console.WriteLine("6. Розрахувати вартість");
                Console.WriteLine("7. Симулювати затримку");
                Console.WriteLine("0. Назад");
                Console.Write("\nОберіть опцію: ");

                string choice = Console.ReadLine() ?? "";

                switch (choice)
                {
                    case "1":
                        CreateParcel();
                        break;
                    case "2":
                        ViewAllParcels();
                        break;
                    case "3":
                        AdvancedSearchParcels();
                        break;
                    case "4":
                        TrackParcel();
                        break;
                    case "5":
                        ChangeParcelStatus();
                        break;
                    case "6":
                        CalculateDeliveryCost();
                        break;
                    case "7":
                        SimulateDelay();
                        break;
                    case "0":
                        return;
                }
            }
        }

        static void CreateParcel()
        {
            Console.Clear();
            Console.WriteLine("═══ СТВОРИТИ ПОСИЛКУ ═══\n");

            Console.Write("ID відправника: ");
            if (!int.TryParse(Console.ReadLine(), out int senderId)) return;

            var sender = _clientService.GetById(senderId);
            if (sender == null)
            {
                Console.WriteLine("Відправника не знайдено!");
                Console.ReadKey();
                return;
            }

            Console.Write("ID одержувача: ");
            if (!int.TryParse(Console.ReadLine(), out int receiverId)) return;

            Console.WriteLine("\nТип посилки:");
            Console.WriteLine("1. Локальна");
            Console.WriteLine("2. Міжнародна");
            ParcelType type = Console.ReadLine() == "2" ? ParcelType.International : ParcelType.Local;

            Console.WriteLine("\nТип вмісту:");
            Console.WriteLine("1. Документ");
            Console.WriteLine("2. Посилка");
            Console.WriteLine("3. Крихке");
            Console.Write("Оберіть: ");
            ContentType contentType = Console.ReadLine() switch
            {
                "3" => ContentType.Fragile,
                "2" => ContentType.Package,
                _ => ContentType.Document
            };

            Console.Write("\nВага (кг): ");
            if (!double.TryParse(Console.ReadLine(), out double weight)) return;

            Console.Write("Оціночна вартість (грн): ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal declaredValue)) return;

            Console.WriteLine("\nКур'єрська служба:");
            Console.WriteLine("1. Укрпошта");
            Console.WriteLine("2. Нова Пошта");
            Console.WriteLine("3. Meest Express");
            Console.Write("Оберіть: ");
            CourierService service = Console.ReadLine() switch
            {
                "2" => CourierService.NovaPoshta,
                "3" => CourierService.MeestExpress,
                _ => CourierService.Ukrposhta
            };

            Console.WriteLine("\nТип доставки:");
            Console.WriteLine("1. Відділення");
            Console.WriteLine("2. Поштомат");
            Console.WriteLine("3. Адресна доставка");
            Console.WriteLine("4. Таксі");
            Console.Write("Оберіть: ");
            DeliveryType deliveryType = Console.ReadLine() switch
            {
                "2" => DeliveryType.Parcelbox,
                "3" => DeliveryType.Address,
                "4" => DeliveryType.Taxi,
                _ => DeliveryType.Office
            };

            string receiverCountry = "Ukraine";
            if (type == ParcelType.International)
            {
                Console.Write("\nКраїна одержувача: ");
                receiverCountry = Console.ReadLine() ?? "Ukraine";
            }

            Console.Write("\nСтрахувати посилку? (y/n): ");
            bool isInsured = Console.ReadLine()?.ToLower() == "y";
            decimal insuranceValue = 0;

            if (isInsured)
            {
                Console.Write("Страхова вартість (грн): ");
                decimal.TryParse(Console.ReadLine(), out insuranceValue);
            }

            bool useFreeDelivery = false;
            if (sender.IsLegend() && sender.CanUseFreeDelivery())
            {
                Console.WriteLine("\n Ви - Легенда Доставки!");
                Console.Write("Бажаєте використати безкоштовну доставку (1 раз на рік)? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    useFreeDelivery = true;
                    sender.UseFreeDelivery();
                }
            }

            var result = _parcelService.Create(senderId, receiverId, type, contentType,
                weight, declaredValue, service, deliveryType, receiverCountry, isInsured, insuranceValue, useFreeDelivery);

            if (result.Success)
            {
                var parcel = (Parcel)result.Data;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n Посилку створено!");
                Console.WriteLine($"Трекінг-номер: {parcel.TrackingNumber}");
                Console.WriteLine($"Орієнтовний термін: {parcel.EstimatedDeliveryDays} днів");
                Console.ResetColor();

                if (parcel.IsPriorityProcessing)
                    Console.WriteLine(" Пріоритетна обробка активована");

                if (parcel.RequiresOperatorConfirmation())
                    Console.WriteLine("  Потребує підтвердження оператора (>5000 грн)");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n Помилка: {result.Data}");
                Console.ResetColor();
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ViewAllParcels()
        {
            Console.Clear();
            Console.WriteLine("═══ ВСІ ПОСИЛКИ ═══\n");

            var parcels = _parcelService.GetAll();

            if (parcels.Count == 0)
            {
                Console.WriteLine("Посилок не знайдено.");
            }
            else
            {
                foreach (var parcel in parcels)
                {
                    DisplayParcelInfo(parcel);
                }
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void DisplayParcelInfo(Parcel parcel)
        {
            var sender = _clientService.GetById(parcel.SenderId);
            var receiver = _clientService.GetById(parcel.ReceiverId);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"╔══════════════════════════════════════════════╗");
            Console.WriteLine($"║          {parcel.TrackingNumber,-39}         ║");
            Console.WriteLine($"╚══════════════════════════════════════════════╝");
            Console.ResetColor();

            Console.WriteLine($"  Відправник: {sender?.FullName ?? "N/A"}");
            Console.WriteLine($"  Одержувач: {receiver?.FullName ?? "N/A"}");

            Console.ForegroundColor = GetStatusColor(parcel.CurrentStatus);
            Console.WriteLine($"  Статус: {GetStatusName(parcel.CurrentStatus)}");
            Console.ResetColor();

            Console.WriteLine($"  Тип: {(parcel.Type == ParcelType.Local ? "Локальна" : "Міжнародна")}");
            Console.WriteLine($"  Вага: {parcel.Weight} кг | 💰 Вартість: {parcel.DeclaredValue} грн");
            Console.WriteLine($"  {parcel.Service} | 📍 {GetDeliveryTypeName(parcel.DeliveryType)}");
            Console.WriteLine($"  {parcel.CreatedAt:dd.MM.yyyy HH:mm} | ⏱️  {parcel.EstimatedDeliveryDays} днів");

            if (parcel.IsPriorityProcessing)
                Console.WriteLine("   ⭐ Пріоритетна обробка");
            if (parcel.IsFreeDelivery)
                Console.WriteLine("  Безкоштовна доставка");
            if (parcel.IsInsured)
                Console.WriteLine($"  Застраховано: {parcel.InsuranceValue} грн");

            Console.WriteLine();
        }

        static ConsoleColor GetStatusColor(ParcelStatus status)
        {
            return status switch
            {
                ParcelStatus.AwaitingShipment => ConsoleColor.Gray,
                ParcelStatus.AcceptedByOperator => ConsoleColor.Cyan,
                ParcelStatus.InTransit => ConsoleColor.Blue,
                ParcelStatus.AtWarehouse => ConsoleColor.Magenta,
                ParcelStatus.Delivered => ConsoleColor.Green,
                ParcelStatus.Lost => ConsoleColor.Red,
                _ => ConsoleColor.White
            };
        }

        static string GetStatusName(ParcelStatus status)
        {
            return status switch
            {
                ParcelStatus.AwaitingShipment => "Очікує відправки",
                ParcelStatus.AcceptedByOperator => "Прийнято оператором",
                ParcelStatus.InTransit => "В дорозі",
                ParcelStatus.AtWarehouse => "На складі",
                ParcelStatus.Delivered => "Доставлено",
                ParcelStatus.Lost => "Втрачено",
                _ => "Невідомо"
            };
        }

        static string GetDeliveryTypeName(DeliveryType type)
        {
            return type switch
            {
                DeliveryType.Office => "Відділення",
                DeliveryType.Parcelbox => "Поштомат",
                DeliveryType.Address => "Адресна",
                DeliveryType.Taxi => "Таксі",
                _ => "Невідомо"
            };
        }

        static void AdvancedSearchParcels()
        {
            Console.Clear();
            Console.WriteLine("═══ РОЗШИРЕНИЙ ПОШУК ПОСИЛОК ═══\n");
            Console.WriteLine("1. Пошук за трекінг-номером");
            Console.WriteLine("2. Пошук за клієнтом");
            Console.WriteLine("3. Пошук за статусом");
            Console.WriteLine("4. Пошук за датою");
            Console.Write("\nОберіть: ");

            string choice = Console.ReadLine() ?? "";
            List<Parcel> results = new List<Parcel>();

            switch (choice)
            {
                case "1":
                    Console.Write("Трекінг-номер: ");
                    results = _parcelService.Search(query: Console.ReadLine());
                    break;

                case "2":
                    Console.Write("ПІБ/телефон клієнта: ");
                    results = _parcelService.Search(query: Console.ReadLine());
                    break;

                case "3":
                    Console.WriteLine("\n1. Очікує відправки");
                    Console.WriteLine("2. Прийнято");
                    Console.WriteLine("3. В дорозі");
                    Console.WriteLine("4. На складі");
                    Console.WriteLine("5. Доставлено");
                    Console.WriteLine("6. Втрачено");
                    Console.Write("Оберіть: ");

                    ParcelStatus? status = Console.ReadLine() switch
                    {
                        "1" => ParcelStatus.AwaitingShipment,
                        "2" => ParcelStatus.AcceptedByOperator,
                        "3" => ParcelStatus.InTransit,
                        "4" => ParcelStatus.AtWarehouse,
                        "5" => ParcelStatus.Delivered,
                        "6" => ParcelStatus.Lost,
                        _ => null
                    };

                    if (status.HasValue)
                        results = _parcelService.Search(status: status.Value);
                    break;

                case "4":
                    Console.Write("Дата (dd.MM.yyyy): ");
                    if (DateTime.TryParse(Console.ReadLine(), out DateTime date))
                        results = _parcelService.Search(date: date);
                    break;
            }

            Console.WriteLine($"\n═══ РЕЗУЛЬТАТИ ({results.Count}) ═══\n");

            foreach (var parcel in results)
            {
                DisplayParcelInfo(parcel);
            }

            Console.WriteLine("Натисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void TrackParcel()
        {
            Console.Clear();
            Console.Write("Трекінг-номер: ");
            string trackingNumber = Console.ReadLine();

            var parcel = _parcelService.GetByTrackingNumber(trackingNumber);

            if (parcel == null)
            {
                Console.WriteLine("Посилку не знайдено!");
            }
            else
            {
                DisplayParcelInfo(parcel);

                Console.WriteLine("--- ІСТОРІЯ СТАТУСІВ ---");
                foreach (var status in parcel.StatusHistory)
                {
                    Console.WriteLine($"{status.Timestamp:dd.MM.yyyy HH:mm} - {GetStatusName(status.Status)}");
                    if (!string.IsNullOrEmpty(status.Note))
                        Console.WriteLine($" {status.Note}");
                }

                if (parcel.Notifications.Any())
                {
                    Console.WriteLine("\n--- ПОВІДОМЛЕННЯ ---");
                    foreach (var notif in parcel.Notifications)
                    {
                        Console.WriteLine($"{notif.Timestamp:dd.MM.yyyy HH:mm} - {GetStatusName(notif.Status)}");
                        if (notif.DelayReason.HasValue)
                            Console.WriteLine($"  Затримка: {notif.DelayReason} (+{notif.DelayDays} днів)");
                    }
                }
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ChangeParcelStatus()
        {
            Console.Clear();
            Console.Write("Трекінг-номер: ");
            string trackingNumber = Console.ReadLine() ?? "";

            var parcel = _parcelService.GetByTrackingNumber(trackingNumber);
            if (parcel == null)
            {
                Console.WriteLine("Посилку не знайдено!");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nПоточний статус: {GetStatusName(parcel.CurrentStatus)}");
            Console.WriteLine("\nНовий статус:");
            Console.WriteLine("1. Очікує відправки");
            Console.WriteLine("2. Прийнято оператором");
            Console.WriteLine("3. В дорозі");
            Console.WriteLine("4. На складі");
            Console.WriteLine("5. Доставлено");
            Console.WriteLine("6. Втрачено");
            Console.Write("Оберіть: ");

            ParcelStatus newStatus = Console.ReadLine() switch
            {
                "2" => ParcelStatus.AcceptedByOperator,
                "3" => ParcelStatus.InTransit,
                "4" => ParcelStatus.AtWarehouse,
                "5" => ParcelStatus.Delivered,
                "6" => ParcelStatus.Lost,
                _ => ParcelStatus.AwaitingShipment
            };

            Console.Write("Примітка (Enter - пропустити): ");
            string note = Console.ReadLine();

            int? operatorId = null;
            if (newStatus == ParcelStatus.AcceptedByOperator && parcel.RequiresOperatorConfirmation())
            {
                Console.Write("\nID оператора (обов'язково для >5000 грн): ");
                if (int.TryParse(Console.ReadLine(), out int opId))
                {
                    var op = _operatorService.GetById(opId);
                    if (op != null)
                    {
                        operatorId = opId;
                        op.IncrementProcessed();
                    }
                    else
                    {
                        Console.WriteLine("Оператора не знайдено!");
                        Console.ReadKey();
                        return;
                    }
                }
            }

            var result = _parcelService.ChangeStatus(trackingNumber, newStatus, note, operatorId);

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ Статус оновлено!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n✗ {result.Data}");
                Console.ResetColor();
            }

            Console.ReadKey();
        }

        static void CalculateDeliveryCost()
        {
            Console.Clear();
            Console.Write("Трекінг-номер: ");
            string trackingNumber = Console.ReadLine();

            var parcel = _parcelService.GetByTrackingNumber(trackingNumber);
            if (parcel == null)
            {
                Console.WriteLine("Посилку не знайдено!");
                Console.ReadKey();
                return;
            }

            var sender = _clientService.GetById(parcel.SenderId);

            if (parcel.IsFreeDelivery)
            {
                Console.WriteLine("\n Безкоштовна доставка застосована!");
                Console.WriteLine("Вартість: 0 грн");
                Console.ReadKey();
                return;
            }

            decimal deliveryCost = _calculationService.CalculateDeliveryCost(parcel);
            decimal tax = _calculationService.CalculateImportTax(parcel);
            decimal total = deliveryCost + tax;

            Console.WriteLine("\n═══ РОЗРАХУНОК ВАРТОСТІ ═══");
            Console.WriteLine($"Вартість доставки: {deliveryCost:F2} грн");
            if (tax > 0)
                Console.WriteLine($"Податок (>150 EUR): {tax:F2} грн");
            Console.WriteLine($"Разом: {total:F2} грн");

            Console.WriteLine($"\n--- ІНФОРМАЦІЯ ПРО КЛІЄНТА ---");
            Console.WriteLine($"Статус: {GetLoyaltyStatusName(sender.Status)}");
            Console.WriteLine($"Доступна знижка: {sender.GetDiscount() * 100}%");

            if (sender.IsLegend() && IsHolidayPeriod())
            {
                Console.WriteLine(" Святковий період - знижка 35% для Легенди!");
            }

            Console.WriteLine($"Можна використати знижку: {(sender.CanUseDiscount() ? "Так" : "Ні (використана цього місяця)")}");

            bool useDiscount = false;
            if (sender.CanUseDiscount())
            {
                Console.Write("\nВикористати знижку? (y/n): ");
                useDiscount = Console.ReadLine()?.ToLower() == "y";
            }

            decimal finalPrice = _calculationService.CalculateFinalPrice(parcel, sender, useDiscount);

            if (useDiscount && sender.CanUseDiscount())
            {
                sender.UseDiscount();
                Console.WriteLine($"\nВартість зі знижкою: {finalPrice:F2} грн");
                Console.WriteLine($"Заощаджено: {total - finalPrice:F2} грн");
                Console.WriteLine(" Знижка застосована!");
            }
            else
            {
                Console.WriteLine($"\nКінцева вартість: {finalPrice:F2} грн");
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static bool IsHolidayPeriod()
        {
            var today = DateTime.Now;
            return (today.Month == DeliveryConfiguration.HolidayStartMonth &&
                    today.Day >= DeliveryConfiguration.HolidayStartDay) ||
                   (today.Month == DeliveryConfiguration.HolidayEndMonth &&
                    today.Day <= DeliveryConfiguration.HolidayEndDay);
        }

        static void SimulateDelay()
        {
            Console.Clear();
            Console.Write("Трекінг-номер: ");
            string trackingNumber = Console.ReadLine();

            var result = _parcelService.SimulateDelay(trackingNumber);

            if (result.Success)
            {
                var data = ((bool HasDelay, DelayReason? Reason, int? DelayDays, int? NewEstimate))result.Data;

                if (data.HasDelay)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n  Виникла затримка!");
                    Console.WriteLine($"Причина: {GetDelayReasonName(data.Reason.Value)}");
                    Console.WriteLine($"Затримка: +{data.DelayDays} днів");
                    Console.WriteLine($"Новий термін: {data.NewEstimate} днів");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n Затримок немає!");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n {result.Data}");
                Console.ResetColor();
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static string GetDelayReasonName(DelayReason reason)
        {
            return reason switch
            {
                DelayReason.Holiday => "Святковий день",
                DelayReason.Accident => "Аварія на транспорті",
                DelayReason.BorderDelay => "Затримка на кордоні",
                DelayReason.TransportBreakdown => "Поломка транспорту",
                DelayReason.BadWeather => "Погані погодні умови",
                DelayReason.CustomsInspection => "Митна перевірка",
                _ => "Невідома причина"
            };
        }

        // ============== МЕНЮ ОПЕРАТОРІВ ==============

        static void OperatorMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("═══ УПРАВЛІННЯ ОПЕРАТОРАМИ ═══\n");
                Console.WriteLine("1. Додати оператора");
                Console.WriteLine("2. Переглянути всіх операторів");
                Console.WriteLine("3. Статистика оператора");
                Console.WriteLine("4. Видалити оператора");
                Console.WriteLine("0. Назад");
                Console.Write("\nОберіть опцію: ");

                string choice = Console.ReadLine() ?? "";

                switch (choice)
                {
                    case "1":
                        AddOperator();
                        break;
                    case "2":
                        ViewAllOperators();
                        break;
                    case "3":
                        ViewOperatorStatistics();
                        break;
                    case "4":
                        DeleteOperator();
                        break;
                    case "0":
                        return;
                }
            }
        }

        static void AddOperator()
        {
            Console.Clear();
            Console.WriteLine("═══ ДОДАТИ ОПЕРАТОРА ═══\n");
            Console.Write("Ім'я оператора: ");
            string name = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(name))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" Ім'я не може бути порожнім!");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }

            var result = _operatorService.Add(name);

            if (result.Success)
            {
                var op = (Operator)result.Data;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n Оператора додано! ID: {op.Id}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n Помилка: {result.Data}");
                Console.ResetColor();
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ViewAllOperators()
        {
            Console.Clear();
            Console.WriteLine("═══ ВСІ ОПЕРАТОРИ ═══\n");

            var operators = _operatorService.GetAll();

            if (operators.Count == 0)
            {
                Console.WriteLine("Операторів не знайдено.");
            }
            else
            {
                foreach (var op in operators)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"╔═══════════════════════════════════════╗");
                    Console.WriteLine($"║      ID: {op.Id,-3} | {op.Name,-28}   ║");
                    Console.WriteLine($"╚═══════════════════════════════════════╝");
                    Console.ResetColor();

                    Console.WriteLine($"    Оброблено посилок: {op.ProcessedParcels}");

                    Console.ForegroundColor = GetEfficiencyColor(op.Efficiency);
                    Console.WriteLine($"    Ефективність: {op.Efficiency:F1}%");
                    Console.ResetColor();

                    Console.WriteLine();
                }
            }

            Console.WriteLine("Натисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static ConsoleColor GetEfficiencyColor(double efficiency)
        {
            return efficiency switch
            {
                >= 90 => ConsoleColor.Green,
                >= 70 => ConsoleColor.Yellow,
                >= 50 => ConsoleColor.DarkYellow,
                _ => ConsoleColor.Red
            };
        }

        static void ViewOperatorStatistics()
        {
            Console.Clear();
            Console.WriteLine("═══ СТАТИСТИКА ОПЕРАТОРА ═══\n");
            Console.Write("ID оператора: ");

            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine(" Невірний ID!");
                Console.ReadKey();
                return;
            }

            var op = _operatorService.GetById(id);
            if (op == null)
            {
                Console.WriteLine(" Оператора не знайдено!");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\n╔═══════════════════════════════════════╗");
            Console.WriteLine($"║              {op.Name,-37}              ║");
            Console.WriteLine($"╚═══════════════════════════════════════╝\n");

            Console.WriteLine($" Всього оброблено посилок: {op.ProcessedParcels}");
            Console.WriteLine($" Ефективність: {op.Efficiency:F1}%");

            // Підрахунок посилок, оброблених цим оператором
            var parcels = _parcelService.GetAll()
                .Where(p => p.CurrentStatus == ParcelStatus.AcceptedByOperator ||
                           p.StatusHistory.Any(s => s.Status == ParcelStatus.AcceptedByOperator))
                .ToList();

            var todayParcels = parcels.Where(p => p.CreatedAt.Date == DateTime.Today).Count();
            var weekParcels = parcels.Where(p => p.CreatedAt >= DateTime.Today.AddDays(-7)).Count();
            var monthParcels = parcels.Where(p => p.CreatedAt >= DateTime.Today.AddMonths(-1)).Count();

            Console.WriteLine($"\n Статистика обробки:");
            Console.WriteLine($"   Сьогодні: {todayParcels}");
            Console.WriteLine($"   За тиждень: {weekParcels}");
            Console.WriteLine($"   За місяць: {monthParcels}");

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void DeleteOperator()
        {
            Console.Clear();
            Console.WriteLine("═══ ВИДАЛИТИ ОПЕРАТОРА ═══\n");
            Console.Write("ID оператора: ");

            if (int.TryParse(Console.ReadLine(), out int id))
            {
                var op = _operatorService.GetById(id);
                if (op != null)
                {
                    Console.WriteLine($"\nОператор: {op.Name}");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("  Ви впевнені? (y/n): ");
                    Console.ResetColor();

                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        var result = _operatorService.Delete(id);

                        if (result.Success)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(" Оператора видалено!");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(" Помилка при видаленні!");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Операцію скасовано.");
                    }
                }
                else
                {
                    Console.WriteLine(" Оператора не знайдено!");
                }
            }
            else
            {
                Console.WriteLine(" Невірний ID!");
            }

            Console.ReadKey();
        }

        // ============== МЕНЮ ТОЧОК ДОСТАВКИ ==============

        static void DeliveryPointMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("═══ УПРАВЛІННЯ ТОЧКАМИ ДОСТАВКИ ═══\n");
                Console.WriteLine("1. Додати точку доставки");
                Console.WriteLine("2. Переглянути всі точки");
                Console.WriteLine("3. Пошук точки за адресою");
                Console.WriteLine("4. Видалити точку");
                Console.WriteLine("0. Назад");
                Console.Write("\nОберіть опцію: ");

                string choice = Console.ReadLine() ?? "";

                switch (choice)
                {
                    case "1":
                        AddDeliveryPoint();
                        break;
                    case "2":
                        ViewAllDeliveryPoints();
                        break;
                    case "3":
                        SearchDeliveryPoint();
                        break;
                    case "4":
                        DeleteDeliveryPoint();
                        break;
                    case "0":
                        return;
                }
            }
        }

        static void AddDeliveryPoint()
        {
            Console.Clear();
            Console.WriteLine("═══ ДОДАТИ ТОЧКУ ДОСТАВКИ ═══\n");

            Console.WriteLine("Тип точки:");
            Console.WriteLine("1. Відділення");
            Console.WriteLine("2. Поштомат");
            Console.WriteLine("3. Адресна доставка");
            Console.WriteLine("4. Таксі (Уклон)");
            Console.Write("Оберіть: ");

            DeliveryType type = Console.ReadLine() switch
            {
                "2" => DeliveryType.Parcelbox,
                "3" => DeliveryType.Address,
                "4" => DeliveryType.Taxi,
                _ => DeliveryType.Office
            };

            Console.Write("\nАдреса: ");
            string address = Console.ReadLine() ?? "";

            Console.Write("Поштовий індекс: ");
            string postalCode = Console.ReadLine() ?? "";

            Console.Write("Назва організації (Enter - пропустити): ");
            string orgName = Console.ReadLine();

            var result = _deliveryPointService.Add(type, address, postalCode,
                string.IsNullOrWhiteSpace(orgName) ? null : orgName);

            if (result.Success)
            {
                var point = (DeliveryPoint)result.Data;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n Точку доставки додано! ID: {point.Id}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n Помилка: {result.Data}");
                Console.ResetColor();
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ViewAllDeliveryPoints()
        {
            Console.Clear();
            Console.WriteLine("═══ ВСІ ТОЧКИ ДОСТАВКИ ═══\n");

            var points = _deliveryPointService.GetAll();

            if (points.Count == 0)
            {
                Console.WriteLine("Точок доставки не знайдено.");
            }
            else
            {
                var groupedPoints = points.GroupBy(p => p.Type);

                foreach (var group in groupedPoints)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n▼ {GetDeliveryTypeName(group.Key)} ({group.Count()})");
                    Console.ResetColor();
                    Console.WriteLine(new string('─', 50));

                    foreach (var point in group)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"ID: {point.Id,-3} | {point.Address}");
                        Console.ResetColor();
                        Console.WriteLine($"  Індекс: {point.PostalCode}");
                        if (!string.IsNullOrEmpty(point.OrganizationName))
                            Console.WriteLine($" {point.OrganizationName}");
                        Console.WriteLine();
                    }
                }
            }

            Console.WriteLine("Натисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void SearchDeliveryPoint()
        {
            Console.Clear();
            Console.WriteLine("═══ ПОШУК ТОЧКИ ДОСТАВКИ ═══\n");
            Console.Write("Введіть адресу або індекс: ");
            string query = Console.ReadLine()?.ToLower() ?? "";

            var points = _deliveryPointService.GetAll()
                .Where(p => p.Address.ToLower().Contains(query) ||
                           p.PostalCode.Contains(query) ||
                           (p.OrganizationName != null && p.OrganizationName.ToLower().Contains(query)))
                .ToList();

            Console.WriteLine($"\n✓ Знайдено точок: {points.Count}\n");

            foreach (var point in points)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"ID: {point.Id} | {GetDeliveryTypeName(point.Type)}");
                Console.ResetColor();
                Console.WriteLine($"    {point.Address}");
                Console.WriteLine($"    {point.PostalCode}");
                if (!string.IsNullOrEmpty(point.OrganizationName))
                    Console.WriteLine($"    {point.OrganizationName}");
                Console.WriteLine();
            }

            Console.WriteLine("Натисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void DeleteDeliveryPoint()
        {
            Console.Clear();
            Console.WriteLine("═══ ВИДАЛИТИ ТОЧКУ ДОСТАВКИ ═══\n");
            Console.Write("ID точки: ");

            if (int.TryParse(Console.ReadLine(), out int id))
            {
                var point = _deliveryPointService.GetById(id);
                if (point != null)
                {
                    Console.WriteLine($"\nТочка: {point.Address}");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("  Ви впевнені? (y/n): ");
                    Console.ResetColor();

                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        var result = _deliveryPointService.Delete(id);

                        if (result.Success)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(" Точку видалено!");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(" Помилка при видаленні!");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Операцію скасовано.");
                    }
                }
                else
                {
                    Console.WriteLine(" Точку не знайдено!");
                }
            }
            else
            {
                Console.WriteLine(" Невірний ID!");
            }

            Console.ReadKey();
        }

        // ============== СТАТИСТИКА ==============

        static void ShowStatistics()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════╗");
            Console.WriteLine("║              СТАТИСТИКА СИСТЕМИ                ║");
            Console.WriteLine("╚════════════════════════════════════════════════╝\n");

            Console.WriteLine("1. Загальна статистика");
            Console.WriteLine("2. Статистика за період");
            Console.WriteLine("3. Статистика по клієнтах");
            Console.WriteLine("4. Статистика по операторах");
            Console.WriteLine("5. Популярні напрямки");
            Console.WriteLine("0. Назад");
            Console.Write("\nОберіть опцію: ");

            string choice = Console.ReadLine() ?? "";

            switch (choice)
            {
                case "1":
                    ShowGeneralStatistics();
                    break;
                case "2":
                    ShowPeriodStatistics();
                    break;
                case "3":
                    ShowClientStatistics();
                    break;
                case "4":
                    ShowOperatorStatistics();
                    break;
                case "5":
                    ShowPopularDestinations();
                    break;
            }
        }

        static void ShowGeneralStatistics()
        {
            Console.Clear();
            Console.WriteLine("═══ ЗАГАЛЬНА СТАТИСТИКА ═══\n");

            var stats = _statisticsService.GetStatistics();

            Console.WriteLine(" ПОСИЛКИ:");
            Console.WriteLine($"   Всього: {stats["TotalParcels"]}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"   Доставлено: {stats["Delivered"]}");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"   В дорозі: {stats["InTransit"]}");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"   Очікують: {stats["AwaitingShipment"]}");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"   Прийнято: {stats["AcceptedByOperator"]}");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"   На складі: {stats["AtWarehouse"]}");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"   Втрачено: {stats["Lost"]}");
            Console.ResetColor();

            if (stats.ContainsKey("AvgLocalDelivery"))
            {
                Console.WriteLine($"\n  СЕРЕДНІЙ ЧАС ДОСТАВКИ:");
                Console.WriteLine($"   Локальна: {stats["AvgLocalDelivery"]:F1} днів");
            }

            if (stats.ContainsKey("AvgInternationalDelivery"))
            {
                Console.WriteLine($"   Міжнародна: {stats["AvgInternationalDelivery"]:F1} днів");
            }

            Console.WriteLine($"\n КЛІЄНТИ: {_clientService.GetAll().Count}");
            Console.WriteLine($" ОПЕРАТОРИ: {_operatorService.GetAll().Count}");
            Console.WriteLine($" ТОЧКИ ДОСТАВКИ: {_deliveryPointService.GetAll().Count}");

            // ТОП-3 операторів
            Console.WriteLine("\n ТОП-3 ОПЕРАТОРІВ:");
            var topOps = (List<Operator>)stats["TopOperators"];
            int position = 1;
            foreach (var op in topOps)
            {
                Console.ForegroundColor = position switch
                {
                    1 => ConsoleColor.Yellow,
                    2 => ConsoleColor.Gray,
                    3 => ConsoleColor.DarkYellow,
                    _ => ConsoleColor.White
                };
                Console.WriteLine($"   {position}. {op.Name}: {op.ProcessedParcels} посилок");
                Console.ResetColor();
                position++;
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ShowPeriodStatistics()
        {
            Console.Clear();
            Console.WriteLine("═══ СТАТИСТИКА ЗА ПЕРІОД ═══\n");

            Console.WriteLine("1. За сьогодні");
            Console.WriteLine("2. За тиждень");
            Console.WriteLine("3. За місяць");
            Console.WriteLine("4. За рік");
            Console.WriteLine("5. Власний період");
            Console.Write("\nОберіть: ");

            string choice = Console.ReadLine() ?? "";
            DateTime? fromDate = null;
            DateTime? toDate = DateTime.Now;

            switch (choice)
            {
                case "1":
                    fromDate = DateTime.Today;
                    break;
                case "2":
                    fromDate = DateTime.Today.AddDays(-7);
                    break;
                case "3":
                    fromDate = DateTime.Today.AddMonths(-1);
                    break;
                case "4":
                    fromDate = DateTime.Today.AddYears(-1);
                    break;
                case "5":
                    Console.Write("\nДата початку (dd.MM.yyyy): ");
                    if (DateTime.TryParse(Console.ReadLine(), out DateTime from))
                        fromDate = from;
                    Console.Write("Дата кінця (dd.MM.yyyy): ");
                    if (DateTime.TryParse(Console.ReadLine(), out DateTime to))
                        toDate = to;
                    break;
            }

            var stats = _statisticsService.GetStatistics(fromDate, toDate);

            Console.WriteLine($"\n Період: {fromDate:dd.MM.yyyy} - {toDate:dd.MM.yyyy}\n");
            Console.WriteLine($"Всього посилок: {stats["TotalParcels"]}");
            Console.WriteLine($"Доставлено: {stats["Delivered"]}");
            Console.WriteLine($"Втрачено: {stats["Lost"]}");

            if (stats.ContainsKey("AvgLocalDelivery"))
                Console.WriteLine($"Середній час (локальна): {stats["AvgLocalDelivery"]:F1} днів");

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ShowClientStatistics()
        {
            Console.Clear();
            Console.WriteLine("═══ СТАТИСТИКА ПО КЛІЄНТАХ ═══\n");

            var clients = _clientService.GetAll();
            var statusGroups = clients.GroupBy(c => c.Status);

            Console.WriteLine(" РОЗПОДІЛ ЗА СТАТУСАМИ:\n");

            foreach (var group in statusGroups.OrderBy(g => g.Key))
            {
                Console.ForegroundColor = GetLoyaltyColor(group.Key);
                Console.WriteLine($"{GetLoyaltyStatusName(group.Key)}: {group.Count()} клієнтів");
                Console.ResetColor();
            }

            Console.WriteLine($"\n Всього клієнтів: {clients.Count}");
            Console.WriteLine($" Організацій: {clients.Count(c => c.Type == ClientType.Organization)}");
            Console.WriteLine($" Фіз. осіб: {clients.Count(c => c.Type == ClientType.Individual)}");

            // ТОП-5 клієнтів за кількістю посилок
            Console.WriteLine("\n ТОП-5 КЛІЄНТІВ:");
            var topClients = clients
                .Select(c => new { Client = c, Count = _clientService.GetClientParcelCount(c.Id) })
                .OrderByDescending(x => x.Count)
                .Take(5);

            int pos = 1;
            foreach (var item in topClients)
            {
                Console.WriteLine($"   {pos}. {item.Client.FullName}: {item.Count} посилок");
                pos++;
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ShowOperatorStatistics()
        {
            Console.Clear();
            Console.WriteLine("═══ СТАТИСТИКА ПО ОПЕРАТОРАХ ═══\n");

            var operators = _operatorService.GetAll();

            if (operators.Count == 0)
            {
                Console.WriteLine("Операторів не знайдено.");
            }
            else
            {
                Console.WriteLine($"{"Оператор",-25} {"Посилок",-12} {"Ефективність",-15}");
                Console.WriteLine(new string('─', 55));

                foreach (var op in operators.OrderByDescending(o => o.ProcessedParcels))
                {
                    Console.Write($"{op.Name,-25} {op.ProcessedParcels,-12} ");

                    Console.ForegroundColor = GetEfficiencyColor(op.Efficiency);
                    Console.WriteLine($"{op.Efficiency:F1}%");
                    Console.ResetColor();
                }

                Console.WriteLine($"\n Всього операторів: {operators.Count}");
                Console.WriteLine($" Оброблено посилок: {operators.Sum(o => o.ProcessedParcels)}");
                Console.WriteLine($" Середня ефективність: {operators.Average(o => o.Efficiency):F1}%");
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ShowPopularDestinations()
        {
            Console.Clear();
            Console.WriteLine("═══ ПОПУЛЯРНІ НАПРЯМКИ ДОСТАВКИ ═══\n");

            var stats = _statisticsService.GetStatistics();
            var destinations = (List<KeyValuePair<string, int>>)stats["PopularDestinations"];

            if (destinations.Count == 0)
            {
                Console.WriteLine("Немає даних про напрямки.");
            }
            else
            {
                Console.WriteLine($"{"#",-5} {"Країна",-25} {"Посилок",-15}");
                Console.WriteLine(new string('─', 50));

                int position = 1;
                foreach (var dest in destinations)
                {
                    Console.ForegroundColor = position switch
                    {
                        1 => ConsoleColor.Yellow,
                        2 => ConsoleColor.Gray,
                        3 => ConsoleColor.DarkYellow,
                        _ => ConsoleColor.White
                    };

                    string medal = position switch
                    {
                        1 => "Перший",
                        2 => "Другий",
                        3 => "Третій",
                        _ => "  "
                    };

                    Console.WriteLine($"{medal} {position,-3} {dest.Key,-25} {dest.Value,-15}");
                    Console.ResetColor();
                    position++;
                }

                Console.WriteLine($"\n Всього напрямків: {destinations.Count}");
                Console.WriteLine($" Всього посилок: {destinations.Sum(d => d.Value)}");
            }

            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }
    }
}