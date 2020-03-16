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
            modelBuilder.Entity<Device>().HasIndex(x => x.MAC).IsUnique();
            modelBuilder.Entity<Device>().HasIndex(x => x.RFID).IsUnique();
            modelBuilder.Entity<Group>().HasIndex(x => x.Name).IsUnique();
            // Cascade remove all Group Memberships when removing Group
            modelBuilder.Entity<Group>().HasMany(x => x.GroupMemberships).WithOne(p => p.Group).HasForeignKey(p => p.GroupId).OnDelete(DeleteBehavior.Cascade);
            // Cascade remove all Group Memberships when removing Employee
            modelBuilder.Entity<Employee>().HasMany(x => x.GroupMemberships).WithOne(p => p.Employee).HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            // Set Unique 
            modelBuilder.Entity<Employee>().HasIndex(x => new { x.FirstName, x.LastName }).IsUnique();
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Workstation> Workstations { get; set; }
        public DbSet<SharedAccount> SharedAccounts { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<DeviceTask> DeviceTasks { get; set; }
        public DbSet<ProximityDevice> ProximityDevices { get; set; }
        public DbSet<WorkstationEvent> WorkstationEvents { get; set; }
        public DbSet<WorkstationSession> WorkstationSessions { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<DeviceAccessProfile> DeviceAccessProfiles { get; set; }
        public DbSet<DataProtection> DataProtection { get; set; }
        public DbSet<DeviceLicense> DeviceLicenses { get; set; }
        public DbSet<LicenseOrder> LicenseOrders { get; set; }
        public DbSet<AppSettings> AppSettings { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMembership> GroupMemberships { get; set; }


        public DbQuery<SummaryByDayAndEmployee> SummaryByDayAndEmployee { get; set; }
        public DbQuery<SummaryByEmployees> SummaryByEmployees { get; set; }
        public DbQuery<SummaryByDepartments> SummaryByDepartments { get; set; }
        public DbQuery<SummaryByWorkstations> SummaryByWorkstations { get; set; }
    }
}