﻿using HES.Core.Entities;
using HES.Core.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HES.Core.Interfaces
{
    public interface ILicenseService
    {
        IQueryable<LicenseOrder> LicenseOrderQuery();
        Task<List<LicenseOrder>> GetLicenseOrdersAsync();
        Task<LicenseOrder> GetLicenseOrderByIdAsync(string id);
        Task<List<LicenseOrder>> GetOpenLicenseOrdersAsync();
        Task<LicenseOrder> CreateOrderAsync(LicenseOrder licenseOrder);
        Task DeleteOrderAsync(string id);
        Task SendOrderAsync(string orderId);
        Task<OrderStatus> GetLicenseOrderStatusAsync(string orderId);
        Task ChangeOrderStatusAsync(LicenseOrder licenseOrder, OrderStatus orderStatus);
        Task<List<DeviceLicense>> AddDeviceLicensesAsync(string orderId, List<string> devicesIds);
        Task UpdateNewDeviceLicensesAsync(string orderId);
        Task<IList<DeviceLicense>> GetDeviceLicensesByDeviceIdAsync(string deviceId);
        Task<IList<DeviceLicense>> GetDeviceLicensesByOrderIdAsync(string orderId);
        Task SetDeviceLicenseAppliedAsync(string deviceId, string licenseId);
    }
}