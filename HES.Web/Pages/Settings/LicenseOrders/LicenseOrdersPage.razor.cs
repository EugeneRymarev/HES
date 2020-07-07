﻿using HES.Core.Entities;
using HES.Core.Enums;
using HES.Core.Interfaces;
using HES.Core.Models.Web.LicenseOrders;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System.ComponentModel;
using System.Threading.Tasks;

namespace HES.Web.Pages.Settings.LicenseOrders
{
    public partial class LicenseOrdersPage : ComponentBase
    {
        [Inject] public ILicenseService LicenseService { get; set; }
        [Inject] public IBreadcrumbsService BreadcrumbsService { get; set; }
        [Inject] public IMainTableService<LicenseOrder, LicenseOrderFilter> MainTableService { get; set; }
        [Inject] public IToastService ToastService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

        private HubConnection hubConnection;

        protected override async Task OnInitializedAsync()
        {
            await MainTableService.InitializeAsync(LicenseService.GetLicenseOrdersAsync, LicenseService.GetLicenseOrdersCountAsync, StateHasChanged, nameof(LicenseOrder.CreatedAt), ListSortDirection.Descending);
            await BreadcrumbsService.SetLicenseOrders();
            await InitializeHubAsync();
        }

        private async Task CreateLicenseOrderAsync()
        {
            RenderFragment body = (builder) =>
            {
                builder.OpenComponent(0, typeof(CreateLicenseOrder));
                builder.AddAttribute(1, "ConnectionId", hubConnection?.ConnectionId);
                builder.CloseComponent();
            };

            await MainTableService.ShowModalAsync("Create License Order", body, ModalDialogSize.Large);
        }

        private async Task SendLicenseOrderAsync()
        {
            RenderFragment body = (builder) =>
            {
                builder.OpenComponent(0, typeof(SendLicenseOrder));
                builder.AddAttribute(1, nameof(SendLicenseOrder.LicenseOrder), MainTableService.SelectedEntity);
                builder.AddAttribute(2, "ConnectionId", hubConnection?.ConnectionId);
                builder.CloseComponent();
            };

            await MainTableService.ShowModalAsync("Send License Order", body);
        }

        private async Task LicenseOrderDetailsAsync()
        {
            RenderFragment body = (builder) =>
            {
                builder.OpenComponent(0, typeof(DetailsLicenseOrder));
                builder.AddAttribute(1, nameof(DetailsLicenseOrder.LicenseOrder), MainTableService.SelectedEntity);
                builder.CloseComponent();
            };

            await MainTableService.ShowModalAsync("License Order Details", body, ModalDialogSize.ExtraLarge);
        }

        private async Task EditLicenseOrderAsync()
        {
            RenderFragment body = (builder) =>
            {
                builder.OpenComponent(0, typeof(EditLicenseOrder));
                builder.AddAttribute(1, nameof(EditLicenseOrder.LicenseOrder), MainTableService.SelectedEntity);
                builder.AddAttribute(2, "ConnectionId", hubConnection?.ConnectionId);
                builder.CloseComponent();
            };

            await MainTableService.ShowModalAsync("Edit License Order", body, ModalDialogSize.ExtraLarge);
        }

        private async Task DeleteLicenseOrderAsync()
        {
            RenderFragment body = (builder) =>
            {
                builder.OpenComponent(0, typeof(DeleteLicenseOrder));
                builder.AddAttribute(1, nameof(DeleteLicenseOrder.LicenseOrder), MainTableService.SelectedEntity);
                builder.AddAttribute(2, "ConnectionId", hubConnection?.ConnectionId);
                builder.CloseComponent();
            };

            await MainTableService.ShowModalAsync("Delete License Order", body);
        }

        private async Task InitializeHubAsync()
        {
            hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/refreshHub"))
            .Build();

            hubConnection.On(RefreshPage.Licenses, async () =>
            {
                await LicenseService.DetachLicenseOrders(MainTableService.Entities);
                await MainTableService.LoadTableDataAsync();
                ToastService.ShowToast("Page updated by another admin.", ToastLevel.Notify);
            });

            await hubConnection.StartAsync();
        }

        public void Dispose()
        {
            _ = hubConnection.DisposeAsync();
        }
    }
}
