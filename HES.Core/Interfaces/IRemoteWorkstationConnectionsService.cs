﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Hideez.SDK.Communication.HES.Client;
using Hideez.SDK.Communication.HES.Client.Requests;
using Hideez.SDK.Communication.Remote;
using Hideez.SDK.Communication.Workstation;

namespace HES.Core.Interfaces
{
    public interface IRemoteWorkstationConnectionsService
    {
        void StartUpdateRemoteDevice(IList<string> devicesId);
        void StartUpdateRemoteDevice(string deviceId);
        Task UpdateRemoteDeviceAsync(string deviceId, string workstationId, bool primaryAccountOnly);
        Task RegisterWorkstationInfoAsync(IRemoteAppConnection remoteAppConnection, WorkstationInfo workstationInfo);
        Task OnAppHubDisconnectedAsync(string workstationId);
        Task<HesResponse> RequestWorkstationUnlockAsync(string workstationId, string unlockToken, string login, string password);
    }
}
