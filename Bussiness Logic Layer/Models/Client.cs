using System;
using System.Collections.Generic;

namespace BusinessLogicLayer.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public ClientType Type { get; set; }
        public LoyaltyStatus Status { get; set; }
        public int TotalParcels { get; set; }
        public DateTime? LastDiscountUsed { get; set; }

        public Client() { }

        public Client(string fullName, string phone, string email, string address, ClientType type)
        {
            FullName = fullName;
            Phone = phone;
            Email = email;
            Address = address;
            Type = type;
            Status = LoyaltyStatus.Beginner;
            TotalParcels = 0;
        }

        public decimal GetDiscount()
        {
            return Status switch
            {
                LoyaltyStatus.Beginner => 0.05m,
                LoyaltyStatus.Active => 0.10m,
                LoyaltyStatus.Pro => 0.15m,
                LoyaltyStatus.Legend => 0.20m,
                _ => 0m
            };
        }

        public bool CanUseDiscount()
        {
            if (!LastDiscountUsed.HasValue) return true;
            return (DateTime.Now - LastDiscountUsed.Value).TotalDays >= 30;
        }

        public void UseDiscount()
        {
            LastDiscountUsed = DateTime.Now;
        }

        public void IncrementParcels()
        {
            TotalParcels++;
            UpdateLoyaltyStatus();
        }

        public void UpdateLoyaltyStatus()
        {
            Status = TotalParcels switch
            {
                >= 200 => LoyaltyStatus.Legend,
                >= 51 => LoyaltyStatus.Pro,
                >= 11 => LoyaltyStatus.Active,
                _ => LoyaltyStatus.Beginner
            };
        }

        public bool IsLegend() => Status == LoyaltyStatus.Legend;
    }
}