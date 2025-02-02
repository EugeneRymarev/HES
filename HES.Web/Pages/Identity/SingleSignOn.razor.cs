﻿using HES.Core.Constants;
using HES.Core.Enums;
using HES.Core.Exceptions;
using HES.Core.Interfaces;
using HES.Core.Models.Web.Identity;
using HES.Web.Components;
using HES.Web.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace HES.Web.Pages.Identity
{
    public partial class SingleSignOn : HESComponentBase
    {
        public IApplicationUserService ApplicationUserService { get; set; }
        public IFido2Service Fido2Service { get; set; }
        [Inject] public IIdentityApiClient IdentityApiClient { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public ILogger<SingleSignOn> Logger { get; set; }

        public AuthenticationStep AuthenticationStep { get; set; }
        public SecurityKeySignInModel SecurityKeySignInModel { get; set; } = new SecurityKeySignInModel();
        public UserEmailModel UserEmailModel { get; set; } = new UserEmailModel();
        public ValidationErrorMessage ValidationErrorMessage { get; set; }
        public Button Button { get; set; }
        public string ReturnUrl { get; set; }

        protected override void OnInitialized()
        {
            try
            {
                ApplicationUserService = ScopedServices.GetRequiredService<IApplicationUserService>();
                Fido2Service = ScopedServices.GetRequiredService<IFido2Service>();

                ReturnUrl = NavigationManager.GetQueryValue("returnUrl") ?? Routes.SingleSignOn;

                SetInitialized();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                SetLoadFailed(ex.Message);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("setFocus", "email");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.Message);
                }
            }
        }

        private async Task NextAsync()
        {
            try
            {
                await Button.SpinAsync(async () =>
                {
                    var user = await ApplicationUserService.GetUserByEmailAsync(UserEmailModel.Email);
                    if (user == null)
                    {
                        ValidationErrorMessage.DisplayError(nameof(UserEmailModel.Email), HESException.GetMessage(HESCode.UserNotFound));
                        return;
                    }

                    await SignInWithSecurityKeyAsync();
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                SetErrorMessage(ex.Message);
            }
        }

        private async Task SignInWithSecurityKeyAsync()
        {
            try
            {
                AuthenticationStep = AuthenticationStep.SecurityKeyAuthentication;

                SecurityKeySignInModel.AuthenticatorAssertionRawResponse = await Fido2Service.MakeAssertionRawResponse(UserEmailModel.Email, JSRuntime);
                var response = await IdentityApiClient.LoginWithFido2Async(SecurityKeySignInModel);
                response.ThrowIfFailed();

                NavigationManager.NavigateTo(ReturnUrl, true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                AuthenticationStep = AuthenticationStep.SecurityKeyError;
            }
        }

        private async Task TryAgainAsync()
        {
            await SignInWithSecurityKeyAsync();
        }

        private void BackToEmailValidation()
        {
            AuthenticationStep = AuthenticationStep.EmailValidation;
        }
    }
}