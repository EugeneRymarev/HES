﻿using HES.Core.Entities;
using HES.Core.Models.Web.Audit;
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
            // HardwareVault
            modelBuilder.Entity<HardwareVault>().HasIndex(x => x.MAC).IsUnique();
            modelBuilder.Entity<HardwareVault>().HasIndex(x => x.RFID).IsUnique();
            modelBuilder.Entity<Group>().HasIndex(x => x.Name).IsUnique();
            // Group           
            modelBuilder.Entity<Group>().HasMany(x => x.GroupMemberships).WithOne(p => p.Group).HasForeignKey(p => p.GroupId).OnDelete(DeleteBehavior.Cascade);
            // Employee
            modelBuilder.Entity<Employee>().HasIndex(x => new { x.FirstName, x.LastName }).IsUnique();
            modelBuilder.Entity<Employee>().HasMany(x => x.GroupMemberships).WithOne(p => p.Employee).HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Employee>().HasMany(x => x.SoftwareVaults).WithOne(p => p.Employee).HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Employee>().HasMany(x => x.SoftwareVaultInvitations).WithOne(p => p.Employee).HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Employee>().HasMany(x => x.Accounts).WithOne(p => p.Employee).HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Employee>().HasMany(x => x.WorkstationEvents).WithOne(p => p.Employee).HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Employee>().HasMany(x => x.WorkstationSessions).WithOne(p => p.Employee).HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            // LicenseOrder
            modelBuilder.Entity<LicenseOrder>().HasMany(x => x.HardwareVaultLicenses).WithOne(p => p.LicenseOrder).HasForeignKey(p => p.LicenseOrderId).OnDelete(DeleteBehavior.Cascade);
            // Account
            modelBuilder.Entity<Account>().HasMany(x => x.WorkstationEvents).WithOne(p => p.Account).HasForeignKey(p => p.AccountId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Account>().HasMany(x => x.WorkstationSessions).WithOne(p => p.Account).HasForeignKey(p => p.AccountId).OnDelete(DeleteBehavior.SetNull);
            // Workstation
            modelBuilder.Entity<Workstation>().HasMany(x => x.WorkstationEvents).WithOne(p => p.Workstation).HasForeignKey(p => p.WorkstationId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Workstation>().HasMany(x => x.WorkstationSessions).WithOne(p => p.Workstation).HasForeignKey(p => p.WorkstationId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Workstation>().HasMany(x => x.WorkstationProximityVaults).WithOne(p => p.Workstation).HasForeignKey(p => p.WorkstationId).OnDelete(DeleteBehavior.Cascade);
            // Department
            modelBuilder.Entity<Department>().HasMany(x => x.Employees).WithOne(p => p.Department).HasForeignKey(p => p.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Department>().HasMany(x => x.Workstations).WithOne(p => p.Department).HasForeignKey(p => p.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Department>().HasMany(x => x.WorkstationEvents).WithOne(p => p.Department).HasForeignKey(p => p.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Department>().HasMany(x => x.WorkstationSessions).WithOne(p => p.Department).HasForeignKey(p => p.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            // Position
            modelBuilder.Entity<Position>().HasMany(x => x.Employees).WithOne(p => p.Position).HasForeignKey(p => p.PositionId).OnDelete(DeleteBehavior.SetNull);
            // Summary
            modelBuilder.Entity<SummaryByDayAndEmployee>().HasNoKey().ToView(null);
            modelBuilder.Entity<SummaryByEmployees>().HasNoKey().ToView(null);
            modelBuilder.Entity<SummaryByDepartments>().HasNoKey().ToView(null);
            modelBuilder.Entity<SummaryByWorkstations>().HasNoKey().ToView(null);

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
        public DbSet<FidoStoredCredential> FidoStoredCredential { get; set; }
        public DbSet<GroupMembership> GroupMemberships { get; set; }
        public DbSet<SummaryByDayAndEmployee> SummaryByDayAndEmployee { get; set; }
        public DbSet<SummaryByEmployees> SummaryByEmployees { get; set; }
        public DbSet<SummaryByDepartments> SummaryByDepartments { get; set; }
        public DbSet<SummaryByWorkstations> SummaryByWorkstations { get; set; }
    }
}