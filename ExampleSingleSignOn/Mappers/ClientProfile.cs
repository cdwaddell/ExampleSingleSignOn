using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using ExampleSingleSignOn.Models.Client;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.Models;
using Claim = System.Security.Claims.Claim;
using Client = IdentityServer4.EntityFramework.Entities.Client;

namespace ExampleSingleSignOn.Mappers
{

    public class ClientMapperProfile : Profile
    {
        public ClientMapperProfile()
        {
            //turn objects into flat arrays
            CreateMap<Client, ClientViewModel>(MemberList.Destination)
                .ForMember(x => x.ClientSecret, opt => opt.MapFrom(src => ""))
                .ForMember(x => x.AllowedGrantTypes,
                    opt => opt.MapFrom(src => src.AllowedGrantTypes
                        .Where(x => !string.IsNullOrWhiteSpace(x.GrantType))
                        .Select(x => x.GrantType))
                )
                .ForMember(x => x.RedirectUris,
                    opt => opt.MapFrom(src => src.RedirectUris
                        .Where(x => !string.IsNullOrWhiteSpace(x.RedirectUri))
                        .Select(x => x.RedirectUri))
                )
                .ForMember(x => x.PostLogoutRedirectUris,
                    opt => opt.MapFrom(src => src.PostLogoutRedirectUris
                        .Where(x => !string.IsNullOrWhiteSpace(x.PostLogoutRedirectUri))
                        .Select(x => x.PostLogoutRedirectUri))
                )
                .ForMember(x => x.AllowedScopes,
                    opt => opt.MapFrom(src => src.AllowedScopes
                        .Where(x => !string.IsNullOrWhiteSpace(x.Scope))
                        .Select(x => x.Scope))
                )
                .ForMember(x => x.Claims,
                    opt => opt.MapFrom(src => src.Claims
                        .Where(x => !string.IsNullOrWhiteSpace(x.Type))
                        .Select(x => new Claim(x.Type, x.Value)))
                )
                .ForMember(x => x.IdentityProviderRestrictions,
                    opt => opt.MapFrom(src => src.IdentityProviderRestrictions
                        .Where(x => !string.IsNullOrWhiteSpace(x.Provider))
                        .Select(x => x.Provider))
                )
                .ForMember(x => x.AllowedCorsOrigins,
                    opt => opt.MapFrom(src => src.AllowedCorsOrigins
                        .Where(x => !string.IsNullOrWhiteSpace(x.Origin))
                        .Select(x => x.Origin))
                );

            //Turn flat arrays into objects
            CreateMap<ClientViewModel, Client>(MemberList.Source)
                .ForMember(x => x.AllowedGrantTypes,
                    opt => opt.MapFrom(
                        src => src.AllowedGrantTypes
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => new ClientGrantType { GrantType = x })
                    )
                )
                .ForMember(x => x.RedirectUris,
                    opt => opt.MapFrom(
                        src => src.RedirectUris
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => new ClientRedirectUri { RedirectUri = x })
                    )
                )
                .ForMember(x => x.PostLogoutRedirectUris,
                    opt => opt.MapFrom(src =>
                        src.PostLogoutRedirectUris
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = x })
                    )
                )
                .ForMember(x => x.AllowedScopes,
                    opt => opt.MapFrom(
                        src => src.AllowedScopes
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => new ClientScope { Scope = x })
                    )
                )
                .ForMember(x => x.Claims,
                    opt => opt.MapFrom(
                        src => src.Claims
                        .Where(x => !string.IsNullOrWhiteSpace(x.Type))
                        .Select(x => new ClientClaim { Type = x.Type, Value = x.Value })
                    )
                )
                .ForMember(x => x.IdentityProviderRestrictions,
                    opt => opt.MapFrom(
                        src => src.IdentityProviderRestrictions
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => new ClientIdPRestriction { Provider = x })
                    )
                )
                .ForMember(x => x.AllowedCorsOrigins,
                    opt => opt.MapFrom(
                        src => src.AllowedCorsOrigins
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => new ClientCorsOrigin { Origin = x })
                    )
                )
                .AfterMap((src, dest) =>
                {
                    if (dest.ClientSecrets == null)
                        dest.ClientSecrets = new List<ClientSecret>();

                    //Handle the client secret
                    if (!string.IsNullOrWhiteSpace(src.ClientSecret))
                    {
                        //expire existing secrets that do not expire
                        foreach (var secret in dest.ClientSecrets.Where(cs => cs.Expiration == null))
                        {
                            //you only have 30 days!
                            secret.Expiration = DateTime.Now.AddDays(30);
                        }

                        dest.ClientSecrets.Add(new ClientSecret { Value = src.ClientSecret.Sha256(), Type = "SharedSecret" });
                    }

                    //Finally reconstruct based on which items were updates, deletes, and additions
                    RemoveDupesAndDeletes(
                        dest.AllowedCorsOrigins,
                        x => x.Origin,
                        g => g.Id == 0);

                    RemoveDupesAndDeletes(
                        dest.IdentityProviderRestrictions,
                        x => x.Provider,
                        g => g.Id == 0);

                    RemoveDupesAndDeletes(
                        dest.Claims,
                        x => new { x.Type, x.Value },
                        g => g.Id == 0);

                    RemoveDupesAndDeletes(
                        dest.AllowedScopes,
                        x => x.Scope,
                        g => g.Id == 0);

                    RemoveDupesAndDeletes(
                        dest.PostLogoutRedirectUris,
                        x => x.PostLogoutRedirectUri,
                        g => g.Id == 0);

                    RemoveDupesAndDeletes(
                        dest.RedirectUris,
                        x => x.RedirectUri,
                        g => g.Id == 0);

                    RemoveDupesAndDeletes(
                        dest.AllowedGrantTypes,
                        x => x.GrantType,
                        g => g.Id == 0);
                });
        }

        private static void RemoveDupesAndDeletes<T>(ICollection<T> dbset, Func<T, object> uniqueSelector, Func<T, bool> newCheck)
        {
            //group by unique field(s)
            var cors = dbset.GroupBy(uniqueSelector).ToArray();
            //remove old items that were not added back on (two exist)
            foreach (var x in cors.Where(g => g.Count() == 1 && !newCheck(g.First())).SelectMany(g => g))
                dbset.Remove(x);
            //remove new items where we already had an old item (wasn't updated)
            foreach (var x in cors.Where(g => g.Count() != 1).SelectMany(g => g.Where(newCheck)))
                dbset.Remove(x);
            //leave new added items on
        }
    }
}
