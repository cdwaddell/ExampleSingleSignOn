// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Modifications Copyright (c) C Daniel Waddell. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ExampleSingleSignOn.Data;

namespace ExampleSingleSignOn.Identity
{
    internal class CustomRoleManager: AspNetRoleManager<CustomRole>
    {
        public CustomRoleManager(IRoleStore<CustomRole> store, IEnumerable<IRoleValidator<CustomRole>> roleValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, ILogger<RoleManager<CustomRole>> logger, IHttpContextAccessor contextAccessor) : 
            base(store, roleValidators, keyNormalizer, errors, logger, contextAccessor)
        {
        }
    }
}