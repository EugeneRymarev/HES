﻿using HES.Core.Entities;
using HES.Core.Enums;
using HES.Core.Interfaces;
using Hideez.SDK.Communication.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HES.Web.Pages.Devices
{
    public partial class ChangeVaultStatus : ComponentBase
    {
        [Inject]
        public IDeviceService HardwareVaultService { get; set; }

        [Inject] 
        public ILogger<ChangeVaultStatus> Logger { get; set; }

        [Inject] 
        public IModalDialogService ModalDialogService { get; set; }

        [Inject] 
        IToastService ToastService { get; set; }

        [Parameter]
        public string HardwareVaultId { get; set; }

        [Parameter]
        public VaultStatus VaultStatus { get; set; }

        [Parameter]
        public EventCallback Refresh { get; set; }

        public string StatusDescription { get; set; }

        private async Task ChangeStatusAsync()
        {
            if (VaultStatus == VaultStatus.Suspended)
            {
                try
                {
                    await HardwareVaultService.SuspendVaultAsync(HardwareVaultId);
                    await Refresh.InvokeAsync(this);
                    ToastService.ShowToast("Vault suspended", ToastLevel.Success);
                    await CloseAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.Message);
                    ToastService.ShowToast(ex.Message, ToastLevel.Error);
                    await CloseAsync();
                }
            }
        }

        private async Task CloseAsync()
        {
            await ModalDialogService.CloseAsync();
        }
    }
}
