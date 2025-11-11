using System;
using System.Collections.Generic;

namespace BusinessLogicLayer.Models
{
    public enum ClientType { Individual, Organization }
    public enum LoyaltyStatus { Beginner, Active, Pro, Legend }
    public enum ParcelType { Local, International }
    public enum ContentType { Document, Package, Fragile }
    public enum DeliveryType { Office, Parcelbox, Address, Taxi }
    public enum CourierService { Ukrposhta, NovaPoshta, MeestExpress }

    public enum ParcelStatus
    {
        AwaitingShipment,
        AcceptedByOperator,
        InTransit,
        AtWarehouse,
        Delivered,
        Lost
    }

    public enum DelayReason
    {
        Holiday,
        Accident,
        BorderDelay,
        TransportBreakdown,
        BadWeather,
        CustomsInspection
    }
}
