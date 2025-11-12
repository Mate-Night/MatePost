using System;
using System.Collections.Generic;
using System.Linq;
using BusinessLogicLayer.Models;

namespace BusinessLogicLayer.Services
{
    /// <summary>
    /// Сервіс для управління посилками
    /// </summary>
    public class ParcelService
    {
        private List<Parcel> _parcels;
        private ClientService _clientService;
        private Random _random;

        /// <summary>
        /// Конструктор сервісу посилок
        /// </summary>
        public ParcelService(ClientService clientService)
        {
            _parcels = new List<Parcel>();
            _clientService = clientService;
            _random = new Random();
        }

        /// <summary>
        /// Повертає список всіх посилок
        /// </summary>
        public List<Parcel> GetAll() => _parcels;

        /// <summary>
        /// Знаходить посилку за трекінг-номером
        /// </summary>
        public Parcel GetByTrackingNumber(string trackingNumber) =>
            _parcels.FirstOrDefault(p => p.TrackingNumber == trackingNumber);

        /// <summary>
        /// Повертає максимально допустиму вагу для типу доставки
        /// </summary>
        private double GetMaxWeight(DeliveryType deliveryType)
        {
            return deliveryType switch
            {
                DeliveryType.Parcelbox => 30.0,// Поштомат - до 30 кг
                DeliveryType.Office => 100.0,// Відділення - до 100 кг
                DeliveryType.Address => 50.0,// Адресна доставка - до 50 кг
                DeliveryType.Taxi => 20.0,// Таксі - до 20 кг
                _ => 100.0
            };
        }

        /// <summary>
        /// Створює нову посилку з усіма перевірками та обмеженнями
        /// </summary>
        public OperationResult Create(int senderId, int receiverId, ParcelType type, ContentType contentType,
            double weight, decimal declaredValue, CourierService service, DeliveryType deliveryType,
            string receiverCountry = "Ukraine", bool isInsured = false, decimal insuranceValue = 0, bool isFreeDelivery = false)
        {
            try
            {
                var sender = _clientService.GetById(senderId);
                var receiver = _clientService.GetById(receiverId);

                if (sender == null)
                    return OperationResult.Fail("Відправника не знайдено");

                if (receiver == null)
                    return OperationResult.Fail("Одержувача не знайдено");

                // Перевірка максимальної ваги для типу доставки
                double maxWeight = GetMaxWeight(deliveryType);
                if (weight > maxWeight)
                {
                    return OperationResult.Fail($"Перевищено максимальну вагу для цього типу доставки ({maxWeight} кг)");
                }

                // Перевірка на від'ємну вагу
                if (weight <= 0)
                {
                    return OperationResult.Fail("Вага посилки має бути більше 0");
                }

                // Перевірка оціночної вартості
                if (declaredValue < 0)
                {
                    return OperationResult.Fail("Оціночна вартість не може бути від'ємною");
                }

                var parcel = new Parcel
                {
                    TrackingNumber = GenerateTrackingNumber(),
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Type = type,
                    ContentType = contentType,
                    Weight = weight,
                    DeclaredValue = declaredValue,
                    Service = service,
                    DeliveryType = deliveryType,
                    CurrentStatus = ParcelStatus.AwaitingShipment,
                    CreatedAt = DateTime.Now,
                    SenderCountry = "Ukraine",
                    ReceiverCountry = receiverCountry,
                    IsInsured = isInsured,
                    InsuranceValue = insuranceValue,
                    IsFreeDelivery = isFreeDelivery,
                    EstimatedDeliveryDays = type == ParcelType.Local ?
                        _random.Next(2, 6) : _random.Next(7, 15),
                    IsPriorityProcessing = sender.IsLegend()
                };

                parcel.AddStatusChange(ParcelStatus.AwaitingShipment);
                parcel.AddNotification(ParcelStatus.AwaitingShipment);
                _parcels.Add(parcel);

                sender.IncrementParcels();

                return OperationResult.Ok(parcel);
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Помилка при створенні посилки: {ex.Message}");
            }
        }

        /// <summary>
        /// Змінює статус посилки з перевіркою підтвердження оператора
        /// </summary>
        public OperationResult ChangeStatus(string trackingNumber, ParcelStatus newStatus, string note = "", int? operatorId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(trackingNumber))
                    return OperationResult.Fail("Трекінг-номер не може бути порожнім");

                var parcel = GetByTrackingNumber(trackingNumber);
                if (parcel == null)
                    return OperationResult.Fail("Посилку не знайдено");

                // Перевіряємо чи потрібне підтвердження оператора
                if (newStatus == ParcelStatus.AcceptedByOperator && parcel.RequiresOperatorConfirmation())
                {
                    if (!operatorId.HasValue)
                    {
                        return OperationResult.Fail("Посилка з оціночною вартістю понад 5000 грн потребує підтвердження оператора");
                    }
                }

                parcel.CurrentStatus = newStatus;
                parcel.AddStatusChange(newStatus, note);
                parcel.AddNotification(newStatus, note);

                return OperationResult.Ok((Parcel: parcel, OperatorId: operatorId));
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Помилка при зміні статусу: {ex.Message}");
            }
        }

        /// <summary>
        /// Симулює випадкову затримку доставки з ймовірністю 5%
        /// </summary>
        public OperationResult SimulateDelay(string trackingNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(trackingNumber))
                    return OperationResult.Fail("Трекінг-номер не може бути порожнім");

                var parcel = GetByTrackingNumber(trackingNumber);
                if (parcel == null)
                    return OperationResult.Fail("Посилку не знайдено");

                // 5% ймовірність затримки
                if (_random.NextDouble() >= 0.05)
                {
                    return OperationResult.Ok((
                        HasDelay: false,
                        Reason: (DelayReason?)null,
                        DelayDays: (int?)null,
                        NewEstimate: (int?)null
                    ));
                }

                var reasons = new[]
                {
                    DelayReason.Holiday,
                    DelayReason.Accident,
                    DelayReason.BorderDelay,
                    DelayReason.TransportBreakdown,
                    DelayReason.BadWeather,
                    DelayReason.CustomsInspection
                };

                var reason = reasons[_random.Next(reasons.Length)];
                int delayDays = _random.Next(1, 6);

                parcel.EstimatedDeliveryDays += delayDays;
                parcel.AddNotification(parcel.CurrentStatus, "", reason, delayDays);

                return OperationResult.Ok((
                    HasDelay: true,
                    Reason: (DelayReason?)reason,
                    DelayDays: (int?)delayDays,
                    NewEstimate: (int?)parcel.EstimatedDeliveryDays
                ));
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Помилка при симуляції затримки: {ex.Message}");
            }
        }

        /// <summary>
        /// Розширений пошук посилок за різними критеріями
        /// </summary>
        public List<Parcel> Search(string query = null, ParcelStatus? status = null, DateTime? date = null)
        {
            try
            {
                var results = _parcels.AsEnumerable();

                if (!string.IsNullOrEmpty(query))
                {
                    var clients = _clientService.Search(query);
                    var clientIds = clients.Select(c => c.Id).ToList();

                    results = results.Where(p =>
                        p.TrackingNumber.Contains(query) ||
                        clientIds.Contains(p.SenderId) ||
                        clientIds.Contains(p.ReceiverId));
                }

                if (status.HasValue)
                    results = results.Where(p => p.CurrentStatus == status.Value);

                if (date.HasValue)
                    results = results.Where(p => p.CreatedAt.Date == date.Value.Date);

                return results.ToList();
            }
            catch (Exception)
            {
                return new List<Parcel>();
            }
        }

        /// <summary>
        /// Завантажує список посилок з файлу
        /// </summary>
        public void LoadParcels(List<Parcel> parcels)
        {
            _parcels = parcels ?? new List<Parcel>();
        }

        /// <summary>
        /// Генерує унікальний трекінг-номер на основі дати та випадкового числа
        /// </summary>
        private string GenerateTrackingNumber()
        {
            return $"{DateTime.Now:yyyyMMddHHmmss}{_random.Next(1000, 9999)}";
        }
    }
}