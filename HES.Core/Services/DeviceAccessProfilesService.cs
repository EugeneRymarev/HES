﻿using HES.Core.Entities;
using HES.Core.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HES.Core.Services
{
    public class DeviceAccessProfilesService : IDeviceAccessProfilesService
    {
        private readonly IAsyncRepository<DeviceAccessProfile> _deviceAccessProfileRepository;

        public DeviceAccessProfilesService(IAsyncRepository<DeviceAccessProfile> deviceAccessProfileRepository)
        {
            _deviceAccessProfileRepository = deviceAccessProfileRepository;
        }

        public IQueryable<DeviceAccessProfile> DeviceAccessProfilesQuery()
        {
            return _deviceAccessProfileRepository.Query();
        }

        public async Task<DeviceAccessProfile> GetByIdAsync(dynamic id)
        {
            return await _deviceAccessProfileRepository.GetByIdAsync(id);
        }

        public async Task CreateProfileAsync(DeviceAccessProfile deviceAccessProfile)
        {
            if (deviceAccessProfile == null)
            {
                throw new ArgumentNullException(nameof(deviceAccessProfile));
            }

            deviceAccessProfile.CreatedAt = DateTime.UtcNow;
            await _deviceAccessProfileRepository.AddAsync(deviceAccessProfile);
        }

        public async Task EditProfileAsync(DeviceAccessProfile deviceAccessProfile)
        {
            if (deviceAccessProfile == null)
            {
                throw new ArgumentNullException(nameof(deviceAccessProfile));
            }

            deviceAccessProfile.UpdatedAt = DateTime.UtcNow;
            await _deviceAccessProfileRepository.UpdateAsync(deviceAccessProfile);
        }

        public async Task DeleteProfileAsync(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var deviceAccessProfile = await _deviceAccessProfileRepository.GetByIdAsync(id);
            if (deviceAccessProfile == null)
            {
                throw new Exception("Device access profile not found");
            }

            await _deviceAccessProfileRepository.DeleteAsync(deviceAccessProfile);
        }
    }
}