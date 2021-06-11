﻿using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    sealed class GithubService
    {
        private readonly GithubMetaService githubMetaService;
        private readonly GithubDelegate githubDelegate;
        private readonly ILogger<GithubService> logger;

        public GithubService(
            GithubMetaService githubMetaService,
            GithubDelegate githubDelegate,
            ILogger<GithubService> logger)
        {
            this.githubMetaService = githubMetaService;
            this.githubDelegate = githubDelegate;
            this.logger = logger;
        }

        public async Task ScanAddressAsync(CancellationToken cancellationToken = default)
        {
            var meta = await this.githubMetaService.GetMetaAsync(cancellationToken);
            if (meta != null)
            {
                var contexts = new List<GithubContext>();
                var scanTasks = this.GetMetaScanTasks(meta, contexts);
                await Task.WhenAll(scanTasks);

                var sortedContexts = contexts
                    .Where(item => item.HttpElapsed != null)
                    .OrderBy(item => item.HttpElapsed);

                foreach (var context in sortedContexts)
                {
                    this.logger.LogInformation($"{context.Address} {context.HttpElapsed}");
                }
            }

            this.logger.LogInformation("扫描结束");
        }


        private IEnumerable<Task> GetMetaScanTasks(Meta meta, IList<GithubContext> contexts)
        {
            foreach (var address in meta.ToIPv4Address())
            {
                var context = new GithubContext
                {
                    Address = address,
                };
                contexts.Add(context);
                yield return this.githubDelegate(context);
            }
        }
    }
}
