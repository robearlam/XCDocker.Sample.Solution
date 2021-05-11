﻿// © 2017 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.AdventureWorks
{
    /// <summary>
    /// Ensure Habitat catalog has been loaded.
    /// </summary>
    /// <seealso>
    ///     <cref>
    ///         Sitecore.Framework.Pipelines.PipelineBlock{System.String, System.String,
    ///         Sitecore.Commerce.Core.CommercePipelineExecutionContext}
    ///     </cref>
    /// </seealso>
    [PipelineDisplayName(AwConstants.InitializeCatalogBlock)]
    public class InitializeCatalogBlock : AsyncPolicyTriggerConditionalPipelineBlock<string, string>
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IImportCatalogsPipeline _importCatalogsPipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeCatalogBlock"/> class.
        /// </summary>
        /// <param name="hostingEnvironment">The hosting environment.</param>
        /// <param name="importCatalogsPipeline">The import catalogs pipeline.</param>
        public InitializeCatalogBlock(
            IWebHostEnvironment hostingEnvironment,
            IImportCatalogsPipeline importCatalogsPipeline)
        {
            _hostingEnvironment = hostingEnvironment;
            _importCatalogsPipeline = importCatalogsPipeline;
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the should not run policy trigger.
        /// </summary>
        /// <value>
        /// The should not run policy trigger.
        /// </value>
        public override string ShouldNotRunPolicyTrigger => "IgnoreSampleData";

        /// <summary>
        /// Executes the block.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public override async Task<string> RunAsync(string arg, CommercePipelineExecutionContext context)
        {
            var artifactSet = "Environment.AdventureWorks.Catalog-1.0";

            // Check if this environment has subscribed to this Artifact Set
            if (!context.GetPolicy<EnvironmentInitializationPolicy>().InitialArtifactSets.Contains(artifactSet))
            {
                return arg;
            }

            // Similar LocalizationEntity entities are imported from a zip archive file and from creating it automatically, therefore skip LocalizeEntityBlock in IPrepPersistEntityPipeline to prevent SQL constraint violation.
            context.CommerceContext.AddPolicyKeys(new[]
            {
                "IgnoreLocalizeEntity"
            });

            await ImportCatalogAsync(context).ConfigureAwait(false);

            // Remove the IgnoreLocalizeEntity, to enable localization for InitializeEnvironmentBundlesBlock
            context.CommerceContext.RemovePolicyKeys(new[]
            {
                "IgnoreLocalizeEntity"
            });

            return arg;
        }

        /// <summary>
        /// Import catalog from file.
        /// </summary>
        /// <param name="context">The context to execute <see cref="IImportCatalogsPipeline"/>.</param>
        /// <returns></returns>
        protected virtual async Task ImportCatalogAsync(CommercePipelineExecutionContext context)
        {
            using (var stream = new FileStream(GetPath("AdventureWorks.zip"), FileMode.Open, FileAccess.Read))
            {
                var file = new FormFile(stream, 0, stream.Length, stream.Name, stream.Name);

                var contextOptions = context.CommerceContext.PipelineContextOptions;

                var argument = new ImportCatalogsArgument(file, CatalogConstants.Replace)
                {
                    BatchSize = -1,
                    ErrorThreshold = 10
                };

                context.CommerceContext.Environment.SetPolicy(new AutoApprovePolicy());
                
                await _importCatalogsPipeline.RunAsync(argument, contextOptions).ConfigureAwait(false);

                context.CommerceContext.Environment.RemovePolicy(typeof(AutoApprovePolicy));

            }
        }

        private string GetPath(string fileName)
        {
            return Path.Combine(_hostingEnvironment.WebRootPath, "data", "Catalogs", fileName);
        }
    }
}
