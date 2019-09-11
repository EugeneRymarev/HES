using HES.Core.Interfaces;
using HES.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace HES.Web.Areas.Identity.Pages.Account.External
{
    [AllowAnonymous]
    public class InviteModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<LoginModel> _logger;

        public InviteModel(UserManager<ApplicationUser> userManager,
                            SignInManager<ApplicationUser> signInManager,
                            IEmployeeService employeeService,
                            ILogger<LoginModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _employeeService = employeeService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            public string Code { get; set; }
        }

        public IActionResult OnGet(string code = null, string email = null)
        {
            if (code == null)
            {
                return BadRequest("A code must be supplied for password reset.");
            }
            else
            {
                Input = new InputModel
                {
                    Code = code,
                    Email = email
                };
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                return BadRequest("Email address does not exist.");
            }

            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded)
            {
                var login_result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, false, lockoutOnFailure: true);
                if (login_result.Succeeded)
                {
                    await _employeeService.CreateSamlIdpAccountAsync(Input.Email, Input.Password, Request.Host.Value);
                    _logger.LogInformation($"SAML IdP User {user.Email} logged in.");
                    return LocalRedirect("~/Identity/Account/External");
                }
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }
    }
}