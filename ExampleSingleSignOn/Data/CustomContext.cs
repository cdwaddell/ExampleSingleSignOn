// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Modifications Copyright (c) C Daniel Waddell. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Extensions;
using IdentityServer4.EntityFramework.Interfaces;
using IdentityServer4.EntityFramework.Options;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExampleSingleSignOn.Data
{
    public class CustomContext: 
        IdentityDbContext<CustomUser, CustomRole, string>, 
        IConfigurationDbContext, IPersistedGrantDbContext
    {
        private readonly ConfigurationStoreOptions _configStoreOptions;
        private readonly OperationalStoreOptions _operStoreOptions;

        public CustomContext(DbContextOptions<CustomContext> dbOptions, ConfigurationStoreOptions configStoreOptions, OperationalStoreOptions operStoreOptions) 
            : base(dbOptions)
        {
            _configStoreOptions = configStoreOptions;
            _operStoreOptions = operStoreOptions;
        }

        public Task<int> SaveChangesAsync()
        {
            return Task.FromResult(SaveChanges());
        }

        public DbSet<PersistedGrant> PersistedGrants { get; set; }

        public DbSet<Client> Clients { get; set; }
        public DbSet<IdentityResource> IdentityResources { get; set; }
        public DbSet<ApiResource> ApiResources { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            builder.ConfigureClientContext(_configStoreOptions);
            builder.ConfigureResourcesContext(_configStoreOptions);
            builder.ConfigurePersistedGrantContext(_operStoreOptions);
        }
    }
}