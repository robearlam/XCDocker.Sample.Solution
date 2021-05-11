﻿// © 2015 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.AdventureWorks
{
    /// <summary>
    /// Defines a block which bootstraps pricing the AdventureWorks sample environment.
    /// </summary>
    /// <seealso>
    ///     <cref>
    ///         Sitecore.Framework.Pipelines.PipelineBlock{System.String, System.String,
    ///         Sitecore.Commerce.Core.CommercePipelineExecutionContext}
    ///     </cref>
    /// </seealso>
    [PipelineDisplayName(AwConstants.InitializeEnvironmentPricingBlock)]
    public class InitializeEnvironmentPricingBlock : AsyncPolicyTriggerConditionalPipelineBlock<string, string>
    {
        private readonly IAddPriceBookPipeline _addPriceBookPipeline;
        private readonly IAddPriceCardPipeline _addPriceCardPipeline;
        private readonly IAddPriceSnapshotPipeline _addPriceSnapshotPipeline;
        private readonly IAddPriceTierPipeline _addPriceTierPipeline;
        private readonly IAddPriceSnapshotTagPipeline _addPriceSnapshotTagPipeline;
        private readonly IPersistEntityPipeline _persistEntityPipeline;
        private readonly IAssociateCatalogToBookPipeline _associateCatalogToBookPipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeEnvironmentPricingBlock"/> class.
        /// </summary>
        /// <param name="addPriceBookPipeline">The add price book pipeline.</param>
        /// <param name="addPriceCardPipeline">The add price card pipeline.</param>
        /// <param name="addPriceSnapshotPipeline">The add price snapshot pipeline.</param>
        /// <param name="addPriceTierPipeline">The add price tier pipeline.</param>
        /// <param name="addPriceSnapshotTagPipeline">The add price snapshot tag pipeline.</param>
        /// <param name="persistEntityPipeline">The persist entity pipeline.</param>
        /// <param name="associateCatalogToBookPipeline">The add public coupon pipeline.</param>
        public InitializeEnvironmentPricingBlock(
            IAddPriceBookPipeline addPriceBookPipeline,
            IAddPriceCardPipeline addPriceCardPipeline,
            IAddPriceSnapshotPipeline addPriceSnapshotPipeline,
            IAddPriceTierPipeline addPriceTierPipeline,
            IAddPriceSnapshotTagPipeline addPriceSnapshotTagPipeline,
            IPersistEntityPipeline persistEntityPipeline,
            IAssociateCatalogToBookPipeline associateCatalogToBookPipeline)
        {
            _addPriceBookPipeline = addPriceBookPipeline;
            _addPriceCardPipeline = addPriceCardPipeline;
            _addPriceSnapshotPipeline = addPriceSnapshotPipeline;
            _addPriceTierPipeline = addPriceTierPipeline;
            _addPriceSnapshotTagPipeline = addPriceSnapshotTagPipeline;
            _persistEntityPipeline = persistEntityPipeline;
            _associateCatalogToBookPipeline = associateCatalogToBookPipeline;
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
        /// The run.
        /// </summary>
        /// <param name="arg">
        /// The argument.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public override async Task<string> RunAsync(string arg, CommercePipelineExecutionContext context)
        {
            var artifactSet = "Environment.AdventureWorks.Pricing-1.0";

            // Check if this environment has subscribed to this Artifact Set
            if (!context.GetPolicy<EnvironmentInitializationPolicy>().InitialArtifactSets.Contains(artifactSet))
            {
                return arg;
            }

            context.Logger.LogInformation($"{Name}.InitializingArtifactSet: ArtifactSet={artifactSet}");

            try
            {
                var currencySetId = context.GetPolicy<GlobalCurrencyPolicy>().DefaultCurrencySet;

                // ADVENTURE WORKS BOOK
                var adventureBook = await _addPriceBookPipeline.RunAsync(
                    new AddPriceBookArgument("AdventureWorksPriceBook")
                    {
                        ParentBook = "DefaultPriceBook",
                        Description = "Adventure works price book",
                        DisplayName = "Adventure Works",
                        CurrencySetId = currencySetId
                    },
                    context).ConfigureAwait(false);

                await CreateProductsCard(adventureBook, context).ConfigureAwait(false);

                await CreateVariantsCard(adventureBook, context).ConfigureAwait(false);

                await CreateTagsCard(adventureBook, context).ConfigureAwait(false);

                await AssociateCatalogToBook(adventureBook.Name, "Adventure Works Catalog", context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                context.CommerceContext.LogException(Name, ex);
            }

            return arg;
        }

        /// <summary>
        /// Creates the products card.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        private async Task CreateProductsCard(PriceBook book, CommercePipelineExecutionContext context)
        {
            context.CommerceContext.RemoveModels(m => m is PriceSnapshotAdded || m is PriceTierAdded);

            var date = DateTimeOffset.UtcNow;

            // ADVENTURE CARD
            var adventureCard = await _addPriceCardPipeline.RunAsync(new AddPriceCardArgument(book, "AdventureWorksPriceCard"), context).ConfigureAwait(false);

            // READY FOR APPROVAL SNAPSHOT
            adventureCard = await _addPriceSnapshotPipeline.RunAsync(new PriceCardSnapshotArgument(adventureCard, new PriceSnapshotComponent(date.AddMinutes(-10))), context).ConfigureAwait(false);
            var readyForApprovalSnapshot = adventureCard.Snapshots.FirstOrDefault(s => s.Id.Equals(context.CommerceContext.GetModel<PriceSnapshotAdded>()?.PriceSnapshotId, StringComparison.OrdinalIgnoreCase));

            adventureCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureCard, readyForApprovalSnapshot, new PriceTier("USD", 1, 2000M)), context).ConfigureAwait(false);

            context.CommerceContext.RemoveModels(m => m is PriceSnapshotAdded || m is PriceTierAdded);

            // ADVENTURE CARD FIRST SNAPSHOT
            adventureCard = await _addPriceSnapshotPipeline.RunAsync(new PriceCardSnapshotArgument(adventureCard, new PriceSnapshotComponent(date.AddHours(-1))), context).ConfigureAwait(false);
            var firstSnapshot = adventureCard.Snapshots.FirstOrDefault(s => s.Id.Equals(context.CommerceContext.GetModel<PriceSnapshotAdded>()?.PriceSnapshotId, StringComparison.OrdinalIgnoreCase));

            adventureCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureCard, firstSnapshot, new PriceTier("USD", 1, 10M)), context).ConfigureAwait(false);
            adventureCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureCard, firstSnapshot, new PriceTier("USD", 5, 5M)), context).ConfigureAwait(false);
            adventureCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureCard, firstSnapshot, new PriceTier("USD", 10, 1M)), context).ConfigureAwait(false);
            adventureCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureCard, firstSnapshot, new PriceTier("CAD", 1, 15M)), context).ConfigureAwait(false);
            adventureCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureCard, firstSnapshot, new PriceTier("CAD", 5, 10M)), context).ConfigureAwait(false);
            adventureCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureCard, firstSnapshot, new PriceTier("CAD", 10, 5M)), context).ConfigureAwait(false);
            adventureCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureCard, firstSnapshot, new PriceTier("EUR", 1, 1M)), context).ConfigureAwait(false);

            context.CommerceContext.RemoveModels(m => m is PriceSnapshotAdded || m is PriceTierAdded);

            // DRAFT SNAPSHOT
            adventureCard = await _addPriceSnapshotPipeline.RunAsync(new PriceCardSnapshotArgument(adventureCard, new PriceSnapshotComponent(date)), context).ConfigureAwait(false);
            var draftSnapshot = adventureCard.Snapshots.FirstOrDefault(s => s.Id.Equals(context.CommerceContext.GetModel<PriceSnapshotAdded>()?.PriceSnapshotId, StringComparison.OrdinalIgnoreCase));

            adventureCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureCard, draftSnapshot, new PriceTier("USD", 1, 1000M)), context).ConfigureAwait(false);

            adventureCard = await _addPriceSnapshotTagPipeline.RunAsync(new PriceCardSnapshotTagArgument(adventureCard, draftSnapshot, new Tag("new pricing")), context).ConfigureAwait(false);

            context.CommerceContext.RemoveModels(m => m is PriceSnapshotAdded || m is PriceTierAdded);

            // ADVENTURE CARD SECOND SNAPSHOT
            adventureCard = await _addPriceSnapshotPipeline.RunAsync(new PriceCardSnapshotArgument(adventureCard, new PriceSnapshotComponent(date.AddDays(30))), context).ConfigureAwait(false);
            var secondSnapshot = adventureCard.Snapshots.FirstOrDefault(s => s.Id.Equals(context.CommerceContext.GetModel<PriceSnapshotAdded>()?.PriceSnapshotId, StringComparison.OrdinalIgnoreCase));

            adventureCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureCard, secondSnapshot, new PriceTier("USD", 1, 7M)), context).ConfigureAwait(false);
            adventureCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureCard, secondSnapshot, new PriceTier("USD", 5, 4M)), context).ConfigureAwait(false);
            adventureCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureCard, secondSnapshot, new PriceTier("USD", 10, 3M)), context).ConfigureAwait(false);
            adventureCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureCard, secondSnapshot, new PriceTier("CAD", 1, 6M)), context).ConfigureAwait(false);
            adventureCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureCard, secondSnapshot, new PriceTier("CAD", 5, 3M)), context).ConfigureAwait(false);
            adventureCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureCard, secondSnapshot, new PriceTier("CAD", 10, 2M)), context).ConfigureAwait(false);
            adventureCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureCard, secondSnapshot, new PriceTier("EUR", 1, 1M)), context).ConfigureAwait(false);

            adventureCard = await _addPriceSnapshotTagPipeline.RunAsync(new PriceCardSnapshotTagArgument(adventureCard, secondSnapshot, new Tag("future pricing")), context).ConfigureAwait(false);

            context.CommerceContext.RemoveModels(m => m is PriceSnapshotAdded || m is PriceTierAdded);

            readyForApprovalSnapshot?.AddOrUpdateChildComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().ReadyForApproval));
            firstSnapshot?.AddOrUpdateChildComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            secondSnapshot?.AddOrUpdateChildComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));

            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(adventureCard), context).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the variants card.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        private async Task CreateVariantsCard(PriceBook book, CommercePipelineExecutionContext context)
        {
            context.CommerceContext.RemoveModels(m => m is PriceSnapshotAdded || m is PriceTierAdded);

            var date = DateTimeOffset.UtcNow;

            // ADVENTURE VARIANTS CARD
            var adventureVariantsCard = await _addPriceCardPipeline.RunAsync(new AddPriceCardArgument(book, "AdventureVariantsPriceCard"), context).ConfigureAwait(false);

            // READY FOR APPROVAL SNAPSHOT
            adventureVariantsCard = await _addPriceSnapshotPipeline.RunAsync(new PriceCardSnapshotArgument(adventureVariantsCard, new PriceSnapshotComponent(date.AddMinutes(-10))), context).ConfigureAwait(false);
            var readyForApprovalSnapshot = adventureVariantsCard.Snapshots.FirstOrDefault(s => s.Id.Equals(context.CommerceContext.GetModel<PriceSnapshotAdded>()?.PriceSnapshotId, StringComparison.OrdinalIgnoreCase));

            adventureVariantsCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureVariantsCard, readyForApprovalSnapshot, new PriceTier("USD", 1, 2000M)), context).ConfigureAwait(false);

            context.CommerceContext.RemoveModels(m => m is PriceSnapshotAdded || m is PriceTierAdded);

            // FIRST APPROVED SNAPSHOT
            adventureVariantsCard = await _addPriceSnapshotPipeline.RunAsync(new PriceCardSnapshotArgument(adventureVariantsCard, new PriceSnapshotComponent(date.AddHours(-1))), context).ConfigureAwait(false);
            var firstSnapshot = adventureVariantsCard.Snapshots.FirstOrDefault(s => s.Id.Equals(context.CommerceContext.GetModel<PriceSnapshotAdded>()?.PriceSnapshotId, StringComparison.OrdinalIgnoreCase));

            adventureVariantsCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureVariantsCard, firstSnapshot, new PriceTier("USD", 1, 9M)), context).ConfigureAwait(false);
            adventureVariantsCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureVariantsCard, firstSnapshot, new PriceTier("USD", 5, 6M)), context).ConfigureAwait(false);
            adventureVariantsCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureVariantsCard, firstSnapshot, new PriceTier("USD", 10, 3M)), context).ConfigureAwait(false);
            adventureVariantsCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureVariantsCard, firstSnapshot, new PriceTier("CAD", 1, 7M)), context).ConfigureAwait(false);
            adventureVariantsCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureVariantsCard, firstSnapshot, new PriceTier("CAD", 5, 4M)), context).ConfigureAwait(false);
            adventureVariantsCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureVariantsCard, firstSnapshot, new PriceTier("CAD", 10, 2M)), context).ConfigureAwait(false);
            adventureVariantsCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureVariantsCard, firstSnapshot, new PriceTier("EUR", 1, 2M)), context).ConfigureAwait(false);

            context.CommerceContext.RemoveModels(m => m is PriceSnapshotAdded || m is PriceTierAdded);

            // DRAFT SNAPSHOT
            adventureVariantsCard = await _addPriceSnapshotPipeline.RunAsync(new PriceCardSnapshotArgument(adventureVariantsCard, new PriceSnapshotComponent(date)), context).ConfigureAwait(false);
            var draftSnapshot = adventureVariantsCard.Snapshots.FirstOrDefault(s => s.Id.Equals(context.CommerceContext.GetModel<PriceSnapshotAdded>()?.PriceSnapshotId, StringComparison.OrdinalIgnoreCase));

            adventureVariantsCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureVariantsCard, draftSnapshot, new PriceTier("USD", 1, 1000M)), context).ConfigureAwait(false);

            context.CommerceContext.RemoveModels(m => m is PriceSnapshotAdded || m is PriceTierAdded);

            // SECOND APPROVED SNAPSHOT
            adventureVariantsCard = await _addPriceSnapshotPipeline.RunAsync(new PriceCardSnapshotArgument(adventureVariantsCard, new PriceSnapshotComponent(date.AddDays(30))), context).ConfigureAwait(false);
            var secondSnapshot = adventureVariantsCard.Snapshots.FirstOrDefault(s => s.Id.Equals(context.CommerceContext.GetModel<PriceSnapshotAdded>()?.PriceSnapshotId, StringComparison.OrdinalIgnoreCase));

            adventureVariantsCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureVariantsCard, secondSnapshot, new PriceTier("USD", 1, 8M)), context).ConfigureAwait(false);
            adventureVariantsCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureVariantsCard, secondSnapshot, new PriceTier("USD", 5, 4M)), context).ConfigureAwait(false);
            adventureVariantsCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureVariantsCard, secondSnapshot, new PriceTier("USD", 10, 2M)), context).ConfigureAwait(false);
            adventureVariantsCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureVariantsCard, secondSnapshot, new PriceTier("CAD", 1, 7M)), context).ConfigureAwait(false);
            adventureVariantsCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureVariantsCard, secondSnapshot, new PriceTier("CAD", 5, 3M)), context).ConfigureAwait(false);
            adventureVariantsCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureVariantsCard, secondSnapshot, new PriceTier("CAD", 10, 1M)), context).ConfigureAwait(false);
            adventureVariantsCard = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(adventureVariantsCard, secondSnapshot, new PriceTier("EUR", 1, 2M)), context).ConfigureAwait(false);

            context.CommerceContext.RemoveModels(m => m is PriceSnapshotAdded || m is PriceTierAdded);

            readyForApprovalSnapshot?.AddOrUpdateChildComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().ReadyForApproval));
            firstSnapshot?.AddOrUpdateChildComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            secondSnapshot?.AddOrUpdateChildComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(adventureVariantsCard), context).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the tags card.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        private async Task CreateTagsCard(PriceBook book, CommercePipelineExecutionContext context)
        {
            // ADVENTURE TAGS CARD
            var card = await _addPriceCardPipeline.RunAsync(new AddPriceCardArgument(book, "AdventureTagsPriceCard"), context).ConfigureAwait(false);

            // ADVENTURE TAGS CARD FIRST SNAPSHOT
            card = await _addPriceSnapshotPipeline.RunAsync(new PriceCardSnapshotArgument(card, new PriceSnapshotComponent(DateTimeOffset.UtcNow)), context).ConfigureAwait(false);
            var firstSnapshot = card.Snapshots.FirstOrDefault();

            // ADVENTURE TAGS CARD FIRST SNAPSHOT  TIERS
            card = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(card, firstSnapshot, new PriceTier("USD", 1, 250M)), context).ConfigureAwait(false);
            card = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(card, firstSnapshot, new PriceTier("USD", 5, 200M)), context).ConfigureAwait(false);
            card = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(card, firstSnapshot, new PriceTier("CAD", 1, 251M)), context).ConfigureAwait(false);
            card = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(card, firstSnapshot, new PriceTier("CAD", 5, 201M)), context).ConfigureAwait(false);

            // ADVENTURE TAGS CARD FIRST SNAPSHOT TAGS
            card = await _addPriceSnapshotTagPipeline.RunAsync(new PriceCardSnapshotTagArgument(card, firstSnapshot, new Tag("adventure works")), context).ConfigureAwait(false);
            card = await _addPriceSnapshotTagPipeline.RunAsync(new PriceCardSnapshotTagArgument(card, firstSnapshot, new Tag("adventure works 2")), context).ConfigureAwait(false);
            card = await _addPriceSnapshotTagPipeline.RunAsync(new PriceCardSnapshotTagArgument(card, firstSnapshot, new Tag("common")), context).ConfigureAwait(false);

            // ADVENTURE TAGS CARD SECOND SNAPSHOT
            card = await _addPriceSnapshotPipeline.RunAsync(new PriceCardSnapshotArgument(card, new PriceSnapshotComponent(DateTimeOffset.UtcNow.AddSeconds(1))), context).ConfigureAwait(false);
            var secondSnapshot = card.Snapshots.FirstOrDefault(s => !s.Id.Equals(firstSnapshot?.Id, StringComparison.OrdinalIgnoreCase));

            // ADVENTURE TAGS CARD SECOND SNAPSHOT TIERS
            card = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(card, secondSnapshot, new PriceTier("USD", 1, 150M)), context).ConfigureAwait(false);
            card = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(card, secondSnapshot, new PriceTier("USD", 5, 100M)), context).ConfigureAwait(false);
            card = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(card, secondSnapshot, new PriceTier("CAD", 1, 101M)), context).ConfigureAwait(false);
            card = await _addPriceTierPipeline.RunAsync(new PriceCardSnapshotTierArgument(card, secondSnapshot, new PriceTier("CAD", 5, 151M)), context).ConfigureAwait(false);

            // ADVENTURE TAGS CARD SECOND SNAPSHOT TAGS
            card = await _addPriceSnapshotTagPipeline.RunAsync(new PriceCardSnapshotTagArgument(card, secondSnapshot, new Tag("adventure works variants")), context).ConfigureAwait(false);
            card = await _addPriceSnapshotTagPipeline.RunAsync(new PriceCardSnapshotTagArgument(card, secondSnapshot, new Tag("adventure works variants 2")), context).ConfigureAwait(false);
            card = await _addPriceSnapshotTagPipeline.RunAsync(new PriceCardSnapshotTagArgument(card, secondSnapshot, new Tag("common")), context).ConfigureAwait(false);

            // ADVENTURE TAGS CARD APPROVAL COMPONENT
            foreach (var s in card.Snapshots)
            {
                s.AddOrUpdateChildComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            }

            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(card), context).ConfigureAwait(false);
        }

        private async Task AssociateCatalogToBook(string bookName, string catalogName, CommercePipelineExecutionContext context)
        {
            // To persist entities conventionally and to prevent any race conditions, create a separate CommercePipelineExecutionContext object and CommerceContext object.
            var pipelineExecutionContext = new CommercePipelineExecutionContext(new CommerceContext(context.CommerceContext.Logger, context.CommerceContext.TelemetryClient)
            {
                GlobalEnvironment = context.CommerceContext.GlobalEnvironment,
                Environment = context.CommerceContext.Environment,
                Headers = new HeaderDictionary(context.CommerceContext.Headers.ToDictionary(x => x.Key, y => y.Value)), // Clone current context headers by shallow copy.
                RequestIdentity = context.CommerceContext.RequestIdentity
            }.PipelineContextOptions, context.CommerceContext.Logger);

            // To persist entities conventionally, remove policy keys in the newly created CommerceContext object.
            pipelineExecutionContext.CommerceContext.RemoveHeader(CoreConstants.PolicyKeys);

            var arg = new CatalogAndBookArgument(bookName, catalogName);
            await _associateCatalogToBookPipeline.RunAsync(arg, pipelineExecutionContext).ConfigureAwait(false);
        }
    }
}
