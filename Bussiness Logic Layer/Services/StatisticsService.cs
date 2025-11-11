using System;
using System.Collections.Generic;
using System.Linq;
using BusinessLogicLayer.Models;

namespace BusinessLogicLayer.Services
{
    public class StatisticsService
    {
        private ParcelService _parcelService;
        private OperatorService _operatorService;

        public StatisticsService(ParcelService parcelService, OperatorService operatorService)
        {
            _parcelService = parcelService;
            _operatorService = operatorService;
        }

        public Dictionary<string, object> GetStatistics(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var parcels = _parcelService.GetAll().AsEnumerable();

            if (fromDate.HasValue)
                parcels = parcels.Where(p => p.CreatedAt >= fromDate.Value);
            if (toDate.HasValue)
                parcels = parcels.Where(p => p.CreatedAt <= toDate.Value);

            var parcelsList = parcels.ToList();

            var stats = new Dictionary<string, object>
            {
                ["TotalParcels"] = parcelsList.Count,
                ["Delivered"] = parcelsList.Count(p => p.CurrentStatus == ParcelStatus.Delivered),
                ["Lost"] = parcelsList.Count(p => p.CurrentStatus == ParcelStatus.Lost),
                ["InTransit"] = parcelsList.Count(p => p.CurrentStatus == ParcelStatus.InTransit),
                ["AwaitingShipment"] = parcelsList.Count(p => p.CurrentStatus == ParcelStatus.AwaitingShipment),
                ["AcceptedByOperator"] = parcelsList.Count(p => p.CurrentStatus == ParcelStatus.AcceptedByOperator),
                ["AtWarehouse"] = parcelsList.Count(p => p.CurrentStatus == ParcelStatus.AtWarehouse)
            };

            var localDelivered = parcelsList.Where(p => p.Type == ParcelType.Local &&
                p.CurrentStatus == ParcelStatus.Delivered).ToList();
            var intlDelivered = parcelsList.Where(p => p.Type == ParcelType.International &&
                p.CurrentStatus == ParcelStatus.Delivered).ToList();

            if (localDelivered.Any())
                stats["AvgLocalDelivery"] = localDelivered.Average(p => p.EstimatedDeliveryDays);
            if (intlDelivered.Any())
                stats["AvgInternationalDelivery"] = intlDelivered.Average(p => p.EstimatedDeliveryDays);

            stats["TopOperators"] = _operatorService.GetAll()
                .OrderByDescending(o => o.ProcessedParcels)
                .Take(3)
                .ToList();

            return stats;
        }
    }
}