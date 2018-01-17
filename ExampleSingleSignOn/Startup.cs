// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Modifications Copyright (c) C Daniel Waddell. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4;
using IdentityServer4.EntityFramework.Interfaces;
using IdentityServer4.EntityFramework.Options;
using IdentityServer4.EntityFramework.Services;
using IdentityServer4.EntityFramework.Stores;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using AutoMapper;
using ExampleSingleSignOn.Data;
using ExampleSingleSignOn.Identity;
using ExampleSingleSignOn.Services;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using SendGrid;

namespace ExampleSingleSignOn
{
    public class Startup
    {
        public static IHostingEnvironment Environment { get; set; }
        public static IConfiguration Configuration { get; set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables("x_");

            Configuration = builder.Build();
            Environment = env;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Configuration);
            services.AddSingleton(Environment);

            services.AddMvc();
            services.AddMemoryCache();

            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            var connectionString = Configuration.GetConnectionString("ExampleSingleSignOn");
            
            services.AddDbContext<CustomContext>(builder =>
                builder.UseSqlServer(connectionString, options =>
                {
                    options.MigrationsAssembly(migrationsAssembly);
                    options.EnableRetryOnFailure(); //needed on azure
                })
            );

            services.AddIdentity<CustomUser, CustomRole>(options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireDigit = true;
                    
                    options.SignIn.RequireConfirmedEmail = true;

                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.DefaultLockoutTimeSpan = new TimeSpan(0, 5, 0);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                })
                .AddEntityFrameworkStores<CustomContext>()
                .AddDefaultTokenProviders()
                .AddUserStore<CustomUserStore>()
                .AddUserManager<CustomUserManager>()
                .AddSignInManager<CustomSignInManager>()
                .AddRoleStore<CustomRoleStore>()
                .AddRoleManager<CustomRoleManager>();

            var idServerBuilder = services.AddIdentityServer()
                .AddInMemoryCaching()
                .AddClientStoreCache<ClientStore>()
                .AddResourceStoreCache<ResourceStore>()
                .AddCorsPolicyCache<CorsPolicyService>()
                .AddAspNetIdentity<CustomUser>();

            var certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            certStore.Open(OpenFlags.ReadOnly);
            var certCollection = certStore.Certificates.Find(
                X509FindType.FindByThumbprint,
                Configuration["CertificateThumbprint"],
                false
            );

            if (certCollection.Count == 0)
                idServerBuilder.AddDeveloperSigningCredential();
            else
                idServerBuilder.AddSigningCredential(certCollection[0]);

            //var oldCerts = certStore.Certificates.Find(...);
            //foreach (var cert in oldCerts)
            //{
            //    idServerBuilder.AddValidationKey(cert);
            //}

            idServerBuilder.Services.AddSingleton(new ConfigurationStoreOptions());
            idServerBuilder.Services.AddScoped<IConfigurationDbContext, CustomContext>();

            idServerBuilder.Services.AddTransient<IClientStore, ClientStore>();
            idServerBuilder.Services.AddTransient<IResourceStore, ResourceStore>();
            idServerBuilder.Services.AddTransient<ICorsPolicyService, CorsPolicyService>();

            idServerBuilder.Services.AddSingleton<TokenCleanup>();
            idServerBuilder.Services.AddSingleton<IHostedService, TokenCleanupHost>();

            idServerBuilder.Services.AddSingleton(new OperationalStoreOptions());
            idServerBuilder.Services.AddScoped<IPersistedGrantDbContext, CustomContext>();
            
            idServerBuilder.Services.AddTransient<IPersistedGrantStore, PersistedGrantStore>();

            services.AddScoped<IEmailSender, EmailSender>();
            services.AddSingleton<ISendGridClient>(new SendGridClient(Configuration["EmailSender:ApiKey"]));

            services.AddOptions();
            services.Configure<EmailSenderOptions>(Configuration.GetSection("EmailSender"));
            
            services.AddAutoMapper(GetType());

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ManageClients", policy =>
                    policy.Requirements.Add(new RolesAuthorizationRequirement(new []{"Admin"})));
            });

            services.AddAuthentication()
                .AddGoogle("Google", options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    options.ClientId = Configuration["Authentication:Google:ClientId"];
                    options.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
                })
                .AddTwitter("Twitter", options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    options.ConsumerKey = Configuration["Authentication:Twitter:ConsumerKey"];
                    options.ConsumerSecret = Configuration["Authentication:Twitter:ConsumerSecret"];
                })
                .AddFacebook("Facebook", options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    options.AppId = Configuration["Authentication:Facebook:AppId"];
                    options.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
                })
                .AddMicrosoftAccount("Microsoft", options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    options.ClientId = Configuration["Authentication:Microsoft:ApplicationId"];
                    options.ClientSecret = Configuration["Authentication:Microsoft:Password"];
                })
                .AddOAuth("Custom OAuth", "OAuth", options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.CallbackPath = "/signin-oauth";

                    options.AuthorizationEndpoint = Configuration["Authentication:OAuth:Authority"];
                    options.TokenEndpoint = Configuration["Authentication:OAuth:TokenEndpoint"];
                    options.UserInformationEndpoint = Configuration["Authentication:OAuth:UserInformationEndpoint"];

                    options.ClientId = Configuration["Authentication:OAuth:ClientId"];
                    options.ClientSecret = Configuration["Authentication:OAuth:ClientSecret"];
                })
                .AddOpenIdConnect("Custom OpenId", "OpenID Connect", options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.SignOutScheme = IdentityServerConstants.SignoutScheme;
                    options.CallbackPath = "/signin-openid";

                    options.Authority = Configuration["Authentication:OpenId:Authority"];
                    options.ClientId = Configuration["Authentication:OpenId:ClientId"];
                    options.ClientSecret = Configuration["Authentication:OpenId:ClientSecret"];

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                });
        }
        
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            
            app.UseIdentityServer();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    "default",
                    "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}

