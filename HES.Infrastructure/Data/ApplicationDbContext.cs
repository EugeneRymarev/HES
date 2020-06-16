﻿using HES.Core.Entities;
using HES.Core.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HES.Infrastructure
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HardwareVault>().HasIndex(x => x.MAC).IsUnique();
            modelBuilder.Entity<HardwareVault>().HasIndex(x => x.RFID).IsUnique();
            modelBuilder.Entity<Group>().HasIndex(x => x.Name).IsUnique();
            // Cascade remove, when removing Group
            modelBuilder.Entity<Group>().HasMany(x => x.GroupMemberships).WithOne(p => p.Group).HasForeignKey(p => p.GroupId).OnDelete(DeleteBehavior.Cascade);
            // Cascade remove, when removing Employee
            modelBuilder.Entity<Employee>().HasMany(x => x.GroupMemberships).WithOne(p => p.Employee).HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Employee>().HasMany(x => x.SoftwareVaults).WithOne(p => p.Employee).HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Employee>().HasMany(x => x.SoftwareVaultInvitations).WithOne(p => p.Employee).HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Employee>().HasMany(x => x.Accounts).WithOne(p => p.Employee).HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Employee>().HasMany(x => x.WorkstationEvents).WithOne(p => p.Employee).HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Employee>().HasMany(x => x.WorkstationSessions).WithOne(p => p.Employee).HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            // Set Unique 
            modelBuilder.Entity<Employee>().HasIndex(x => new { x.FirstName, x.LastName }).IsUnique();
            // Cascade remove, when removing LicenseOrder
            modelBuilder.Entity<LicenseOrder>().HasMany(x => x.HardwareVaultLicenses).WithOne(p => p.LicenseOrder).HasForeignKey(p => p.LicenseOrderId).OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<HardwareVault> HardwareVaults { get; set; }
        public DbSet<HardwareVaultActivation> HardwareVaultActivations { get; set; }
        public DbSet<HardwareVaultProfile> HardwareVaultProfiles { get; set; }
        public DbSet<HardwareVaultTask> HardwareVaultTasks { get; set; }
        public DbSet<HardwareVaultLicense> HardwareVaultLicenses { get; set; }
        public DbSet<LicenseOrder> LicenseOrders { get; set; }
        public DbSet<SoftwareVault> SoftwareVaults { get; set; }
        public DbSet<SoftwareVaultInvitation> SoftwareVaultInvitations { get; set; }
        public DbSet<SharedAccount> SharedAccounts { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<Workstation> Workstations { get; set; }
        public DbSet<WorkstationProximityVault> WorkstationProximityVaults { get; set; }
        public DbSet<WorkstationEvent> WorkstationEvents { get; set; }
        public DbSet<WorkstationSession> WorkstationSessions { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<DataProtection> DataProtection { get; set; }
        public DbSet<AppSettings> AppSettings { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMembership> GroupMemberships { get; set; }


        public DbQuery<SummaryByDayAndEmployee> SummaryByDayAndEmployee { get; set; }
        public DbQuery<SummaryByEmployees> SummaryByEmployees { get; set; }
        public DbQuery<SummaryByDepartments> SummaryByDepartments { get; set; }
        public DbQuery<SummaryByWorkstations> SummaryByWorkstations { get; set; }
    }
}