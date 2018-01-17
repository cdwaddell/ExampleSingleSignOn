// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Modifications Copyright (c) C Daniel Waddell. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using IdentityServer4.Models;

namespace ExampleSingleSignOn.Models.Client
{
    public class ClientViewModel
    {
        public bool Enabled { get; set; } = true;

        public string ClientSecret { get; set; }

        [Required]
        [Display(Name = "Client ID")]
        public string ClientId { get; set; }

        [Required]
        [Display(Name = "Display Name")]
        public string ClientName { get; set; }

        [Display(Name = "Url")]
        public string ClientUri { get; set; }

        [Display(Name = "Logo Url")]
        public string LogoUri { get; set; }

        [Display(Name = "Logout Url")]
        public string LogoutUri { get; set; }

        [Display(Name = "Protocol")]
        public string ProtocolType { get; set; } = "oidc";

        [Display(Name = "Require Secret")]
        public bool RequireClientSecret { get; set; } = true;

        [Display(Name = "Require Consent")]
        public bool RequireConsent { get; set; } = true;

        [Display(Name = "Allow Remember Consent")]
        public bool AllowRememberConsent { get; set; } = true;

        [Display(Name = "Require Session Logout")]
        public bool LogoutSessionRequired { get; set; } = true;

        [Display(Name = "Enable Local Login")]
        public bool EnableLocalLogin { get; set; } = true;

        [Display(Name = "Prefix Claims")]
        public bool PrefixClientClaims { get; set; } = true;

        [Display(Name = "ID Token Life")]
        public int IdentityTokenLifetime { get; set; } = 300;

        [Display(Name = "Access Token Life")]
        public int AccessTokenLifetime { get; set; } = 3600;

        [Display(Name = "Auth Code Life")]
        public int AuthorizationCodeLifetime { get; set; } = 300;

        [Display(Name = "Absolute Refresh Life")]
        public int AbsoluteRefreshTokenLifetime { get; set; } = 2592000;

        [Display(Name = "Sliding Refresh Life")]
        public int SlidingRefreshTokenLifetime { get; set; } = 1296000;

        [Display(Name = "Refresh Token Use")]
        public TokenUsage RefreshTokenUsage { get; set; } = TokenUsage.OneTimeOnly;

        [Display(Name = "Refresh Token Expire")]
        public TokenExpiration RefreshTokenExpiration { get; set; } = TokenExpiration.Absolute;

        [Display(Name = "Access Token Type")]
        public AccessTokenType AccessTokenType { get; set; } = AccessTokenType.Jwt;

        [Display(Name = "Added Claims")]
        public List<Claim> Claims { get; set; } = new List<Claim>();

        [Display(Name = "Allowed Scopes")]
        public List<string> AllowedScopes { get; set; } = new List<string>();

        [Display(Name = "Redirect Urls")]
        public List<string> RedirectUris { get; set; } = new List<string>();

        [Display(Name = "Post Logout Urls")]
        public List<string> PostLogoutRedirectUris { get; set; } = new List<string>();

        [Display(Name = "Identity Restrictions")]
        public List<string> IdentityProviderRestrictions { get; set; } = new List<string>();

        [Display(Name = "Allowed Cors Origins")]
        public List<string> AllowedCorsOrigins { get; set; } = new List<string>();

        [Display(Name = "Allowed Grant Types")]
        public List<string> AllowedGrantTypes { get; set; } = new List<string> { GrantType.Implicit };

        [Display(Name = "Require Proof Key")]
        public bool RequirePkce { get; set; }

        [Display(Name = "Allow Plain Text Proof")]
        public bool AllowPlainTextPkce { get; set; }

        [Display(Name = "Allow Token in Browser")]
        public bool AllowAccessTokensViaBrowser { get; set; }

        [Display(Name = "Allow Offline Access")]

        public bool AllowOfflineAccess { get; set; }

        [Display(Name = "Allow Include User in ID")]
        public bool AlwaysIncludeUserClaimsInIdToken { get; set; }

        [Display(Name = "Update Token on Refresh")]
        public bool UpdateAccessTokenClaimsOnRefresh { get; set; }

        [Display(Name = "Include JWT ID")]
        public bool IncludeJwtId { get; set; }

        [Display(Name = "Always Send Claims")]
        public bool AlwaysSendClientClaims { get; set; }

        public bool Editable { get; set; }

    }
}