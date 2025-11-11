using System;
using System.Collections.Generic;
using System.Linq;
using BusinessLogicLayer.Models;

namespace BusinessLogicLayer.Services
{
    public class ParcelService
    {
        private List<Parcel> _parcels;
        private ClientService _clientService;
        private Random _random;

        public ParcelService(ClientService clientService)
        {
            _parcels = new List<Parcel>();
            _clientService = clientService;
            _random = new Random();
        }

        public List<Parcel> GetAll() => _parcels;

        public Parcel GetByTrackingNumber(string trackingNumber) =>
            _parcels.FirstOrDefault(p => p.TrackingNumber == trackingNumber);

        public OperationResult Create(int senderId, int receiverId, ParcelType type, ContentType contentType,
            double weight, decimal declaredValue, CourierService service, DeliveryType deliveryType,
            string receiverCountry = "Ukraine", bool isInsured = false, decimal insuranceValue = 0)
        {
            var sender = _clientService.GetById(senderId);
            var receiver = _clientService.GetById(receiverId);

            if (sender == null || receiver == null)
                return OperationResult.Fail();

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
                EstimatedDeliveryDays = type == ParcelType.Local ?
                    _random.Next(2, 6) : _random.Next(7, 15)
            };

            parcel.AddStatusChange(ParcelStatus.AwaitingShipment);
            parcel.AddNotification(ParcelStatus.AwaitingShipment);
            _parcels.Add(parcel);

            sender.IncrementParcels();

            return OperationResult.Ok(parcel);
        }

        public OperationResult ChangeStatus(string trackingNumber, ParcelStatus newStatus, string note = "", int? operatorId = null)
        {
            var parcel = GetByTrackingNumber(trackingNumber);
            if (parcel == null) return OperationResult.Fail();

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

            return OperationResult.Ok(new { Parcel = parcel, OperatorId = operatorId });
        }

        public OperationResult SimulateDelay(string trackingNumber)
        {
            var parcel = GetByTrackingNumber(trackingNumber);
            if (parcel == null) return OperationResult.Fail();

            if (_random.NextDouble() >= 0.05)
                return OperationResult.Ok(new { HasDelay = false });

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

            return OperationResult.Ok(new
            {
                HasDelay = true,
                Reason = reason,
                DelayDays = delayDays,
                NewEstimate = parcel.EstimatedDeliveryDays
            });
        }

        public List<Parcel> Search(string query = null, ParcelStatus? status = null, DateTime? date = null)
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

        public void LoadParcels(List<Parcel> parcels)
        {
            _parcels = parcels ?? new List<Parcel>();
        }

        private string GenerateTrackingNumber()
        {
            return $"{DateTime.Now:yyyyMMddHHmmss}{_random.Next(1000, 9999)}";
        }
    }
}