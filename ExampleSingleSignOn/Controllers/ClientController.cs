// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Modifications Copyright (c) C Daniel Waddell. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using ExampleSingleSignOn.Data;
using ExampleSingleSignOn.Models.Client;
using ExampleSingleSignOn.Services;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.Models;
using Client = IdentityServer4.EntityFramework.Entities.Client;
using ApiResource = IdentityServer4.EntityFramework.Entities.ApiResource;
using IdentityResource = IdentityServer4.EntityFramework.Entities.IdentityResource;

namespace ExampleSingleSignOn.Controllers
{
    [Route("[controller]")]
    public class ClientController : Controller
    {
        private readonly CustomContext _context;
        private readonly IMapper _mapper;

        public ClientController(CustomContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;
        }

        [Authorize(Policies.ManageClients)]
        [HttpGet("New", Name = "NewClient")]
        public IActionResult NewClient()
        {
            var model = new[] { new ClientViewModel { Editable = true } };

            SetupViewBag();

            return View(nameof(ManageClients), model);
        }

        [Authorize(Policies.ManageClients)]
        [HttpPost("Scopes", Name = "SaveScope")]
        public IActionResult SaveScope(ScopeInputViewModel model, [FromQuery] string parent = null)
        {
            switch (model.ScopeType)
            {
                case ScopeType.Identity:
                    _context.IdentityResources.Add(new IdentityResource
                    {
                        DisplayName = model.ScopeName,
                        Name = model.ScopeName,
                        Enabled = true
                    });
                    break;
                case ScopeType.ApiResource:
                    _context.ApiResources.Add(new ApiResource
                    {
                        Name = model.ScopeName,
                        DisplayName = model.ScopeName,
                        Enabled = true
                    });
                    break;
                case ScopeType.Api:
                    if(parent == null)
                        return RedirectToAction(nameof(ManageScopes), new { message = $"Parent scope must be specified" });

                    var api = _context.ApiResources.Include(x => x.Scopes)
                        .SingleOrDefault(x => x.Name == parent);

                    if(api == null)
                        return RedirectToAction(nameof(ManageScopes), new { message = $"Could not find api {parent}" });

                    if(api.Scopes.Any(x => x.Name == model.ScopeName))
                        return RedirectToAction(nameof(ManageScopes), new { message = $"Duplicate scope {model.ScopeName} found for {parent}" });

                    api.Scopes.Add(new ApiScope
                    {
                        Name = model.ScopeName,
                        DisplayName = model.ScopeName
                    });

                    break;
            }
            _context.SaveChanges();
            return RedirectToAction(nameof(ManageScopes), new {message = $"New {model.ScopeType} Scope {model.ScopeName} Created"});
        }
        
        [Authorize(Policies.ManageClients)]
        [HttpPost("Scopes/Secrets", Name = "SetSecret")]
        public IActionResult SetSecret(SecretInputViewModel model)
        {
            if(!ModelState.IsValid)
                return RedirectToAction(nameof(ManageScopes), new { message = $"Invalid Request" });

            var resource = _context.ApiResources
                .Include(ar => ar.Secrets)
                .SingleOrDefault(ar => ar.Name == model.ScopeName);

            if(resource == null)
                return RedirectToAction(nameof(ManageScopes), new { message = $"Could not find {model.ScopeName}" });

            foreach (var secret in resource.Secrets.Where(s => s.Expiration == null))
            {
                secret.Expiration = DateTime.UtcNow.AddDays(30);
            }

            resource.Secrets.Add(new ApiSecret
            {
                Value = model.Secret.Sha256()
            });

            _context.SaveChanges();
            
            return RedirectToAction(nameof(ManageScopes), new { message = $"Secret for {model.ScopeName} has been set, previous secret has 30 days" });
        }
        
        [Authorize(Policies.ManageClients)]
        [HttpPost("Scopes/Claims", Name = "SaveScopeClaim")]
        public IActionResult SaveScopeClaim(ScopeClaimInputViewModel model, [FromQuery] string parent = null)
        {
            switch (model.ScopeType)
            {
                case ScopeType.Identity:
                    var idScope = _context.IdentityResources.Include(x => x.UserClaims).SingleOrDefault(x => x.Name == model.ScopeName);
                    if (idScope == null)
                        return RedirectToAction(nameof(ManageScopes), new { message = $"ID scope {model.ScopeName} not found" });

                    if(idScope.UserClaims.Any(x => x.Type == model.ClaimName))
                        return RedirectToAction(nameof(ManageScopes), new { message = $"Duplicate claim {model.ClaimName} found for {model.ScopeName}" });

                    idScope.UserClaims.Add(new IdentityClaim
                    {
                        Type = model.ClaimName
                    });
                    break;
                case ScopeType.ApiResource:
                    var apiResourceScope = _context.ApiResources.Include(x => x.UserClaims).SingleOrDefault(x => x.Name == model.ScopeName);
                    if (apiResourceScope == null)
                        return RedirectToAction(nameof(ManageScopes), new { message = $"API scope {model.ScopeName} not found" });

                    if (apiResourceScope.UserClaims.Any(x => x.Type == model.ClaimName))
                        return RedirectToAction(nameof(ManageScopes), new { message = $"Duplicate claim {model.ClaimName} found for {model.ScopeName}" });

                    apiResourceScope.UserClaims.Add(new ApiResourceClaim()
                    {
                        Type = model.ClaimName
                    });
                    break;
                case ScopeType.Api:
                    if (parent == null)
                        return RedirectToAction(nameof(ManageScopes), new { message = $"Parent scope must be specified" });

                    var api = _context.ApiResources.Include(x => x.Scopes).ThenInclude(x => x.UserClaims)
                        .SingleOrDefault(x => x.Name == parent);

                    if (api == null)
                        return RedirectToAction(nameof(ManageScopes), new { message = $"Could not find api {parent}" });

                    var apiScope = api.Scopes.SingleOrDefault(x => x.Name == model.ScopeName);
                    if (apiScope == null)
                        return RedirectToAction(nameof(ManageScopes), new { message = $"API scope {model.ScopeName} not found" });

                    if (apiScope.UserClaims.Any(x => x.Type == model.ClaimName))
                        return RedirectToAction(nameof(ManageScopes), new { message = $"Duplicate claim {model.ClaimName} found for {model.ScopeName}" });

                    apiScope.UserClaims.Add(new ApiScopeClaim()
                    {
                        Type = model.ClaimName
                    });

                    break;
            }
            _context.SaveChanges();
            return RedirectToAction(nameof(ManageScopes), new { message = $"New {model.ScopeType} Scope Claim {model.ClaimName} Added to {model.ScopeName}" });
        }

        [Authorize(Policies.ManageClients)]
        [HttpGet("Scopes", Name = "Scopes")]
        public IActionResult ManageScopes(string message = null)
        {
            ViewBag.Message = message;

            var model = new ScopesViewModel
            {
                IdentityResources =
                    _context.IdentityResources
                        .Include(x => x.UserClaims)
                        .Select(x => new ScopeViewModel
                        {
                            ScopeName = x.Name,
                            ScopeType = ScopeType.Identity,
                            ClaimNames = x.UserClaims.Select(y => y.Type).ToArray()
                        })
                        .ToArray(),
                ApiResources =
                    _context.ApiResources
                        .Include(x => x.UserClaims)
                        .Include(x => x.Scopes)
                        .ThenInclude(x => x.UserClaims)
                        .Select(x => 
                            new ApiResourceViewModel
                            {
                                ApiResourceScope = new ScopeViewModel
                                {
                                    ScopeName = x.Name,
                                    ScopeType = ScopeType.ApiResource,
                                    ClaimNames = x.UserClaims.Select(y => y.Type).ToArray()
                                },
                                ApiScopes = x.Scopes.Select(y => 
                                    new ScopeViewModel
                                    {
                                        ScopeName = y.Name,
                                        ScopeType = ScopeType.Api,
                                        ClaimNames = y.UserClaims.Select(z => z.Type).ToArray()
                                    }
                                ).ToArray()
                            }
                        ).ToArray()
            };

            return View(model);
        }

        [Authorize(Policies.ManageClients)]
        [HttpGet("", Name = "Clients")]
        public IActionResult ManageClients(string message = null)
        {
            SetupViewBag();
            ClientViewModel[] clients;

            if (User.IsInRole("SuperAdmin"))
            {
                clients = ClientQuery
                    .AsEnumerable()
                    .Select(c =>
                    {
                        var model = _mapper.Map<ClientViewModel>(c);
                        model.Editable = true;
                        return model;
                    })
                    .ToArray();
            }
            else
            {
                clients = ClientQuery
                    .AsEnumerable()
                    .Select(c => _mapper.Map<ClientViewModel>(c))
                    .ToArray();
            }

            return View(clients);
        }

        [Authorize(Policies.ManageClients)]
        [HttpPost("", Name = "PostClient")]
        public IActionResult SaveClient(ClientViewModel client)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(Policies.ManageClients, new { message = "Invalid Client" });

            var existing = ClientQuery
                .SingleOrDefault(c => c.ClientId == client.ClientId);

            if (existing == null)
            {
                _context.Clients.Add(_mapper.Map<Client>(client));
                _context.SaveChanges();
            }
            else
                _mapper.Map(client, existing);

            _context.SaveChanges();

            return RedirectToAction(nameof(ManageClients));
        }

        [Authorize(Policies.ManageClients)]
        [HttpDelete("{clientId}", Name = "DeleteClient")]
        public IActionResult DeleteClient(string clientId = "")
        {
            var existing = ClientQuery
                .SingleOrDefault(c => c.ClientId == clientId);
            
            if (existing == null)
                return NotFound();

            _context.Clients.Remove(existing);
            _context.SaveChanges();

            return Ok();
        }

        private IQueryable<Client> ClientQuery
        {
            get
            {
                return _context.Clients
                    .Include(c => c.AllowedCorsOrigins)
                    .Include(c => c.AllowedScopes)
                    .Include(c => c.PostLogoutRedirectUris)
                    .Include(c => c.RedirectUris)
                    .Include(c => c.AllowedGrantTypes)
                    .Include(c => c.ClientSecrets)
                    .Include(c => c.IdentityProviderRestrictions);
            }
        }

        private static readonly ClientViewModel BlankClient = new ClientViewModel
        {
            AllowedGrantTypes = new List<string> { "" },
            AllowedScopes = new List<string> { "" },
            AllowedCorsOrigins = new List<string> { "" },
            RedirectUris = new List<string> { "" },
            PostLogoutRedirectUris = new List<string> { "" },
            Claims = new List<Claim> { new Claim("", "") },
            IdentityProviderRestrictions = new List<string> { "" },
            Editable = true
        };

        private void SetupViewBag()
        {
            var gt = new[]
            {
                new SelectListItem {Value = "", Text = ""},
                new SelectListItem {Value = GrantType.Implicit, Text = GrantType.Implicit},
                new SelectListItem {Value = GrantType.AuthorizationCode, Text = GrantType.AuthorizationCode},
                new SelectListItem {Value = GrantType.Hybrid, Text = GrantType.Hybrid},
                new SelectListItem {Value = GrantType.ClientCredentials, Text = GrantType.ClientCredentials},
                new SelectListItem {Value = GrantType.ResourceOwnerPassword, Text = GrantType.ResourceOwnerPassword}
            };

            var apiResources = _context.ApiResources
                .Select(s => new { Text = s.DisplayName, Value = s.Name });

            var identityResources = _context.IdentityResources
                .Select(s => new {Text = s.DisplayName, Value = s.Name});

            var scopeList = identityResources
                    .Union(apiResources)
                    .Distinct()
                    .OrderBy(x => x.Text)
                    .Select(x => new SelectListItem{Text = x.Text, Value = x.Value})
                    .ToList();


            scopeList.Insert(0, new SelectListItem { Value = "", Text = "" });
            ViewBag.Scopes = scopeList;

            ViewBag.BlankClient = BlankClient;

            ViewBag.GrantTypes = gt.ToArray();

            ViewBag.GrantTypesNoBlank = gt.Where(x => x.Value != "");
        }
    }
}