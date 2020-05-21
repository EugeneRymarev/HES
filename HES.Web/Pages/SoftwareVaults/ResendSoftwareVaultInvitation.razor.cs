﻿using HES.Core.Entities;
using HES.Core.Enums;
using HES.Core.Interfaces;
using HES.Core.Models.Web.AppSettings;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HES.Web.Pages.SoftwareVaults
{
    public partial class ResendSoftwareVaultInvitation : ComponentBase
    {
        [Inject] public ISoftwareVaultService SoftwareVaultService { get; set; }
        [Inject] public IAppSettingsService AppSettingsService { get; set; }
        [Inject] public ILogger<ResendSoftwareVaultInvitation> Logger { get; set; }
        [Inject] public IModalDialogService ModalDialogService { get; set; }
        [Inject] IToastService ToastService { get; set; }
        [Parameter] public EventCallback Refresh { get; set; }
        [Parameter] public SoftwareVaultInvitation SoftwareVaultInvitation { get; set; }

        public ServerSettings ServerSettings { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                ServerSettings = await AppSettingsService.GetServerSettingsAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                ToastService.ShowToast(ex.Message, ToastLevel.Error);
                await ModalDialogService.CloseAsync();
            }
        }

        public async Task SendAsync()
        {
            try
            {
                await SoftwareVaultService.ResendInvitationAsync(SoftwareVaultInvitation.Employee, ServerSettings, SoftwareVaultInvitation.Id);
                await Refresh.InvokeAsync(this);
                ToastService.ShowToast("Invitation sent.", ToastLevel.Success);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                ToastService.ShowToast(ex.Message, ToastLevel.Error);
            }
            finally
            {
                await ModalDialogService.CloseAsync();
            }
        }
    }
}