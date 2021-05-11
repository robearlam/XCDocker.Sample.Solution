﻿// © 2017 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Entitlements;
using Sitecore.Commerce.Plugin.GiftCards;
using Sitecore.Commerce.Plugin.ManagedLists;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.AdventureWorks
{
    /// <summary>
    /// Defines a block which adds a set of Test GiftCards during the environment initialization.
    /// </summary>
    /// <seealso>
    ///     <cref>
    ///         Sitecore.Framework.Pipelines.PipelineBlock{System.String, System.String,
    ///         Sitecore.Commerce.Core.CommercePipelineExecutionContext}
    ///     </cref>
    /// </seealso>
    [PipelineDisplayName(AwConstants.InitializeEnvironmentGiftCardsBlock)]
    public class InitializeEnvironmentGiftCardsBlock : AsyncPolicyTriggerConditionalPipelineBlock<string, string>
    {
        private readonly IPersistEntityPipeline _persistEntityPipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeEnvironmentGiftCardsBlock"/> class.
        /// </summary>
        /// <param name="persistEntityPipeline">The persist entity pipeline.</param>
        public InitializeEnvironmentGiftCardsBlock(
            IPersistEntityPipeline persistEntityPipeline)
        {
            _persistEntityPipeline = persistEntityPipeline;
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
        /// The <see cref="bool"/>.
        /// </returns>
        public override async Task<string> RunAsync(string arg, CommercePipelineExecutionContext context)
        {
            var artifactSet = "GiftCards.TestGiftCards-1.0";

            // Check if this environment has subscribed to this Artifact Set
            if (!context.GetPolicy<EnvironmentInitializationPolicy>().InitialArtifactSets.Contains(artifactSet))
            {
                return arg;
            }

            context.Logger.LogInformation($"{Name}.InitializingArtifactSet: ArtifactSet={artifactSet}");

            // Add stock gift cards for testing
            var result = await _persistEntityPipeline.RunAsync(
                    new PersistEntityArgument(
                        new GiftCard(new List<Component>
                        {
                            new ListMembershipsComponent
                            {
                                Memberships = new List<string>
                                {
                                    CommerceEntity.ListName<Entitlement>(),
                                    CommerceEntity.ListName<GiftCard>()
                                }
                            }
                        })
                        {
                            Id = $"{CommerceEntity.IdPrefix<GiftCard>()}GC1000000",
                            Name = "Test Gift Card ($1,000,000)",
                            Balance = new Money("USD", 1000000M),
                            ActivationDate = DateTimeOffset.UtcNow,
                            Customer = new EntityReference
                            {
                                EntityTarget = "DefaultCustomer"
                            },
                            OriginalAmount = new Money("USD", 1000000M),
                            GiftCardCode = "GC1000000"
                        }),
                    context)
                .ConfigureAwait(false);

            await _persistEntityPipeline.RunAsync(
                    new PersistEntityArgument(
                        new EntityIndex
                        {
                            Id = $"{EntityIndex.IndexPrefix<GiftCard>("Id")}GC1000000",
                            IndexKey = "GC1000000",
                            EntityId = $"{CommerceEntity.IdPrefix<GiftCard>()}GC1000000",
                            EntityUniqueId = result.Entity.UniqueId
                        }),
                    context)
                .ConfigureAwait(false);

            result = await _persistEntityPipeline.RunAsync(
                new PersistEntityArgument(
                    new GiftCard(new List<Component>
                    {
                        new ListMembershipsComponent
                        {
                            Memberships = new List<string>
                            {
                                CommerceEntity.ListName<Entitlement>(),
                                CommerceEntity.ListName<GiftCard>()
                            }
                        }
                    })
                    {
                        Id = $"{CommerceEntity.IdPrefix<GiftCard>()}GC100B",
                        Name = "Test Gift Card ($100,000,000,000,000)",
                        Balance = new Money("USD", 100000000000000M),
                        ActivationDate = DateTimeOffset.UtcNow,
                        Customer = new EntityReference
                        {
                            EntityTarget = "DefaultCustomer"
                        },
                        OriginalAmount = new Money("USD", 100000000000000M),
                        GiftCardCode = "GC100B",
                    }),
                context).ConfigureAwait(false);

            await _persistEntityPipeline.RunAsync(
                new PersistEntityArgument(
                    new EntityIndex
                    {
                        Id = $"{EntityIndex.IndexPrefix<GiftCard>("Id")}GC100B",
                        IndexKey = "GC100B",
                        EntityId = $"{CommerceEntity.IdPrefix<GiftCard>()}GC100B",
                        EntityUniqueId = result.Entity.UniqueId
                    }),
                context).ConfigureAwait(false);

            result = await _persistEntityPipeline.RunAsync(
                new PersistEntityArgument(
                    new GiftCard(new List<Component>
                    {
                        new ListMembershipsComponent
                        {
                            Memberships = new List<string>
                            {
                                CommerceEntity.ListName<Entitlement>(),
                                CommerceEntity.ListName<GiftCard>()
                            }
                        }
                    })
                    {
                        Id = $"{CommerceEntity.IdPrefix<GiftCard>()}GC100",
                        Name = "Test Gift Card ($100)",
                        Balance = new Money("USD", 100M),
                        ActivationDate = DateTimeOffset.UtcNow,
                        Customer = new EntityReference
                        {
                            EntityTarget = "DefaultCustomer"
                        },
                        OriginalAmount = new Money("USD", 100M),
                        GiftCardCode = "GC100"
                    }),
                context).ConfigureAwait(false);

            await _persistEntityPipeline.RunAsync(
                new PersistEntityArgument(
                    new EntityIndex
                    {
                        Id = $"{EntityIndex.IndexPrefix<GiftCard>("Id")}GC100",
                        IndexKey = "GC100",
                        EntityId = $"{CommerceEntity.IdPrefix<GiftCard>()}GC100",
                        EntityUniqueId = result.Entity.UniqueId
                    }),
                context).ConfigureAwait(false);

            return arg;
        }
    }
}
