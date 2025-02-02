﻿using HES.Core.Enums;
using HES.Core.Interfaces;
using HES.Core.Models.Web.AppSettings;
using HES.Web.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HES.Web.Pages.Settings.Parameters
{
    public partial class Parameters : HESComponentBase, IDisposable
    {
        public IAppSettingsService AppSettingsService { get; set; }
        [Inject] public IModalDialogService ModalDialogService { get; set; }
        [Inject] public IBreadcrumbsService BreadcrumbsService { get; set; }
        [Inject] public IToastService ToastService { get; set; }
        [Inject] public ILogger<Parameters> Logger { get; set; }

        public LicensingSettings LicensingSettings { get; set; }
        public string DomainHost { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                AppSettingsService = ScopedServices.GetRequiredService<IAppSettingsService>();
                SynchronizationService.UpdateParametersPage += UpdateParametersPage;

                await BreadcrumbsService.SetParameters();
                await LoadDataSettingsAsync();

                SetInitialized();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                SetLoadFailed(ex.Message);
            }
        }

        private async Task UpdateParametersPage(string exceptPageId, string userName)
        {
            await InvokeAsync(async () =>
            {
                await LoadDataSettingsAsync();
                StateHasChanged();

                if (PageId != exceptPageId)
                    await ToastService.ShowToastAsync($"Page edited by {userName}.", ToastType.Notify);
            });
        }

        private async Task LoadDataSettingsAsync()
        {
            LicensingSettings = await LoadLicensingSettingsAsync();
            DomainHost = await LoadDomainSettingsAsync();
        }

        private async Task<LicensingSettings> LoadLicensingSettingsAsync()
        {
            var license = await AppSettingsService.GetLicensingSettingsAsync();

            if (license == null)
                return new LicensingSettings();

            return license;
        }

        private async Task OpenDialogLicensingSettingsAsync()
        {
            RenderFragment body = (builder) =>
            {
                builder.OpenComponent(0, typeof(LicenseSettingsDialog));
                builder.AddAttribute(1, nameof(LicenseSettingsDialog.LicensingSettings), LicensingSettings);
                builder.AddAttribute(2, nameof(LicenseSettingsDialog.ExceptPageId), PageId);
                builder.CloseComponent();
            };

            await ModalDialogService.ShowAsync("License Settings", body);
        }

        private async Task<string> LoadDomainSettingsAsync()
        {
            var domainSettings = await AppSettingsService.GetLdapSettingsAsync();
            return domainSettings?.Host;
        }

        private async Task OpenDialogLdapSettingsAsync()
        {
            RenderFragment body = (builder) =>
            {
                builder.OpenComponent(0, typeof(LdapSettingsDialog));
                builder.AddAttribute(1, nameof(LdapSettingsDialog.Host), DomainHost);
                builder.AddAttribute(2, nameof(LdapSettingsDialog.ExceptPageId), PageId);
                builder.CloseComponent();
            };

            await ModalDialogService.ShowAsync("Domain Settings", body);
        }

        private async Task OpenDialogDeleteLdapCredentialsAsync()
        {
            RenderFragment body = (builder) =>
            {
                builder.OpenComponent(0, typeof(DeleteLdapCredentials));
                builder.AddAttribute(1, nameof(DeleteLdapCredentials.ExceptPageId), PageId);
                builder.CloseComponent();
            };

            await ModalDialogService.ShowAsync("Delete", body);
        }

        public void Dispose()
        {
            SynchronizationService.UpdateParametersPage -= UpdateParametersPage;
        }
    }
}