using HES.Core.Entities;
using HES.Core.Interfaces;
using HES.Core.Services;
using HES.Infrastructure;
using HES.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HES.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add Services
            services.AddScoped(typeof(IAsyncRepository<>), typeof(Repository<>));

            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IDeviceService, DeviceService>();
            services.AddScoped<ISharedAccountService, SharedAccountService>();
            services.AddScoped<ITemplateService, TemplateService>();
            services.AddScoped<ISettingsService, SettingsService>();
            services.AddScoped<IApplicationUserService, ApplicationUserService>();
            services.AddSingleton<IRemoteTaskService, RemoteTaskService>(s =>
            {
                var scope = s.CreateScope();
                var deviceAccountRepository = scope.ServiceProvider.GetService<IAsyncRepository<DeviceAccount>>();
                var deviceTaskRepository = scope.ServiceProvider.GetService<IAsyncRepository<DeviceTask>>();
                var deviceRepository = scope.ServiceProvider.GetService<IAsyncRepository<Device>>();
                var logger = scope.ServiceProvider.GetService<ILogger<RemoteTaskService>>();
                return new RemoteTaskService(deviceAccountRepository, deviceTaskRepository, deviceRepository, logger);
            });
            services.AddSingleton<IDataProtectionService, DataProtectionService>(s =>
            {
                var scope = s.CreateScope();
                var dataProtectionRepository = scope.ServiceProvider.GetService<IAsyncRepository<AppSettings>>();
                var dataProtectionProvider = scope.ServiceProvider.GetService<IDataProtectionProvider>();
                var logger = scope.ServiceProvider.GetService<ILogger<DataProtectionService>>();
                return new DataProtectionService(dataProtectionRepository, dataProtectionProvider, logger);
            });

            // Crypto
            services.AddTransient<IAesCryptography, AesCryptography>();
            // Email
            services.AddSingleton<IEmailSender, EmailSender>(i =>
                 new EmailSender(
                     Configuration["EmailSender:Host"],
                     Configuration.GetValue<int>("EmailSender:Port"),
                     Configuration.GetValue<bool>("EmailSender:EnableSSL"),
                     Configuration["EmailSender:UserName"],
                     Configuration["EmailSender:Password"]));

            // Cookie
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // Dismiss strong password
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 3;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredUniqueChars = 0;
                options.Password.RequireNonAlphanumeric = false;

            });

            // Database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(Configuration.GetConnectionString("DefaultConnection")));

            // Identity
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddDefaultUI(UIFramework.Bootstrap4)
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Mvc
            services.AddMvc()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AddPageRoute("/Employees/Index", "");
                    options.Conventions.AuthorizeFolder("/Employees");
                    options.Conventions.AuthorizeFolder("/SharedAccounts");
                    options.Conventions.AuthorizeFolder("/Templates");
                    options.Conventions.AuthorizeFolder("/Devices");
                    options.Conventions.AuthorizeFolder("/Settings");
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddSignalR();
            services.AddDataProtection();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseSignalR(routes =>
            {
                routes.MapHub<DeviceHub>("/deviceHub");
                routes.MapHub<AppHub>("/appHub");
            });
            app.UseMvc();
            app.UseStatusCodePages("text/html", "<h1>HTTP status code {0}</h1>");

            // Init administrator
            using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var userManager = scope.ServiceProvider.GetService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetService<RoleManager<IdentityRole>>();

                new ApplicationDbSeed(userManager, roleManager).Initialize();
            }
        }
    }
}