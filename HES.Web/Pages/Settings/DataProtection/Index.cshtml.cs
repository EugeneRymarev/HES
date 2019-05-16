﻿using HES.Core.Interfaces;
using HES.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace HES.Web.Pages.Settings.DataProtection
{
    public class IndexModel : PageModel
    {
        private readonly IDataProtectionService _dataProtectionService;
        private readonly ILogger<IndexModel> _logger;

        public ProtectionStatus Status { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public CurrentPasswordModel CurrentPassword { get; set; }
        public NewPasswordModel NewPassword { get; set; }
        public ChangePasswordModel ChangePassword { get; set; }

        public class CurrentPasswordModel
        {
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }
        }

        public class NewPasswordModel
        {
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public class ChangePasswordModel
        {
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string OldPassword { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public IndexModel(IDataProtectionService dataProtectionService, ILogger<IndexModel> logger)
        {
            _dataProtectionService = dataProtectionService;
            _logger = logger;
        }

        public void OnGet()
        {
            Status = _dataProtectionService.GetStatus();
        }

        public async Task<IActionResult> OnPostEnableDataProtectionAsync(NewPasswordModel NewPassword)
        {
            if (NewPassword == null)
            {
                _logger.LogWarning("NewPassword == null");
                return NotFound();
            }

            try
            {
                await _dataProtectionService.EnableDataProtectionAsync(NewPassword.Password);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                _logger.LogError(ex.Message);
            }

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostDisableDataProtectionAsync(CurrentPasswordModel CurrentPassword)
        {
            if (CurrentPassword == null)
            {
                _logger.LogWarning("CurrentPassword == null");
                return NotFound();
            }

            try
            {
                await _dataProtectionService.DisableDataProtectionAsync(CurrentPassword.Password);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                _logger.LogError(ex.Message);
            }

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostActivateDataProtectionAsync(CurrentPasswordModel CurrentPassword)
        {
            if (CurrentPassword == null)
            {
                _logger.LogWarning("CurrentPassword == null");
                return NotFound();
            }

            try
            {
                await _dataProtectionService.ActivateDataProtectionAsync(CurrentPassword.Password);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                _logger.LogError(ex.Message);
            }

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostChangePwdDataProtectionAsync(ChangePasswordModel ChangePassword)
        {
            if (ChangePassword == null)
            {
                _logger.LogWarning("ChangePassword == null");
                return NotFound();
            }

            try
            {
                await _dataProtectionService.ChangeDataProtectionPasswordAsync(ChangePassword.OldPassword, ChangePassword.NewPassword);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                _logger.LogError(ex.Message);
            }

            return RedirectToPage("./Index");
        }
    }
}