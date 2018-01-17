// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Modifications Copyright (c) C Daniel Waddell. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ExampleSingleSignOn.Data;

namespace ExampleSingleSignOn.Identity
{
    internal class CustomUserStore:UserStore<CustomUser, CustomRole, CustomContext, string>
    {
        public CustomUserStore(CustomContext context, IdentityErrorDescriber describer = null) : 
            base(context, describer)
        {
        }
    }
}