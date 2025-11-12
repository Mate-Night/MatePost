using System;
using System.Collections.Generic;
using System.Linq;
using BusinessLogicLayer.Models;

namespace BusinessLogicLayer.Services
{
    /// <summary>
    /// Сервіс для збору та обробки статистики системи
    /// </summary>
    public class StatisticsService
    {
        private ParcelService _parcelService;
        private OperatorService _operatorService;

        /// <summary>
        /// Конструктор сервісу статистики
        /// </summary>
        public StatisticsService(ParcelService parcelService, OperatorService operatorService)
        {
            _parcelService = parcelService;
            _operatorService = operatorService;
        }

        /// <summary>
        /// Збирає повну статистику системи за вказаний період
        /// </summary>
        public Dictionary<string, object> GetStatistics(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var parcels = _parcelService.GetAll().AsEnumerable();

            // Фільтрація за періодом
            if (fromDate.HasValue)
                parcels = parcels.Where(p => p.CreatedAt >= fromDate.Value);
            if (toDate.HasValue)
                parcels = parcels.Where(p => p.CreatedAt <= toDate.Value);

            var parcelsList = parcels.ToList();

            var stats = new Dictionary<string, object>
            {
                // Загальна кількість посилок
                ["TotalParcels"] = parcelsList.Count,

                // Розподіл за статусами
                ["Delivered"] = parcelsList.Count(p => p.CurrentStatus == ParcelStatus.Delivered),
                ["Lost"] = parcelsList.Count(p => p.CurrentStatus == ParcelStatus.Lost),
                ["InTransit"] = parcelsList.Count(p => p.CurrentStatus == ParcelStatus.InTransit),
                ["AwaitingShipment"] = parcelsList.Count(p => p.CurrentStatus == ParcelStatus.AwaitingShipment),
                ["AcceptedByOperator"] = parcelsList.Count(p => p.CurrentStatus == ParcelStatus.AcceptedByOperator),
                ["AtWarehouse"] = parcelsList.Count(p => p.CurrentStatus == ParcelStatus.AtWarehouse)
            };

            // Середній час доставки для локальних посилок
            var localDelivered = parcelsList.Where(p => p.Type == ParcelType.Local &&
                p.CurrentStatus == ParcelStatus.Delivered).ToList();

            // Середній час доставки для міжнародних посилок
            var intlDelivered = parcelsList.Where(p => p.Type == ParcelType.International &&
                p.CurrentStatus == ParcelStatus.Delivered).ToList();

            if (localDelivered.Any())
                stats["AvgLocalDelivery"] = localDelivered.Average(p => p.EstimatedDeliveryDays);

            if (intlDelivered.Any())
                stats["AvgInternationalDelivery"] = intlDelivered.Average(p => p.EstimatedDeliveryDays);

            // ТОП-3 найефективніших операторів
            stats["TopOperators"] = _operatorService.GetAll()
                .OrderByDescending(o => o.ProcessedParcels)
                .Take(3)
                .ToList();

            // Найпопулярніші напрямки доставки
            var destinations = parcelsList
                .GroupBy(p => p.ReceiverCountry)
                .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                .OrderByDescending(kv => kv.Value)
                .ToList();

            stats["PopularDestinations"] = destinations;

            return stats;
        }
    }
}