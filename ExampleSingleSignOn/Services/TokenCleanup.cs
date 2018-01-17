// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Modifications Copyright (c) C Daniel Waddell. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Interfaces;
using IdentityServer4.EntityFramework.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExampleSingleSignOn.Services
{
    internal class TokenCleanup
    {
        private readonly ILogger<TokenCleanup> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval;
        private CancellationTokenSource _source;

        public TokenCleanup(IServiceProvider serviceProvider, ILogger<TokenCleanup> logger, OperationalStoreOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (options.TokenCleanupInterval < 1)
                throw new ArgumentException("interval must be more than 1 second");
            _serviceProvider = serviceProvider;
            _logger = logger;
            _interval = TimeSpan.FromSeconds(options.TokenCleanupInterval);
        }

        public void Start()
        {
            Start(CancellationToken.None);
        }

        public void Start(CancellationToken cancellationToken)
        {
            if (_source != null)
                throw new InvalidOperationException("Already started. Call Stop first.");
            _logger.LogDebug("Starting token cleanup", Array.Empty<object>());
            _source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task.Factory.StartNew(() => StartInternal(_source.Token), cancellationToken);
        }

        public void Stop()
        {
            if (_source == null)
                throw new InvalidOperationException("Not started. Call Start first.");
            _logger.LogDebug("Stopping token cleanup", Array.Empty<object>());
            _source.Cancel();
            _source = null;
        }

        private async Task StartInternal(CancellationToken cancellationToken)
        {
            TokenCleanup tokenCleanup = this;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(tokenCleanup._interval, cancellationToken);
                }
                catch (TaskCanceledException )
                {
                    tokenCleanup._logger.LogDebug("TaskCanceledException. Exiting.", Array.Empty<object>());
                    return;
                }
                catch (Exception ex)
                {
                    tokenCleanup._logger.LogError("Task.Delay exception: {0}. Exiting.", (object)ex.Message);
                    return;
                }
                if (cancellationToken.IsCancellationRequested)
                {
                    tokenCleanup._logger.LogDebug("CancellationRequested. Exiting.", Array.Empty<object>());
                    return;
                }
                tokenCleanup.ClearTokens();
            }
            tokenCleanup._logger.LogDebug("CancellationRequested. Exiting.", Array.Empty<object>());
        }

        public void ClearTokens()
        {
            try
            {
                _logger.LogTrace("Querying for tokens to clear", Array.Empty<object>());
                using (IServiceScope scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
                {
                    using (IPersistedGrantDbContext service = scope.ServiceProvider.GetService<IPersistedGrantDbContext>())
                    {
                        PersistedGrant[] array = service.PersistedGrants.Where(x => x.Expiration < (DateTimeOffset?)DateTimeOffset.UtcNow).ToArray();
                        _logger.LogInformation("Clearing {tokenCount} tokens", (object)array.Length);
                        if (array.Length == 0)
                            return;
                        service.PersistedGrants.RemoveRange(array);
                        service.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception clearing tokens: {exception}", (object)ex.Message);
            }
        }
    }
}