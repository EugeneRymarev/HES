﻿using System;
using System.Linq;
using System.Threading.Tasks;
using HES.Core.Entities;
using HES.Core.Interfaces;
using Hideez.SDK.Communication;
using Hideez.SDK.Communication.HES.DTO;
using Hideez.SDK.Communication.Remote;
using Hideez.SDK.Communication.Workstation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HES.Core.Hubs
{
    public class AppHub : Hub<IRemoteAppConnection>
    {
        private readonly IRemoteDeviceConnectionsService _remoteDeviceConnectionsService;
        private readonly IRemoteWorkstationConnectionsService _remoteWorkstationConnectionsService;
        private readonly IWorkstationEventService _workstationEventService;
        private readonly IWorkstationSessionService _workstationSessionService;
        private readonly IDeviceService _deviceService;
        private readonly IDeviceTaskService _deviceTaskService;
        private readonly ILogger<AppHub> _logger;

        public AppHub(IRemoteDeviceConnectionsService remoteDeviceConnectionsService,
                      IRemoteWorkstationConnectionsService remoteWorkstationConnectionsService,
                      IWorkstationEventService workstationEventService,
                      IWorkstationSessionService workstationSessionService,
                      IDeviceService deviceService,
                      IDeviceTaskService deviceTaskService,
                      ILogger<AppHub> logger)
        {
            _remoteDeviceConnectionsService = remoteDeviceConnectionsService;
            _remoteWorkstationConnectionsService = remoteWorkstationConnectionsService;
            _workstationEventService = workstationEventService;
            _workstationSessionService = workstationSessionService;
            _deviceService = deviceService;
            _deviceTaskService = deviceTaskService;
            _logger = logger;
        }

        private string GetWorkstationId()
        {
            if (Context.Items.TryGetValue("WorkstationId", out object workstationId))
                return (string)workstationId;
            else
                throw new Exception("AppHub does not contain WorkstationId!");
        }

        public override Task OnConnectedAsync()
        {
            try
            {
                var httpContext = Context.GetHttpContext();
                string workstationId = httpContext.Request.Headers["WorkstationId"].ToString();

                if (string.IsNullOrWhiteSpace(workstationId))
                    throw new Exception($"AppHub.OnConnectedAsync - httpContext.Request.Headers does not contain WorkstationId");

                _logger.LogInformation($"[{workstationId}] Workstation connected");
                _remoteDeviceConnectionsService.OnAppHubConnected(workstationId, Clients.Caller);
                Context.Items.Add("WorkstationId", workstationId);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex.Message);
            }

            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                var workstationId = GetWorkstationId();

                _logger.LogInformation($"[{workstationId}] Workstation disconnected");

                _remoteDeviceConnectionsService.OnAppHubDisconnected(workstationId);
                await _remoteWorkstationConnectionsService.OnAppHubDisconnectedAsync(workstationId);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex.Message);
            }

            await base.OnDisconnectedAsync(exception);
        }

        #region Device

        // incoming request
        public async Task OnDeviceConnected(BleDeviceDto dto)
        {
            try
            {
                if (dto == null || dto.DeviceSerialNo == null)
                    throw new ArgumentNullException(nameof(dto));

                // Update Battery, Firmware, State, LastSynced         
                await _deviceService.UpdateDeviceInfoAsync(dto.DeviceSerialNo, dto.Battery, dto.FirmwareVersion, dto.IsLocked);

                _remoteDeviceConnectionsService.OnDeviceConnected(dto.DeviceSerialNo, GetWorkstationId(), Clients.Caller);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex.Message);
            }
        }

        // Incoming request
        public Task OnDeviceDisconnected(string deviceId)
        {
            try
            {
                if (!string.IsNullOrEmpty(deviceId))
                    _remoteDeviceConnectionsService.OnDeviceDisconnected(deviceId, GetWorkstationId());
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex.Message);
            }
            return Task.CompletedTask;
        }

        // Incomming request
        public async Task<DeviceInfoDto> GetInfoByRfid(string rfid)
        {
            try
            {
                var device = await _deviceService
                    .Query()
                    .Include(d => d.Employee)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.RFID == rfid);

                return await GetDeviceInfo(device);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex.Message);
                return null;
            }
        }

        // Incomming request
        public async Task<DeviceInfoDto> GetInfoByMac(string mac)
        {
            try
            {
                var device = await _deviceService
                    .Query()
                    .Include(d => d.Employee)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.MAC == mac);

                return await GetDeviceInfo(device);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex.Message);
                return null;
            }
        }

        // Incomming request
        public async Task<DeviceInfoDto> GetInfoBySerialNo(string serialNo)
        {
            try
            {
                var device = await _deviceService
                    .Query()
                    .Include(d => d.Employee)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == serialNo);

                var deviceInfo = await GetDeviceInfo(device);
                return deviceInfo;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex.Message);
                return null;
            }
        }

        private async Task<DeviceInfoDto> GetDeviceInfo(Device device)
        {
            if (device == null)
                return null;

            bool needUpdate = await _deviceTaskService
                .Query()
                .Where(t => t.DeviceId == device.Id)
                .AsNoTracking()
                .AnyAsync();

            var info = new DeviceInfoDto()
            {
                OwnerName = device.Employee?.FullName,
                OwnerEmail = device.Employee?.Email,
                DeviceMac = device.MAC,
                DeviceSerialNo = device.Id,
                NeedUpdate = needUpdate
            };

            return info;
        }

        // Incoming request
        public async Task<HideezErrorInfo> FixDevice(string deviceId)
        {
            try
            {
                await _remoteWorkstationConnectionsService.UpdateRemoteDeviceAsync(deviceId, GetWorkstationId(), primaryAccountOnly: true);
                _remoteWorkstationConnectionsService.StartUpdateRemoteDevice(deviceId);
                return HideezErrorInfo.Ok;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{deviceId}] {ex.Message}");
                return new HideezErrorInfo(ex);
            }
        }

        #endregion

        #region Workstation

        // Incomming request
        public async Task<HideezErrorInfo> RegisterWorkstationInfo(WorkstationInfo workstationInfo)
        {
            try
            {
                await _remoteWorkstationConnectionsService.RegisterWorkstationInfoAsync(Clients.Caller, workstationInfo);
                return HideezErrorInfo.Ok;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{workstationInfo?.MachineName}] {ex.Message}");
                return new HideezErrorInfo(ex);
            }
        }

        #endregion

        #region Audit

        // Incomming request
        public async Task<HideezErrorInfo> SaveClientEvents(WorkstationEventDto[] workstationEventsDto)
        {
            if (workstationEventsDto == null)
                throw new ArgumentNullException(nameof(workstationEventsDto));

            // Ignore not approved workstation
            //var workstationId = workstationEventsDto.FirstOrDefault().WorkstationId;
            //var workstaton = await _workstationService.GetByIdAsync(workstationId);
            //if (workstaton.Approved == false)
            //{
            //    return new HideezErrorInfo(HideezErrorCode.HesWorkstationNotApproved, $"{workstationId}");
            //}

            foreach (var eventDto in workstationEventsDto)
            {
                try
                {
                    await _workstationEventService.AddEventDtoAsync(eventDto);
                }
                catch (Exception ex)
                {
                    var objDto = $@"Version:{eventDto.Version}, 
                                        Id:{eventDto.Id},
                                        Date:{eventDto.Date},
                                        EventId:{eventDto.EventId},
                                        SeverityId:{eventDto.SeverityId},
                                        Note:{eventDto.Note},
                                        WorkstationId:{eventDto.WorkstationId},
                                        UserSession:{eventDto.UserSession},
                                        WorkstationSessionId:{eventDto.WorkstationSessionId},
                                        DeviceId:{eventDto.DeviceId},
                                        AccountName:{eventDto.AccountName},
                                        AccountLogin:{eventDto.AccountLogin}";
                    _logger.LogError($"{ex.Message} [AddEventDtoAsync] [Object DTO]: {objDto}");
                }

                try
                {
                    await _workstationSessionService.AddOrUpdateWorkstationSession(eventDto);
                }
                catch (Exception ex)
                {
                    var objDto = $@"Version:{eventDto.Version}, 
                                        Id:{eventDto.Id},
                                        Date:{eventDto.Date},
                                        EventId:{eventDto.EventId},
                                        SeverityId:{eventDto.SeverityId},
                                        Note:{eventDto.Note},
                                        WorkstationId:{eventDto.WorkstationId},
                                        UserSession:{eventDto.UserSession},
                                        WorkstationSessionId:{eventDto.WorkstationSessionId},
                                        DeviceId:{eventDto.DeviceId},
                                        AccountName:{eventDto.AccountName},
                                        AccountLogin:{eventDto.AccountLogin}";
                    _logger.LogError($"{ex.Message} [AddOrUpdateWorkstationSession] [Object DTO]: {objDto}");
                }
            }

            return HideezErrorInfo.Ok;
        }

        #endregion
    }
}