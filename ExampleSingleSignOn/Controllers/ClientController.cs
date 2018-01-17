// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Modifications Copyright (c) C Daniel Waddell. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


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
using IdentityServer4.Models;
using Client = IdentityServer4.EntityFramework.Entities.Client;

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

        [Authorize("ManageClients")]
        [HttpGet("New", Name = "NewClient")]
        public IActionResult NewClient()
        {
            var model = new[] { new ClientViewModel { Editable = true } };

            SetupViewBag();

            return View(nameof(ManageClients), model);
        }

        [Authorize("ManageClients")]
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

        [Authorize("ManageClients")]
        [HttpPost("", Name = "PostClient")]
        public IActionResult SaveClient(ClientViewModel client)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("ManageClients", new { message = "Invalid Client" });

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

        [Authorize("ManageClients")]
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