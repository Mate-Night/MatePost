using System;
using System.Collections.Generic;

namespace BusinessLogicLayer.Models
{
    /// <summary>Тип клієнта</summary>
    public enum ClientType
    {
        /// <summary>Фізична особа</summary>
        Individual,
        /// <summary>Організація</summary>
        Organization
    }

    /// <summary>Статус лояльності клієнта</summary>
    public enum LoyaltyStatus
    {
        /// <summary>Початківець (0-10 посилок)</summary>
        Beginner,
        /// <summary>Активний клієнт (11-50 посилок)</summary>
        Active,
        /// <summary>Поштовий профі (51-200 посилок)</summary>
        Pro,
        /// <summary>Легенда доставки (200+ посилок)</summary>
        Legend
    }

    /// <summary>Тип посилки</summary>
    public enum ParcelType
    {
        /// <summary>Локальна доставка по Україні</summary>
        Local,
        /// <summary>Міжнародна доставка</summary>
        International
    }

    /// <summary>Тип вмісту посилки</summary>
    public enum ContentType
    {
        /// <summary>Документи</summary>
        Document,
        /// <summary>Звичайна посилка</summary>
        Package,
        /// <summary>Крихкий вміст</summary>
        Fragile
    }

    /// <summary>Тип доставки</summary>
    public enum DeliveryType
    {
        /// <summary>Доставка до відділення</summary>
        Office,
        /// <summary>Доставка до поштомату</summary>
        Parcelbox,
        /// <summary>Адресна доставка кур'єром</summary>
        Address,
        /// <summary>Швидка доставка таксі</summary>
        Taxi
    }

    /// <summary>Кур'єрська служба</summary>
    public enum CourierService
    {
        /// <summary>Укрпошта</summary>
        Ukrposhta,
        /// <summary>Нова Пошта</summary>
        NovaPoshta,
        /// <summary>Meest Express</summary>
        MeestExpress
    }

    /// <summary>Статус посилки в процесі доставки</summary>
    public enum ParcelStatus
    {
        /// <summary>Очікує відправки</summary>
        AwaitingShipment,
        /// <summary>Прийнято оператором</summary>
        AcceptedByOperator,
        /// <summary>В дорозі</summary>
        InTransit,
        /// <summary>На складі в місті призначення</summary>
        AtWarehouse,
        /// <summary>Доставлено одержувачу</summary>
        Delivered,
        /// <summary>Втрачено</summary>
        Lost
    }

    /// <summary>Причини затримки доставки</summary>
    public enum DelayReason
    {
        /// <summary>Святковий день</summary>
        Holiday,
        /// <summary>Аварія на транспорті</summary>
        Accident,
        /// <summary>Затримка на кордоні</summary>
        BorderDelay,
        /// <summary>Поломка транспорту</summary>
        TransportBreakdown,
        /// <summary>Погані погодні умови</summary>
        BadWeather,
        /// <summary>Митна перевірка</summary>
        CustomsInspection
    }
}