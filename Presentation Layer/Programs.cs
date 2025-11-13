using System;
using System.Collections.Generic;
using System.Linq;
using BusinessLogicLayer.Models;
using BusinessLogicLayer.Services;
using PersistenceLayer;

namespace PresentationLayer
{
    /// <summary>
    /// Головний клас консольного додатку MatePost - система управління поштовими послугами
    /// </summary>
    class Program
    {
        private static ClientService _clientService = null!;
        private static ParcelService _parcelService = null!;
        private static OperatorService _operatorService = null!;

        private static AuthService _authService = null!;
        private static string _currentUserToken = null;
        private static string _currentUserRole = null;
        private static string _currentUsername = null;

        private static DeliveryPointService _deliveryPointService = null!;
        private static CalculationService _calculationService = null!;
        private static StatisticsService _statisticsService = null!;
        private static JsonDataStore _dataStore = null!;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;

            InitializeServices();

            if (!AuthenticateUser())
            {
                Console.WriteLine("Не вдалось увійти в систему.");
                return;
            }
            LoadData();

            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("------------------------------------------");
                Console.WriteLine("|        MATEPOST - Поштова Система      |");
                Console.WriteLine("------------------------------------------");
                // Додаткове меню для адміністратора
                if (_currentUserRole == "Admin")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("A. Управління користувачами (Admin)");
                    Console.ResetColor();
                }
                Console.WriteLine("1. Управління клієнтами");
                Console.WriteLine("2. Управління посилками");
                Console.WriteLine("3. Управління операторами");
                Console.WriteLine("4. Управління точками доставки");
                Console.WriteLine("5. Статистика");
                Console.WriteLine("6. Зберегти дані");
                Console.WriteLine("0. Вихід");
                Console.Write("Оберіть опцію: ");

                string choice = Console.ReadLine() ?? "";

                switch (choice)
                {
                    case "A":
                    case "a":
                        if (_currentUserRole == "Admin")
                            UserManagementMenu();
                        break;
                    case "1":
                        ClientMenu();
                        break;
                    case "2":
                        ParcelMenu();
                        break;
                    case "3":
                        OperatorMenu();
                        break;
                    case "4":
                        DeliveryPointMenu();
                        break;
                    case "5":
                        ShowStatistics();
                        break;
                    case "6":
                        SaveData();
                        Console.WriteLine("Дані збережено!");
                        Console.ReadKey();
                        break;
                    case "0":
                        SaveData();
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Невірний вибір!");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static void InitializeServices()
        {
            _dataStore = new JsonDataStore();
            _clientService = new ClientService();
            _parcelService = new ParcelService(_clientService);
            _operatorService = new OperatorService();
            _deliveryPointService = new DeliveryPointService();
            _calculationService = new CalculationService();
            _statisticsService = new StatisticsService(_parcelService, _operatorService);
            _authService = new AuthService("https://localhost:7030");
        }

        static void LoadData()
        {
            try
            {
                _clientService.LoadClients(_dataStore.LoadClients());
                _parcelService.LoadParcels(_dataStore.LoadParcels());
                _operatorService.LoadOperators(_dataStore.LoadOperators());
                _deliveryPointService.LoadDeliveryPoints(_dataStore.LoadDeliveryPoints());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при завантаженні даних: {ex.Message}");
                Console.WriteLine("Програма продовжить роботу з порожньою базою даних.");
                Console.ReadKey();
            }
        }

        static void SaveData()
        {
            _dataStore.SaveClients(_clientService.GetAll());
            _dataStore.SaveParcels(_parcelService.GetAll());
            _dataStore.SaveOperators(_operatorService.GetAll());
            _dataStore.SaveDeliveryPoints(_deliveryPointService.GetAll());
        }

        static void ClientMenu()
        {
            Console.Clear();
            Console.WriteLine("=== УПРАВЛІННЯ КЛІЄНТАМИ ===");
            Console.WriteLine("1. Додати клієнта");
            Console.WriteLine("2. Переглянути всіх клієнтів");
            Console.WriteLine("3. Знайти клієнта");
            Console.WriteLine("4. Видалити клієнта");
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
                    DeleteClient();
                    break;
            }
        }

        static void AddClient()
        {
            Console.Clear();
            Console.WriteLine("=== ДОДАТИ КЛІЄНТА ===");

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
                Console.WriteLine($"\nКлієнта додано! ID: {client.Id}");
            }
            else
            {
                Console.WriteLine("\nПомилка при додаванні клієнта!");
            }

            Console.ReadKey();
        }

        static void ViewAllClients()
        {
            Console.Clear();
            Console.WriteLine("=== ВСІ КЛІЄНТИ ===\n");

            var clients = _clientService.GetAll();

            if (clients.Count == 0)
            {
                Console.WriteLine("Клієнтів не знайдено.");
            }
            else
            {
                foreach (var client in clients)
                {
                    // Динамічно отримуємо кількість посилок
                    int parcelCount = _clientService.GetClientParcelCount(client.Id);

                    Console.WriteLine($"ID: {client.Id} | {client.FullName}");
                    Console.WriteLine($"   Телефон: {client.Phone} | Email: {client.Email}");
                    Console.WriteLine($"   Адреса: {client.Address}");
                    Console.WriteLine($"   Тип: {client.Type} | Статус: {client.Status}");
                    Console.WriteLine($"   Посилок: {parcelCount} | Знижка: {client.GetDiscount() * 100}%");

                    if (client.IsLegend())
                    {
                        Console.WriteLine($"   Безкоштовна доставка: {(client.CanUseFreeDelivery() ? "Доступна" : "Використана цього року")}");
                    }

                    Console.WriteLine();
                }
            }

            Console.ReadKey();
        }

        static void SearchClient()
        {
            Console.Clear();
            Console.Write("Введіть пошуковий запит: ");
            string query = Console.ReadLine() ?? "";

            var clients = _clientService.Search(query);

            Console.WriteLine($"\nЗнайдено клієнтів: {clients.Count}\n");

            foreach (var client in clients)
            {
                Console.WriteLine($"ID: {client.Id} | {client.FullName} | {client.Phone}");
            }

            Console.ReadKey();
        }

        static void DeleteClient()
        {
            Console.Clear();
            Console.Write("Введіть ID клієнта для видалення: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                var result = _clientService.Delete(id);
                Console.WriteLine(result.Success ? "Клієнта видалено!" : "Клієнта не знайдено!");
            }
            Console.ReadKey();
        }

        static void ParcelMenu()
        {
            Console.Clear();
            Console.WriteLine("=== УПРАВЛІННЯ ПОСИЛКАМИ ===");
            Console.WriteLine("1. Створити посилку");
            Console.WriteLine("2. Переглянути всі посилки");
            Console.WriteLine("3. Розширений пошук посилок");
            Console.WriteLine("4. Відстежити посилку за трекінг-номером");
            Console.WriteLine("5. Змінити статус посилки");
            Console.WriteLine("6. Розрахувати вартість доставки");
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
            }
        }

        static void CreateParcel()
        {
            Console.Clear();
            Console.WriteLine("=== СТВОРИТИ ПОСИЛКУ ===");

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

            Console.Write("\nЧи містить посилка небезпечні матеріали? (y/n): ");
            bool isDangerous = Console.ReadLine()?.ToLower() == "y";

            if (isDangerous && type == ParcelType.International)
            {
                Console.WriteLine("\nМіжнародне відправлення небезпечних вантажів заборонено!");
                Console.ReadKey();
                return;
            }

            bool useFreeDelivery = false;
            if (sender.IsLegend() && sender.CanUseFreeDelivery())
            {
                Console.WriteLine("\nВи - Легенда Доставки!");
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
                Console.WriteLine($"\nПосилку створено!");
                Console.WriteLine($"Трекінг-номер: {parcel.TrackingNumber}");
                Console.WriteLine($"Орієнтовний термін доставки: {parcel.EstimatedDeliveryDays} днів");

                if (parcel.IsPriorityProcessing)
                {
                    Console.WriteLine("Пріоритетна обробка активована (Легенда)");
                }

                if (parcel.IsFreeDelivery)
                {
                    Console.WriteLine("Безкоштовна доставка застосована!");
                }

                if (parcel.RequiresOperatorConfirmation())
                {
                    Console.WriteLine("Увага! Посилка потребує підтвердження оператора (вартість > 5000 грн)");
                }
            }
            else
            {
                Console.WriteLine($"\nПомилка: {result.Data}");
            }

            Console.ReadKey();
        }

        static void ViewAllParcels()
        {
            Console.Clear();
            Console.WriteLine("=== ВСІ ПОСИЛКИ ===\n");

            var parcels = _parcelService.GetAll();

            if (parcels.Count == 0)
            {
                Console.WriteLine("Посилок не знайдено.");
            }
            else
            {
                foreach (var parcel in parcels)
                {
                    var sender = _clientService.GetById(parcel.SenderId);
                    var receiver = _clientService.GetById(parcel.ReceiverId);

                    Console.WriteLine($"Трекінг: {parcel.TrackingNumber}");
                    Console.WriteLine($"   Відправник: {sender?.FullName ?? "N/A"}");
                    Console.WriteLine($"   Одержувач: {receiver?.FullName ?? "N/A"}");
                    Console.WriteLine($"   Статус: {parcel.CurrentStatus}");
                    Console.WriteLine($"   Тип: {parcel.Type} | Вага: {parcel.Weight} кг");
                    Console.WriteLine($"   Вартість: {parcel.DeclaredValue} грн");
                    Console.WriteLine($"   Служба: {parcel.Service} | Доставка: {parcel.DeliveryType}");
                    Console.WriteLine($"   Створено: {parcel.CreatedAt:dd.MM.yyyy HH:mm}");

                    if (parcel.IsPriorityProcessing)
                        Console.WriteLine("   Пріоритетна обробка");
                    if (parcel.IsFreeDelivery)
                        Console.WriteLine("   Безкоштовна доставка");

                    Console.WriteLine();
                }
            }

            Console.ReadKey();
        }

        static void AdvancedSearchParcels()
        {
            Console.Clear();
            Console.WriteLine("=== РОЗШИРЕНИЙ ПОШУК ПОСИЛОК ===");
            Console.WriteLine("1. Пошук за трекінг-номером");
            Console.WriteLine("2. Пошук за клієнтом (ПІБ/телефон)");
            Console.WriteLine("3. Пошук за статусом");
            Console.WriteLine("4. Пошук за датою створення");
            Console.Write("\nОберіть спосіб пошуку: ");

            string choice = Console.ReadLine() ?? "";
            List<Parcel> results = new List<Parcel>();

            switch (choice)
            {
                case "1":
                    Console.Write("Введіть трекінг-номер: ");
                    string tracking = Console.ReadLine() ?? "";
                    results = _parcelService.Search(query: tracking);
                    break;

                case "2":
                    Console.Write("Введіть ПІБ або телефон клієнта: ");
                    string clientQuery = Console.ReadLine() ?? "";
                    results = _parcelService.Search(query: clientQuery);
                    break;

                case "3":
                    Console.WriteLine("\nОберіть статус:");
                    Console.WriteLine("1. Очікує відправки");
                    Console.WriteLine("2. Прийнято оператором");
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
                    Console.Write("Введіть дату (dd.MM.yyyy): ");
                    if (DateTime.TryParse(Console.ReadLine(), out DateTime searchDate))
                    {
                        results = _parcelService.Search(date: searchDate);
                    }
                    break;
            }

            Console.WriteLine($"\n=== РЕЗУЛЬТАТИ ПОШУКУ ({results.Count}) ===\n");

            foreach (var parcel in results)
            {
                var sender = _clientService.GetById(parcel.SenderId);
                var receiver = _clientService.GetById(parcel.ReceiverId);

                Console.WriteLine($"Трекінг: {parcel.TrackingNumber} | Статус: {parcel.CurrentStatus}");
                Console.WriteLine($"   {sender?.FullName} → {receiver?.FullName}");
                Console.WriteLine($"   Дата: {parcel.CreatedAt:dd.MM.yyyy HH:mm}");
                Console.WriteLine();
            }

            Console.ReadKey();
        }

        static void TrackParcel()
        {
            Console.Clear();
            Console.Write("Введіть трекінг-номер: ");
            string trackingNumber = Console.ReadLine();

            var parcel = _parcelService.GetByTrackingNumber(trackingNumber);

            if (parcel == null)
            {
                Console.WriteLine("Посилку не знайдено!");
            }
            else
            {
                Console.WriteLine($"\n=== ПОСИЛКА {parcel.TrackingNumber} ===");
                Console.WriteLine($"Поточний статус: {parcel.CurrentStatus}");
                Console.WriteLine($"Орієнтовний термін: {parcel.EstimatedDeliveryDays} днів");

                Console.WriteLine("\n--- Історія статусів ---");
                foreach (var status in parcel.StatusHistory)
                {
                    Console.WriteLine($"{status.Timestamp:dd.MM.yyyy HH:mm} - {status.Status}");
                    if (!string.IsNullOrEmpty(status.Note))
                        Console.WriteLine($"   Примітка: {status.Note}");
                }

                if (parcel.Notifications.Count > 0)
                {
                    Console.WriteLine("\n--- Повідомлення ---");
                    foreach (var notif in parcel.Notifications)
                    {
                        Console.WriteLine($"{notif.Timestamp:dd.MM.yyyy HH:mm} - {notif.Status}");
                        if (notif.DelayReason.HasValue)
                        {
                            Console.WriteLine($"   Затримка: {notif.DelayReason} (+{notif.DelayDays} днів)");
                        }
                    }
                }
            }

            Console.ReadKey();
        }

        static void ChangeParcelStatus()
        {
            Console.Clear();
            Console.Write("Трекінг-номер посилки: ");
            string trackingNumber = Console.ReadLine() ?? "";

            var parcel = _parcelService.GetByTrackingNumber(trackingNumber);
            if (parcel == null)
            {
                Console.WriteLine("Посилку не знайдено!");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nПоточний статус: {parcel.CurrentStatus}");
            Console.WriteLine("\nОберіть новий статус:");
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

            Console.Write("Примітка (необов'язково): ");
            string note = Console.ReadLine();

            int? operatorId = null;
            if (newStatus == ParcelStatus.AcceptedByOperator && parcel.RequiresOperatorConfirmation())
            {
                Console.WriteLine("\nЦя посилка потребує підтвердження оператора (вартість > 5000 грн)");
                Console.Write("ID оператора: ");
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
                Console.WriteLine("\nСтатус оновлено!");
            }
            else
            {
                Console.WriteLine($"\nПомилка: {result.Data}");
            }

            Console.ReadKey();
        }

        static void CalculateDeliveryCost()
        {
            Console.Clear();
            Console.Write("Трекінг-номер посилки: ");
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
                Console.WriteLine($"\n=== РОЗРАХУНОК ВАРТОСТІ ===");
                Console.WriteLine("Безкоштовна доставка застосована!");
                Console.WriteLine("Вартість: 0 грн");
                Console.ReadKey();
                return;
            }

            decimal deliveryCost = _calculationService.CalculateDeliveryCost(parcel);
            decimal tax = _calculationService.CalculateImportTax(parcel);
            decimal totalWithoutDiscount = deliveryCost + tax;

            Console.WriteLine($"\n=== РОЗРАХУНОК ВАРТОСТІ ===");
            Console.WriteLine($"Вартість доставки: {deliveryCost} грн");
            if (tax > 0)
                Console.WriteLine($"Податок (>150 EUR): {tax} грн");
            Console.WriteLine($"Разом: {totalWithoutDiscount} грн");

            Console.WriteLine($"\n--- ІНФОРМАЦІЯ ПРО КЛІЄНТА ---");
            Console.WriteLine($"Статус: {sender.Status}");
            Console.WriteLine($"Доступна знижка: {sender.GetDiscount() * 100}%");

            if (sender.IsLegend() && IsHolidayPeriod())
            {
                Console.WriteLine("Святковий період - знижка 35% для Легенди!");
            }

            Console.WriteLine($"Можна використати знижку: {(sender.CanUseDiscount() ? "Так" : "Ні (вже використана цього місяця)")}");

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

                Console.WriteLine($"\nВартість зі знижкою: {finalPrice} грн");
                Console.WriteLine($"Заощаджено: {totalWithoutDiscount - finalPrice} грн");
                Console.WriteLine("Знижка застосована!");
            }
            else
            {
                Console.WriteLine($"\nКінцева вартість: {finalPrice} грн");
            }

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
            Console.Write("Трекінг-номер посилки: ");
            string trackingNumber = Console.ReadLine();

            var result = _parcelService.SimulateDelay(trackingNumber);

            if (result.Success)
            {
                var data = ((bool HasDelay, DelayReason? Reason, int? DelayDays, int? NewEstimate))result.Data;

                if (data.HasDelay)
                {
                    Console.WriteLine($"\nВиникла затримка!");
                    Console.WriteLine($"Причина: {data.Reason}");
                    Console.WriteLine($"Затримка: +{data.DelayDays} днів");
                    Console.WriteLine($"Новий термін доставки: {data.NewEstimate} днів");
                }
                else
                {
                    Console.WriteLine("\nЗатримок немає!");
                }
            }
            else
            {
                Console.WriteLine($"\n{result.Data}");
            }

            Console.ReadKey();
        }

        static void OperatorMenu()
        {
            Console.Clear();
            Console.WriteLine("=== УПРАВЛІННЯ ОПЕРАТОРАМИ ===");
            Console.WriteLine("1. Додати оператора");
            Console.WriteLine("2. Переглянути всіх операторів");
            Console.WriteLine("3. Видалити оператора");
            Console.WriteLine("0. Назад");
            Console.Write("\nОберіть опцію: ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Console.Write("Ім'я оператора: ");
                    string name = Console.ReadLine();
                    var result = _operatorService.Add(name);
                    Console.WriteLine(result.Success ? "Оператора додано!" : "Помилка!");
                    Console.ReadKey();
                    break;
                case "2":
                    ViewAllOperators();
                    break;
                case "3":
                    Console.Write("ID оператора: ");
                    if (int.TryParse(Console.ReadLine(), out int id))
                    {
                        var del = _operatorService.Delete(id);
                        Console.WriteLine(del.Success ? "Оператора видалено!" : "Помилка!");
                    }
                    Console.ReadKey();
                    break;
            }
        }

        static void ViewAllOperators()
        {
            Console.Clear();
            Console.WriteLine("=== ВСІ ОПЕРАТОРИ ===\n");

            var operators = _operatorService.GetAll();

            if (operators.Count == 0)
            {
                Console.WriteLine("Операторів не знайдено.");
            }
            else
            {
                foreach (var op in operators)
                {
                    Console.WriteLine($"ID: {op.Id} | {op.Name}");
                    Console.WriteLine($"   Оброблено посилок: {op.ProcessedParcels}");
                    Console.WriteLine($"   Ефективність: {op.Efficiency}%");
                    Console.WriteLine();
                }
            }

            Console.ReadKey();
        }

        static void DeliveryPointMenu()
        {
            Console.Clear();
            Console.WriteLine("=== УПРАВЛІННЯ ТОЧКАМИ ДОСТАВКИ ===");
            Console.WriteLine("1. Додати точку доставки");
            Console.WriteLine("2. Переглянути всі точки");
            Console.WriteLine("3. Видалити точку");
            Console.WriteLine("0. Назад");
            Console.Write("\nОберіть опцію: ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    AddDeliveryPoint();
                    break;
                case "2":
                    ViewAllDeliveryPoints();
                    break;
                case "3":
                    Console.Write("ID точки: ");
                    if (int.TryParse(Console.ReadLine(), out int id))
                    {
                        var result = _deliveryPointService.Delete(id);
                        Console.WriteLine(result.Success ? "Точку видалено!" : "Помилка!");
                    }
                    Console.ReadKey();
                    break;
            }
        }

        static void AddDeliveryPoint()
        {
            Console.Clear();
            Console.WriteLine("=== ДОДАТИ ТОЧКУ ДОСТАВКИ ===");

            Console.WriteLine("\nТип точки:");
            Console.WriteLine("1. Відділення");
            Console.WriteLine("2. Поштомат");
            Console.WriteLine("3. Адресна доставка");
            Console.WriteLine("4. Таксі");
            Console.Write("Оберіть: ");

            DeliveryType type = Console.ReadLine() switch
            {
                "2" => DeliveryType.Parcelbox,
                "3" => DeliveryType.Address,
                "4" => DeliveryType.Taxi,
                _ => DeliveryType.Office
            };

            Console.Write("Адреса: ");
            string address = Console.ReadLine();

            Console.Write("Поштовий індекс: ");
            string postalCode = Console.ReadLine();

            Console.Write("Назва організації (необов'язково): ");
            string orgName = Console.ReadLine();

            var result = _deliveryPointService.Add(type, address, postalCode,
                string.IsNullOrEmpty(orgName) ? null : orgName);

            Console.WriteLine(result.Success ? "\nТочку доставки додано!" : "\nПомилка!");
            Console.ReadKey();
        }

        static void ViewAllDeliveryPoints()
        {
            Console.Clear();
            Console.WriteLine("=== ВСІ ТОЧКИ ДОСТАВКИ ===\n");

            var points = _deliveryPointService.GetAll();

            if (points.Count == 0)
            {
                Console.WriteLine("Точок доставки не знайдено.");
            }
            else
            {
                foreach (var point in points)
                {
                    Console.WriteLine($"ID: {point.Id} | Тип: {point.Type}");
                    Console.WriteLine($"   Адреса: {point.Address}");
                    Console.WriteLine($"   Індекс: {point.PostalCode}");
                    if (!string.IsNullOrEmpty(point.OrganizationName))
                        Console.WriteLine($"   Організація: {point.OrganizationName}");
                    Console.WriteLine();
                }
            }

            Console.ReadKey();
        }

        static void ShowStatistics()
        {
            Console.Clear();
            Console.WriteLine("=== СТАТИСТИКА ===\n");

            var stats = _statisticsService.GetStatistics();

            Console.WriteLine("--- ЗАГАЛЬНА ІНФОРМАЦІЯ ---");
            Console.WriteLine($"Всього посилок: {stats["TotalParcels"]}");
            Console.WriteLine($"Доставлено: {stats["Delivered"]}");
            Console.WriteLine($"Втрачено: {stats["Lost"]}");
            Console.WriteLine($"В дорозі: {stats["InTransit"]}");
            Console.WriteLine($"Очікують відправки: {stats["AwaitingShipment"]}");
            Console.WriteLine($"Прийнято оператором: {stats["AcceptedByOperator"]}");
            Console.WriteLine($"На складі: {stats["AtWarehouse"]}");

            if (stats.ContainsKey("AvgLocalDelivery"))
                Console.WriteLine($"\nСередній час локальної доставки: {stats["AvgLocalDelivery"]:F1} днів");

            if (stats.ContainsKey("AvgInternationalDelivery"))
                Console.WriteLine($"Середній час міжнародної доставки: {stats["AvgInternationalDelivery"]:F1} днів");

            Console.WriteLine("\n--- ТОП-3 ОПЕРАТОРІВ ---");
            var topOps = (List<Operator>)stats["TopOperators"];
            foreach (var op in topOps)
            {
                Console.WriteLine($"{op.Name}: {op.ProcessedParcels} посилок");
            }

            if (stats.ContainsKey("PopularDestinations"))
            {
                Console.WriteLine("\n--- ПОПУЛЯРНІ НАПРЯМКИ ДОСТАВКИ ---");
                var destinations = (List<KeyValuePair<string, int>>)stats["PopularDestinations"];
                int position = 1;
                foreach (var dest in destinations.Take(5))
                {
                    Console.WriteLine($"{position}. {dest.Key}: {dest.Value} посилок");
                    position++;
                }
            }

            Console.ReadKey();
        }
        // ============== МЕТОДИ АУТЕНТИФІКАЦІЇ ==============

/// <summary>
/// Аутентифікація користувача
/// </summary>
static bool AuthenticateUser()
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║   MATEPOST - ВХІД В СИСТЕМУ         ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("1. Логін");
        Console.WriteLine("0. Вихід");
        Console.WriteLine();
        Console.Write("Оберіть опцію: ");

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

/// <summary>
/// Логін користувача
/// </summary>
static bool Login()
{
    Console.Clear();
    Console.WriteLine("═══ ЛОГІН ═══");
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("💡 Дефолтний адмін:");
    Console.WriteLine("   Логін: admin");
    Console.WriteLine("   Пароль: Admin_password1");
    Console.ResetColor();
    Console.WriteLine();
    
    Console.Write("Логін: ");
    string username = Console.ReadLine() ?? "";
    
    Console.Write("Пароль: ");
    string password = ReadPassword();

    Console.WriteLine();
    Console.Write("⏳ Підключення до сервера...");

    try
    {
        var result = _authService.LoginAsync(username, password).Result;

        if (result.Success)
        {
            dynamic data = result.Data;
            _currentUserToken = data.Token;
            _currentUserRole = data.Role;
            _currentUsername = username;

            Console.WriteLine(" ✓");
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
            Console.WriteLine(" ✗");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n✗ Помилка входу");
            Console.WriteLine($"  {result.Data}");
            Console.ResetColor();
            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
            return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(" ✗");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n✗ Помилка з'єднання з сервером!");
        Console.WriteLine($"  {ex.Message}");
        Console.WriteLine("\n💡 Переконайтесь що Security API запущений:");
        Console.WriteLine("   cd Security && dotnet run");
        Console.ResetColor();
        Console.WriteLine("\nНатисніть будь-яку клавішу...");
        Console.ReadKey();
        return false;
    }
}

/// <summary>
/// Читання пароля з прихованням символів
/// </summary>
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

// ============== УПРАВЛІННЯ КОРИСТУВАЧАМИ (тільки для Admin) ==============

/// <summary>
/// Меню управління користувачами
/// </summary>
static void UserManagementMenu()
{
    if (_currentUserRole != "Admin")
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n✗ Доступ заборонено! Тільки для адміністраторів.");
        Console.ResetColor();
        Console.ReadKey();
        return;
    }

    while (true)
    {
        Console.Clear();
        Console.WriteLine("═══ УПРАВЛІННЯ КОРИСТУВАЧАМИ ═══");
        Console.WriteLine();
        Console.WriteLine("1. Список користувачів");
        Console.WriteLine("2. Зареєструвати нового користувача");
        Console.WriteLine("3. Змінити роль користувача");
        Console.WriteLine("4. Змінити свій пароль");
        Console.WriteLine("0. Назад");
        Console.WriteLine();
        Console.Write("Оберіть опцію: ");

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

/// <summary>
/// Список всіх користувачів
/// </summary>
static void ListUsers()
{
    Console.Clear();
    Console.WriteLine("═══ СПИСОК КОРИСТУВАЧІВ ═══");
    Console.WriteLine();

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
                    
                    // Кольорове відображення ролі
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
            Console.WriteLine($"✗ Помилка: {result.Data}");
            Console.ResetColor();
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ Помилка: {ex.Message}");
        Console.ResetColor();
    }

    Console.WriteLine();
    Console.WriteLine("Натисніть будь-яку клавішу...");
    Console.ReadKey();
}

/// <summary>
/// Реєстрація нового користувача (тільки адміном)
/// </summary>
static void RegisterNewUser()
{
    Console.Clear();
    Console.WriteLine("═══ РЕЄСТРАЦІЯ КОРИСТУВАЧА ═══");
    Console.WriteLine();
    
    Console.Write("Логін: ");
    string username = Console.ReadLine() ?? "";
    
    if (string.IsNullOrWhiteSpace(username))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("✗ Логін не може бути порожнім!");
        Console.ResetColor();
        Console.ReadKey();
        return;
    }
    
    Console.Write("Пароль: ");
    string password = ReadPassword();
    
    Console.WriteLine();
    Console.WriteLine("Роль:");
    Console.WriteLine("1. Admin");
    Console.WriteLine("2. Manager");
    Console.WriteLine("3. Operator");
    Console.WriteLine("4. Client");
    Console.Write("Оберіть: ");
    
    string role = Console.ReadLine() switch
    {
        "1" => "Admin",
        "2" => "Manager",
        "3" => "Operator",
        _ => "Client"
    };

    Console.WriteLine();
    Console.Write("⏳ Створення користувача...");

    try
    {
        var result = _authService.RegisterAsync(_currentUserToken, username, password, role).Result;

        if (result.Success)
        {
            Console.WriteLine(" ✓");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ {result.Data}");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine(" ✗");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n✗ {result.Data}");
            Console.ResetColor();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(" ✗");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n✗ Помилка: {ex.Message}");
        Console.ResetColor();
    }

    Console.WriteLine("\nНатисніть будь-яку клавішу...");
    Console.ReadKey();
}

/// <summary>
/// Зміна ролі користувача
/// </summary>
static void ChangeUserRole()
{
    Console.Clear();
    Console.WriteLine("═══ ЗМІНА РОЛІ КОРИСТУВАЧА ═══");
    Console.WriteLine();
    
    Console.Write("Логін користувача: ");
    string username = Console.ReadLine() ?? "";
    
    if (string.IsNullOrWhiteSpace(username))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("✗ Логін не може бути порожнім!");
        Console.ResetColor();
        Console.ReadKey();
        return;
    }
    
    Console.WriteLine();
    Console.WriteLine("Нова роль:");
    Console.WriteLine("1. Admin");
    Console.WriteLine("2. Manager");
    Console.WriteLine("3. Operator");
    Console.WriteLine("4. Client");
    Console.Write("Оберіть: ");
    
    string role = Console.ReadLine() switch
    {
        "1" => "Admin",
        "2" => "Manager",
        "3" => "Operator",
        _ => "Client"
    };

    Console.WriteLine();
    Console.Write("⏳ Зміна ролі...");

    try
    {
        var result = _authService.ChangeUserRoleAsync(_currentUserToken, username, role).Result;
        
        if (result.Success)
        {
            Console.WriteLine(" ✓");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ {result.Data}");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine(" ✗");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n✗ {result.Data}");
            Console.ResetColor();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(" ✗");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n✗ Помилка: {ex.Message}");
        Console.ResetColor();
    }

    Console.WriteLine("\nНатисніть будь-яку клавішу...");
    Console.ReadKey();
}

/// <summary>
/// Зміна власного пароля
/// </summary>
static void ChangePassword()
{
    Console.Clear();
    Console.WriteLine("═══ ЗМІНА ПАРОЛЯ ═══");
    Console.WriteLine();
    
    Console.Write("Старий пароль: ");
    string oldPassword = ReadPassword();
    
    Console.Write("Новий пароль: ");
    string newPassword = ReadPassword();
    
    Console.Write("Підтвердіть новий пароль: ");
    string confirmPassword = ReadPassword();

    if (newPassword != confirmPassword)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n✗ Паролі не співпадають!");
        Console.ResetColor();
        Console.ReadKey();
        return;
    }

    Console.WriteLine();
    Console.Write("⏳ Зміна пароля...");

    try
    {
        var result = _authService.ChangePasswordAsync(_currentUserToken, oldPassword, newPassword).Result;
        
        if (result.Success)
        {
            Console.WriteLine(" ✓");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ {result.Data}");
            Console.WriteLine("\nВи будете автоматично вийшли з системи.");
            Console.WriteLine("Увійдіть знову з новим паролем.");
            Console.ResetColor();
            Console.ReadKey();
            
            // Виходимо з програми після зміни пароля
            Environment.Exit(0);
        }
        else
        {
            Console.WriteLine(" ✗");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n✗ {result.Data}");
            Console.ResetColor();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(" ✗");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n✗ Помилка: {ex.Message}");
        Console.ResetColor();
    }

    Console.WriteLine("\nНатисніть будь-яку клавішу...");
    Console.ReadKey();
}
    }

}