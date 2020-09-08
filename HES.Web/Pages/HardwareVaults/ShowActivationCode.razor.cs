﻿using HES.Core.Entities;
using HES.Core.Enums;
using HES.Core.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace HES.Web.Pages.HardwareVaults
{
    public partial class ShowActivationCode : ComponentBase
    {
        [Inject] public IHardwareVaultService HardwareVaultService { get; set; }
        [Inject] public IEmailSenderService EmailSenderService { get; set; }
        [Inject] public IModalDialogService ModalDialogService { get; set; }
        [Inject] public IToastService ToastService { get; set; }
        [Inject] public ILogger<ShowActivationCode> Logger { get; set; }
        [Inject] public IJSRuntime JsRuntime { get; set; }
        [Parameter] public string HardwareVaultId { get; set; }
        public HardwareVault HardwareVault { get; set; }

        public string Code { get; set; }
        public string InputType { get; private set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                HardwareVault = await HardwareVaultService.GetVaultByIdAsync(HardwareVaultId);
                if (HardwareVault == null)
                    throw new Exception("HardwareVault not found.");

                Code = await HardwareVaultService.GetVaultActivationCodeAsync(HardwareVault.Id);
                InputType = "Password";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                ToastService.ShowToast(ex.Message, ToastLevel.Error);
                await ModalDialogService.CancelAsync();
            }
        }

        private async Task SendEmailAsync()
        {
            await EmailSenderService.SendHardwareVaultActivationCodeAsync(HardwareVault.Employee, Code);
        }

        private async Task CopyToClipboardAsync()
        {
            await JsRuntime.InvokeVoidAsync("copyToClipboard");
        }

        private async Task CloseAsync()
        {
            Code = string.Empty;
            await ModalDialogService.CloseAsync();
        }
    }
}