// © 2018 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.Habitat
{
    /// <summary>
    /// Ensure that a bundle is created
    /// </summary>
    /// <seealso>
    ///     <cref>
    ///         Sitecore.Framework.Pipelines.PipelineBlock{System.String, System.String,
    ///         Sitecore.Commerce.Core.CommercePipelineExecutionContext}
    ///     </cref>
    /// </seealso>
    [PipelineDisplayName(HabitatConstants.InitializeBundlesBlock)]
    public class InitializeEnvironmentBundlesBlock : AsyncPolicyTriggerConditionalPipelineBlock<string, string>
    {
        private readonly CommerceCommander _commerceCommander;
        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeEnvironmentBundlesBlock"/> class.
        /// </summary>
        public InitializeEnvironmentBundlesBlock(CommerceCommander commerceCommander)
        {
            _commerceCommander = commerceCommander;
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
            var artifactSet = "Environment.Habitat.SellableItems-1.0";

            // Check if this environment has subscribed to this Artifact Set
            if (!context.GetPolicy<EnvironmentInitializationPolicy>().InitialArtifactSets.Contains(artifactSet))
            {
                return arg;
            }

            // Ensure that the new items will be published right away
            context.CommerceContext.Environment.SetPolicy(new AutoApprovePolicy());

            await CreateExampleStaticBundles(context).ConfigureAwait(false);

            await CreateExampleDynamicBundles(context).ConfigureAwait(false);

            context.CommerceContext.Environment.RemovePolicy(typeof(AutoApprovePolicy));

            return arg;
        }

        private async Task CreateExampleStaticBundles(CommercePipelineExecutionContext context)
        {
            // First bundle
            var bundle1 =
                await CreateBundle(
                    context.CommerceContext,
                    "Static",
                    "6001001",
                    "SmartWiFiBundle",
                    "Smart WiFi Bundle",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new[]
                    {
                        "smart",
                        "wifi",
                        "bundle"
                    },
                    new List<BundleItem>
                    {
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042964|56042964",
                            Quantity = 1
                        },
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042971|56042971",
                            Quantity = 1
                        }
                    }).ConfigureAwait(false);

            // Set image and list price for bundle
            bundle1.GetComponent<ImagesComponent>().Images.Add("65703328-1456-48da-a693-bad910d7d1fe");

            bundle1.SetPolicy(
                new ListPricingPolicy(
                    new List<Money>
                    {
                        new Money("USD", 200.00M),
                        new Money("CAD", 250.00M)
                    }));

            await _commerceCommander.Pipeline<IPersistEntityPipeline>()
                .RunAsync(new PersistEntityArgument(bundle1), context).ConfigureAwait(false);

            // Associate bundle to parent category
            await AssociateEntity(context.CommerceContext, new CatalogReferenceArgument(
                "Entity-Catalog-Habitat_Master",
                "Entity-Category-Habitat_Master-Connected home",
                bundle1.Id)).ConfigureAwait(false);

            // Second bundle
            var bundle2 =
                await CreateBundle(
                    context.CommerceContext,
                    "Static",
                    "6001002",
                    "ActivityTrackerCameraBundle",
                    "Activity Tracker & Camera Bundle",
                    "Sample bundle containing two activity trackers and two cameras.",
                    "Striva Wearables",
                    string.Empty,
                    string.Empty,
                    new[]
                    {
                        "activitytracker",
                        "camera",
                        "bundle"
                    },
                    new List<BundleItem>
                    {
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042896|56042896",
                            Quantity = 2
                        },
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-7042066|57042066",
                            Quantity = 2
                        }
                    }).ConfigureAwait(false);

            // Set image and list price for bundle
            bundle2.GetComponent<ImagesComponent>().Images.Add("003c9ee5-2d97-4a6c-bb9e-24e110cd7645");

            bundle2.SetPolicy(
                new ListPricingPolicy(
                    new List<Money>
                    {
                        new Money("USD", 220.00M),
                        new Money("CAD", 280.00M)
                    }));

            await _commerceCommander.Pipeline<IPersistEntityPipeline>()
                .RunAsync(new PersistEntityArgument(bundle2), context).ConfigureAwait(false);

            // Associate bundle to parent category
            await AssociateEntity(context.CommerceContext, new CatalogReferenceArgument(
                "Entity-Catalog-Habitat_Master",
                "Entity-Category-Habitat_Master-Fitness Activity Trackers",
                 bundle2.Id)).ConfigureAwait(false);

            // Third bundle
            var bundle3 =
                await CreateBundle(
                    context.CommerceContext,
                    "Static",
                    "6001003",
                    "RefrigeratorFlipPhoneBundle",
                    "Refrigerator & Flip Phone Bundle",
                    "Sample bundle containing a refrigerator and two flip phones.",
                    "Viva Refrigerators",
                    string.Empty,
                    string.Empty,
                    new[]
                    {
                        "refrigerator",
                        "flipphone",
                        "bundle"
                    },
                    new List<BundleItem>
                    {
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042567|56042568",
                            Quantity = 1
                        },
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042331|56042331",
                            Quantity = 2
                        },
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042896|56042896",
                            Quantity = 3
                        },
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-7042066|57042066",
                            Quantity = 4
                        }
                    }).ConfigureAwait(false);

            // Set image and list price for bundle
            bundle3.GetComponent<ImagesComponent>().Images.Add("372d8bc6-6888-4375-91c1-f3bee2d31558");

            bundle3.SetPolicy(
                new ListPricingPolicy(
                    new List<Money>
                    {
                        new Money("USD", 10.00M),
                        new Money("CAD", 20.00M)
                    }));

            await _commerceCommander.Pipeline<IPersistEntityPipeline>()
                .RunAsync(new PersistEntityArgument(bundle3), context).ConfigureAwait(false);

            // Associate bundle to parent category
            await AssociateEntity(context.CommerceContext, new CatalogReferenceArgument(
                "Entity-Catalog-Habitat_Master",
                "Entity-Category-Habitat_Master-Appliances",
                bundle3.Id)).ConfigureAwait(false);

            // Fourth bundle with digital items
            var bundle4 =
                await CreateBundle(
                    context.CommerceContext,
                    "Static",
                    "6001004",
                    "GiftCardAndSubscriptionBundle",
                    "Gift Card & Subscription Bundle",
                    "Sample bundle containing a gift card and two subscriptions.",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new[]
                    {
                        "bundle",
                        "giftcard",
                        "entitlement"
                    },
                    new List<BundleItem>
                    {
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042986|56042987",
                            Quantity = 1
                        },
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042453|56042453",
                            Quantity = 2
                        }
                    }).ConfigureAwait(false);

            // Set image and list price for bundle
            bundle4.GetComponent<ImagesComponent>().Images.Add("7b57e6e0-a4ef-417e-809c-572f2e30aef7");

            bundle4.SetPolicy(
                new ListPricingPolicy(
                    new List<Money>
                    {
                        new Money("USD", 10.00M),
                        new Money("CAD", 20.00M)
                    }));

            await _commerceCommander.Pipeline<IPersistEntityPipeline>()
                .RunAsync(new PersistEntityArgument(bundle4), context).ConfigureAwait(false);

            // Associate bundle to parent category
            await AssociateEntity(context.CommerceContext, new CatalogReferenceArgument(
                "Entity-Catalog-Habitat_Master",
                "Entity-Category-Habitat_Master-eGift Cards and Gift Wrapping",
                bundle4.Id)).ConfigureAwait(false);

            // Preorderable bundle
            var bundle5 =
                await CreateBundle(
                    context.CommerceContext,
                    "Static",
                    "6001005",
                    "PreorderableBundle",
                    "Preorderable Bundle",
                    "Sample bundle containing a phone and headphones.",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new[]
                    {
                        "bundle"
                    },
                    new List<BundleItem>
                    {
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042305|56042305",
                            Quantity = 1
                        },
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042059|56042059",
                            Quantity = 1
                        }
                    }).ConfigureAwait(false);

            // Set image and list price for bundle
            bundle5.GetComponent<ImagesComponent>().Images.Add("b0b07d7b-ddaf-4798-8eb9-af7f570af3fe");

            bundle5.SetPolicy(
                new ListPricingPolicy(
                    new List<Money>
                    {
                        new Money("USD", 44.99M),
                        new Money("CAD", 59.99M)
                    }));

            await _commerceCommander.Pipeline<IPersistEntityPipeline>()
                .RunAsync(new PersistEntityArgument(bundle5), context).ConfigureAwait(false);

            // Associate bundle to parent category
            await AssociateEntity(context.CommerceContext, new CatalogReferenceArgument(
                "Entity-Catalog-Habitat_Master",
                "Entity-Category-Habitat_Master-Phones",
                bundle5.Id)).ConfigureAwait(false);

            // Backorderable bundle
            var bundle6 =
                await CreateBundle(
                    context.CommerceContext,
                    "Static",
                    "6001006",
                    "BackorderableBundle",
                    "Backorderable Bundle",
                    "Sample bundle containing a phone and headphones.",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new[]
                    {
                        "bundle"
                    },
                    new List<BundleItem>
                    {
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042305|56042305",
                            Quantity = 1
                        },
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042058|56042058",
                            Quantity = 1
                        }
                    }).ConfigureAwait(false);

            // Set image and list price for bundle
            bundle6.GetComponent<ImagesComponent>().Images.Add("b0b07d7b-ddaf-4798-8eb9-af7f570af3fe");

            bundle6.SetPolicy(
                new ListPricingPolicy(
                    new List<Money>
                    {
                        new Money("USD", 44.99M),
                        new Money("CAD", 59.99M)
                    }));

            await _commerceCommander.Pipeline<IPersistEntityPipeline>()
                .RunAsync(new PersistEntityArgument(bundle6), context).ConfigureAwait(false);

            // Associate bundle to parent category
            await AssociateEntity(context.CommerceContext, new CatalogReferenceArgument(
                "Entity-Catalog-Habitat_Master",
                "Entity-Category-Habitat_Master-Phones",
                bundle6.Id)).ConfigureAwait(false);

            // Backorderable bundle
            var bundle7 =
                await CreateBundle(
                    context.CommerceContext,
                    "Static",
                    "6001007",
                    "PreorderableBackorderableBundle",
                    "Preorderable / Backorderable Bundle",
                    "Sample bundle containing headphones.",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new[]
                    {
                        "bundle"
                    },
                    new List<BundleItem>
                    {
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042058|56042058",
                            Quantity = 1
                        },
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042059|56042059",
                            Quantity = 1
                        }
                    }).ConfigureAwait(false);

            // Set image and list price for bundle
            bundle7.GetComponent<ImagesComponent>().Images.Add("b0b07d7b-ddaf-4798-8eb9-af7f570af3fe");

            bundle7.SetPolicy(
                new ListPricingPolicy(
                    new List<Money>
                    {
                        new Money("USD", 44.99M),
                        new Money("CAD", 59.99M)
                    }));

            await _commerceCommander.Pipeline<IPersistEntityPipeline>()
                .RunAsync(new PersistEntityArgument(bundle7), context).ConfigureAwait(false);

            // Associate bundle to parent category
            await AssociateEntity(context.CommerceContext, new CatalogReferenceArgument(
                "Entity-Catalog-Habitat_Master",
                "Entity-Category-Habitat_Master-Audio",
                bundle7.Id)).ConfigureAwait(false);

            // Eigth bundle with a gift card only
            var bundle8 =
                await CreateBundle(
                    context.CommerceContext,
                    "Static",
                    "6001008",
                    "GiftCardBundle",
                    "Gift Card Bundle",
                    "Sample bundle containing a gift card.",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new[]
                    {
                        "bundle",
                        "entitlement",
                        "giftcard"
                    },
                    new List<BundleItem>
                    {
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042986|56042987",
                            Quantity = 1
                        }
                    }).ConfigureAwait(false);

            // Set image and list price for bundle
            bundle8.GetComponent<ImagesComponent>().Images.Add("7b57e6e0-a4ef-417e-809c-572f2e30aef7");

            bundle8.SetPolicy(
                new ListPricingPolicy(
                    new List<Money>
                    {
                        new Money("USD", 40.00M),
                        new Money("CAD", 50.00M)
                    }));

            await _commerceCommander.Pipeline<IPersistEntityPipeline>()
                .RunAsync(new PersistEntityArgument(bundle8), context).ConfigureAwait(false);

            // Associate bundle to parent category
            await AssociateEntity(context.CommerceContext, new CatalogReferenceArgument(
                "Entity-Catalog-Habitat_Master",
                "Entity-Category-Habitat_Master-eGift Cards and Gift Wrapping",
                bundle8.Id)).ConfigureAwait(false);

            // Warranty bundle
            var bundle9 =
                await CreateBundle(
                    context.CommerceContext,
                    "Static",
                    "6001009",
                    "WarrantyBundle",
                    "Warranty Bundle",
                    "Sample bundle containing a warranty.",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new[]
                    {
                        "bundle",
                        "warranty"
                    },
                    new List<BundleItem>
                    {
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-7042259|57042259",
                            Quantity = 1
                        }
                    }).ConfigureAwait(false);

            // Set image and list price for bundle
            bundle9.GetComponent<ImagesComponent>().Images.Add("eebf49f2-74df-4fe6-b77f-f2d1d447827c");

            bundle9.SetPolicy(
                new ListPricingPolicy(
                    new List<Money>
                    {
                        new Money("USD", 150.00M),
                        new Money("CAD", 200.00M)
                    }));

            await _commerceCommander.Pipeline<IPersistEntityPipeline>()
                .RunAsync(new PersistEntityArgument(bundle9), context).ConfigureAwait(false);

            // Associate bundle to parent category
            await AssociateEntity(context.CommerceContext, new CatalogReferenceArgument(
                "Entity-Catalog-Habitat_Master",
                "Entity-Category-Habitat_Master-eGift Cards and Gift Wrapping",
                bundle9.Id)).ConfigureAwait(false);

            // Service bundle
            var bundle10 =
                await CreateBundle(
                    context.CommerceContext,
                    "Static",
                    "6001010",
                    "ServiceBundle",
                    "Service Bundle",
                    "Sample bundle containing a service.",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new[]
                    {
                        "bundle",
                        "service"
                    },
                    new List<BundleItem>
                    {
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042418|56042418",
                            Quantity = 1
                        }
                    }).ConfigureAwait(false);

            // Set image and list price for bundle
            bundle10.GetComponent<ImagesComponent>().Images.Add("8b59fe2a-c234-4f92-b84b-7515411bf46e");

            bundle10.SetPolicy(
                new ListPricingPolicy(
                    new List<Money>
                    {
                        new Money("USD", 150.00M),
                        new Money("CAD", 200.00M)
                    }));

            await _commerceCommander.Pipeline<IPersistEntityPipeline>()
                .RunAsync(new PersistEntityArgument(bundle10), context).ConfigureAwait(false);

            // Associate bundle to parent category
            await AssociateEntity(context.CommerceContext, new CatalogReferenceArgument(
                "Entity-Catalog-Habitat_Master",
                "Entity-Category-Habitat_Master-eGift Cards and Gift Wrapping",
                bundle10.Id)).ConfigureAwait(false);

            // Subscription bundle
            var bundle11 =
                await CreateBundle(
                    context.CommerceContext,
                    "Static",
                    "6001011",
                    "SubscriptionBundle",
                    "Subscription Bundle",
                    "Sample bundle containing a subscription.",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new[]
                    {
                        "bundle",
                        "subscription"
                    },
                    new List<BundleItem>
                    {
                        new BundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042453|56042453",
                            Quantity = 1
                        }
                    }).ConfigureAwait(false);

            // Set image and list price for bundle
            bundle11.GetComponent<ImagesComponent>().Images.Add("22d74215-8e5f-4de3-a9d6-ece3042bd64c");

            bundle11.SetPolicy(
                new ListPricingPolicy(
                    new List<Money>
                    {
                        new Money("USD", 10.00M),
                        new Money("CAD", 15.00M)
                    }));

            await _commerceCommander.Pipeline<IPersistEntityPipeline>()
                .RunAsync(new PersistEntityArgument(bundle11), context).ConfigureAwait(false);

            // Associate bundle to parent category
            await AssociateEntity(context.CommerceContext, new CatalogReferenceArgument(
                "Entity-Catalog-Habitat_Master",
                "Entity-Category-Habitat_Master-eGift Cards and Gift Wrapping",
                bundle11.Id)).ConfigureAwait(false);
        }

        private async Task CreateExampleDynamicBundles(CommercePipelineExecutionContext context)
        {
            // First bundle
            var bundle1 =
                await CreateBundle(
                    context.CommerceContext,
                    "Dynamic",
                    "6002001",
                    "SmartWiFiDynamicBundle",
                    "Smart WiFi Dynamic Bundle",
                    "Sample dynamic bundle containing upgrades with manual pricing",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new[]
                    {
                        "smart",
                        "wifi",
                        "dynamic",
                        "bundle"
                    },
                    new List<BundleItem>
                    {
                        new DynamicBundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042194|56042194",
                            Quantity = 1,
                            MaximumQuantity = 3,
                            IsUpgradable = true,
                            IsOptional = false,
                            UnitPrice = new MultiCurrency { Values = {
                                new Money("USD", 50.00M),
                                new Money("CAD", 60.00M)
                                }
                            }
                        },
                        new DynamicBundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042944|56042944",
                            Quantity = 2,
                            MaximumQuantity = 3,
                            IsUpgradable = false,
                            IsOptional = false,
                            UnitPrice = new MultiCurrency { Values = {
                                    new Money("USD", 100.00M),
                                    new Money("CAD", 120.00M)
                                }
                            }
                        }
                    }).ConfigureAwait(false);

            var upgradeComponent = bundle1.GetComponent<BundleComponent>().ChildComponents.OfType<BundleItemUpgradeOptionsComponent>()
                                            .FirstOrDefault(i => i.SellableItemId.Equals("Entity-SellableItem-6042194|56042194", StringComparison.OrdinalIgnoreCase));
            upgradeComponent?.UpgradeItems.AddRange(new List<DynamicBundleItem>
                {
                    new DynamicBundleItem
                    {
                        SellableItemId = "Entity-SellableItem-6042198|56042198",
                        Quantity = 1,
                        MaximumQuantity = 3,
                        IsUpgradable = true,
                        IsOptional = false,
                        UnitPrice = new MultiCurrency { Values = {
                                new Money("CAD", 173.00M)
                            }
                        }
                    },
                    new DynamicBundleItem
                    {
                        SellableItemId = "Entity-SellableItem-6042206|56042206",
                        Quantity = 1,
                        MaximumQuantity = 3,
                        IsUpgradable = true,
                        IsOptional = false,
                        UnitPrice = new MultiCurrency { Values = {
                                new Money("CAD", 208.00M)
                            }
                        }
                    }
                });

            // Set image and list price for bundle
            bundle1.GetComponent<ImagesComponent>().Images.Add("A2BAA348-FA68-4C72-8AFE-ADC108EDCD63");
            bundle1.RemovePolicy(typeof(AutomaticPricingPolicy));          

            bundle1.SetPolicy(
                new ListPricingPolicy(
                    new List<Money>
                    {
                        new Money("USD", 250.00M),
                        new Money("CAD", 300.00M)
                    }));

            await _commerceCommander.Pipeline<IPersistEntityPipeline>()
                .RunAsync(new PersistEntityArgument(bundle1), context).ConfigureAwait(false);

            foreach (var upgradeItem in upgradeComponent?.UpgradeItems)
            {
                await CreateBundleRelationship(bundle1.Id, upgradeItem, context).ConfigureAwait(false);
            }

            // Associate bundle to parent category
            await AssociateEntity(context.CommerceContext, new CatalogReferenceArgument(
                "Entity-Catalog-Habitat_Master",
                "Entity-Category-Habitat_Master-Connected home",
                bundle1.Id)).ConfigureAwait(false);

             // Second bundle
            var bundle2 =
                await CreateBundle(
                    context.CommerceContext,
                    "Dynamic",
                    "6002002",
                    "AutomaticPricingDynamicBundle",
                    "Automatic Pricing Dynamic Bundle",
                    "Sample dynamic bundle containing upgrades with automatic pricing",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new[]
                    {
                        "dynamic",
                        "bundle"
                    },
                    new List<BundleItem>
                    {
                        new DynamicBundleItem
                        {
                            SellableItemId = "Entity-SellableItem-7042071|57042071",
                            Quantity = 1,
                            MaximumQuantity = 3,
                            IsUpgradable = true,
                            IsOptional = false
                        },
                        new DynamicBundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042063",
                            Quantity = 2,
                            MaximumQuantity = 3,
                            IsUpgradable = false,
                            IsOptional = true
                        }
                    }).ConfigureAwait(false);

            var upgradeComponent2 = bundle2.GetComponent<BundleComponent>().ChildComponents.OfType<BundleItemUpgradeOptionsComponent>()
                                            .FirstOrDefault(i => i.SellableItemId.Equals("Entity-SellableItem-7042071|57042071", StringComparison.OrdinalIgnoreCase));
            upgradeComponent2?.UpgradeItems.AddRange(new List<DynamicBundleItem>
                {
                    new DynamicBundleItem
                    {
                        SellableItemId = "Entity-SellableItem-7042073",
                        Quantity = 1,
                        MaximumQuantity = 3,
                        IsUpgradable = false,
                        IsOptional = false
                    },
                    new DynamicBundleItem
                    {
                        SellableItemId = "Entity-SellableItem-7042078|57042078",
                        Quantity = 1,
                        MaximumQuantity = 3,
                        IsUpgradable = false,
                        IsOptional = false
                    }
                });           

            // Set image and list price for bundle
            bundle2.GetComponent<ImagesComponent>().Images.Add("C8D50E8E-D22A-4780-8A4F-7126167DEEC1");

            await _commerceCommander.Pipeline<IPersistEntityPipeline>()
                .RunAsync(new PersistEntityArgument(bundle2), context).ConfigureAwait(false);

            foreach (var upgradeItem in upgradeComponent2?.UpgradeItems)
            {
                await CreateBundleRelationship(bundle2.Id, upgradeItem, context).ConfigureAwait(false);
            }

            // Associate bundle to parent category
            await AssociateEntity(context.CommerceContext, new CatalogReferenceArgument(
                "Entity-Catalog-Habitat_Master",
                 "Entity-Category-Habitat_Master-Cameras",
                bundle2.Id)).ConfigureAwait(false);

             // Appliances All Variants bundle
            var bundle3 =
                await CreateBundle(
                    context.CommerceContext,
                    "Dynamic",
                    "6002003",
                    "AppliancesAllVariantsBundle",
                    "Appliances All Variants Bundle",
                    "Sample dynamic bundle containing all variants for appliances",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new[]
                    {
                        "bundle",
                        "dynamic",
                        "appliances"
                    },
                    new List<BundleItem>
                    {
                        new DynamicBundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042641",
                            Quantity = 1,
                            MaximumQuantity = 1,
                            IsUpgradable = false,
                            IsOptional = false,
                            UnitPrice = new MultiCurrency { Values = {
                            new Money("USD", 1450.00M),
                            new Money("CAD", 1550.00M)
                        }
                    }
                        },
                        new DynamicBundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042737",
                            Quantity = 1,
                            MaximumQuantity = 2,
                            IsUpgradable = false,
                            IsOptional = true,
                            UnitPrice = new MultiCurrency { Values = {
                                    new Money("USD", 1000.00M),
                                    new Money("CAD", 1050.00M)
                                }
                            }
                        },
                        new DynamicBundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042579",
                            Quantity = 1,
                            MaximumQuantity = 1,
                            IsUpgradable = false,
                            IsOptional = false,
                            UnitPrice = new MultiCurrency { Values = {
                                    new Money("USD", 4450.00M),
                                    new Money("CAD", 4550.00M)
                                }
                            }
                        }
                    }).ConfigureAwait(false);

            // Set image and list price for bundle
            bundle3.GetComponent<ImagesComponent>().Images.Add("42c1f3d1-6e98-4331-a1d3-720cbfaf1158");
            bundle3.RemovePolicy(typeof(AutomaticPricingPolicy));   

            bundle3.SetPolicy(
                new ListPricingPolicy(
                    new List<Money>
                    {
                        new Money("USD", 6989.99M),
                        new Money("CAD", 7199.99M)
                    }));

            await _commerceCommander.Pipeline<IPersistEntityPipeline>()
                .RunAsync(new PersistEntityArgument(bundle3), context).ConfigureAwait(false);

            // Associate bundle to parent category
            await AssociateEntity(context.CommerceContext, new CatalogReferenceArgument(
                "Entity-Catalog-Habitat_Master",
                "Entity-Category-Habitat_Master-Appliances",
                bundle3.Id)).ConfigureAwait(false);
           
             // Home Theater automatic pricing bundle
            var bundle4 =
                await CreateBundle(
                    context.CommerceContext,
                    "Dynamic",
                    "6002004",
                    "HomeTheaterDynamicBundle",
                    "Home Theater Dynamic Bundle",
                    "Sample dynamic bundle containing optional items with automatic pricing",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new[]
                    {
                        "dynamic",
                        "hometheater",
                        "bundle"
                    },
                    new List<BundleItem>
                    {
                        new DynamicBundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042400|56042400",
                            Quantity = 1,
                            MaximumQuantity = 2,
                            IsUpgradable = false,
                            IsOptional = false
                        },
                        new DynamicBundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042408|56042408",
                            Quantity = 1,
                            MaximumQuantity = 3,
                            IsUpgradable = false,
                            IsOptional = true
                        },
                        new DynamicBundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042414|56042414",
                            Quantity = 2,
                            MaximumQuantity = 3,
                            IsUpgradable = true,
                            IsOptional = true
                        }
                    }).ConfigureAwait(false);

            var upgradeComponent4 = bundle4.GetComponent<BundleComponent>().ChildComponents.OfType<BundleItemUpgradeOptionsComponent>()
                                            .FirstOrDefault(i => i.SellableItemId.Equals("Entity-SellableItem-6042414|56042414", StringComparison.OrdinalIgnoreCase));
            upgradeComponent4?.UpgradeItems.AddRange(new List<DynamicBundleItem>
                {
                    new DynamicBundleItem
                    {
                        SellableItemId = "Entity-SellableItem-6042412|56042412",
                        Quantity = 1,
                        MaximumQuantity = 3,
                        IsUpgradable = false,
                        IsOptional = false
                    },
                    new DynamicBundleItem
                    {
                        SellableItemId = "Entity-SellableItem-6042413|56042413",
                        Quantity = 1,
                        MaximumQuantity = 3,
                        IsUpgradable = false,
                        IsOptional = false
                    }
                });           

            // Set image and list price for bundle
            bundle4.GetComponent<ImagesComponent>().Images.Add("b31f4127-a4e0-4ef4-9a65-15e343cd892f");

            await _commerceCommander.Pipeline<IPersistEntityPipeline>()
                .RunAsync(new PersistEntityArgument(bundle4), context).ConfigureAwait(false);

            foreach (var upgradeItem in upgradeComponent4.UpgradeItems)
            {
                await CreateBundleRelationship(bundle4.Id, upgradeItem, context).ConfigureAwait(false);
            }

            // Associate bundle to parent category
            await AssociateEntity(context.CommerceContext, new CatalogReferenceArgument(
                "Entity-Catalog-Habitat_Master",
                 "Entity-Category-Habitat_Master-Home Theater",
                bundle4.Id)).ConfigureAwait(false);

            // Car Audio bundle with GiftCard
            var bundle5 =
                await CreateBundle(
                    context.CommerceContext,
                    "Dynamic",
                    "6002005",
                    "CarAudioBundleWithGiftCard",
                    "Car Audio bundle with GiftCard",
                    "Sample dynamic bundle containing a gift card",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new[]
                    {
                        "bundle",
                        "dynamic",
                        "caraudio"
                    },
                    new List<BundleItem>
                    {
                        new DynamicBundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042122",
                            Quantity = 1,
                            MaximumQuantity = 1,
                            IsUpgradable = false,
                            IsOptional = true,
                            UnitPrice = new MultiCurrency { Values = {
                                    new Money("USD", 420.00M),
                                    new Money("CAD", 450.00M)
                                }
                            }
                        },
                        new DynamicBundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042115|56042115",
                            Quantity = 1,
                            MaximumQuantity = 2,
                            IsUpgradable = true,
                            IsOptional = false,
                            UnitPrice = new MultiCurrency { Values = {
                                    new Money("USD", 120.00M),
                                    new Money("CAD", 150.00M)
                                }
                            }
                        },
                        new DynamicBundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042129|56042129",
                            Quantity = 1,
                            MaximumQuantity = 3,
                            IsUpgradable = true,
                            IsOptional = true,
                            UnitPrice = new MultiCurrency { Values = {
                                    new Money("USD", 14.00M),
                                    new Money("CAD", 18.00M)
                                }
                            }
                        },
                        new DynamicBundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042986",
                            Quantity = 1,
                            MaximumQuantity = 1,
                            IsUpgradable = false,
                            IsOptional = false,
                            UnitPrice = new MultiCurrency { Values = {
                                    new Money("USD", 25.00M),
                                    new Money("CAD", 30.00M)
                                }
                            }
                        }
                    }).ConfigureAwait(false);

            var upgradeComponent5 = bundle5.GetComponent<BundleComponent>().ChildComponents.OfType<BundleItemUpgradeOptionsComponent>()
                .FirstOrDefault(i => i.SellableItemId.Equals("Entity-SellableItem-6042115|56042115", StringComparison.OrdinalIgnoreCase));
            upgradeComponent5?.UpgradeItems.Add(
                new DynamicBundleItem
                {
                    SellableItemId = "Entity-SellableItem-6042117|56042117",
                    Quantity = 1,
                    MaximumQuantity = 3,
                    IsUpgradable = false,
                    IsOptional = false,
                    UnitPrice = new MultiCurrency { Values = {
                            new Money("CAD", 1072.00M)
                        }
                    }
                }
            );

            var upgradeComponent5_1 = bundle5.GetComponent<BundleComponent>().ChildComponents.OfType<BundleItemUpgradeOptionsComponent>()
                .FirstOrDefault(i => i.SellableItemId.Equals("Entity-SellableItem-6042129|56042129", StringComparison.OrdinalIgnoreCase));
            upgradeComponent5_1?.UpgradeItems.AddRange(new List<DynamicBundleItem>
            {
                new DynamicBundleItem
                {
                    SellableItemId = "Entity-SellableItem-6042131|56042131",
                    Quantity = 1,
                    MaximumQuantity = 3,
                    IsUpgradable = false,
                    IsOptional = false,
                    UnitPrice = new MultiCurrency { Values = {
                            new Money("CAD", 94.00M)
                        }
                    }
                },
                new DynamicBundleItem
                {
                    SellableItemId = "Entity-SellableItem-6042128",
                    Quantity = 1,
                    MaximumQuantity = 3,
                    IsUpgradable = false,
                    IsOptional = false,
                    UnitPrice = new MultiCurrency { Values = {
                            new Money("CAD", 115.00M)
                        }
                    }
                }
            });    

            // Set image and list price for bundle
            bundle5.GetComponent<ImagesComponent>().Images.Add("190386FF-6D19-4F82-A5FB-D43554212E61");
            bundle5.RemovePolicy(typeof(AutomaticPricingPolicy));   

            bundle5.SetPolicy(
                new ListPricingPolicy(
                    new List<Money>
                    {
                        new Money("USD", 579.99M),
                        new Money("CAD", 649.99M)
                    }));

            await _commerceCommander.Pipeline<IPersistEntityPipeline>()
                .RunAsync(new PersistEntityArgument(bundle5), context).ConfigureAwait(false);

            foreach (var upgradeItem in upgradeComponent5.UpgradeItems)
            {
                await CreateBundleRelationship(bundle5.Id, upgradeItem, context).ConfigureAwait(false);
            }

            foreach (var upgradeItem in upgradeComponent5_1.UpgradeItems)
            {
                await CreateBundleRelationship(bundle5.Id, upgradeItem, context).ConfigureAwait(false);
            }

            // Associate bundle to parent category
            await AssociateEntity(context.CommerceContext, new CatalogReferenceArgument(
                "Entity-Catalog-Habitat_Master",
                "Entity-Category-Habitat_Master-Audio",
                bundle5.Id)).ConfigureAwait(false);
           
             // GameCube automatic pricing bundle with subscription
            var bundle6 =
                await CreateBundle(
                    context.CommerceContext,
                    "Dynamic",
                    "6002006",
                    "GameCubeDynamicBundleWithSubscription",
                    "GameCube Dynamic Bundle with Subscription",
                    "Sample dynamic bundle containing subscription with automatic pricing",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new[]
                    {
                        "dynamic",
                        "gaming",
                        "bundle"
                    },
                    new List<BundleItem>
                    {
                        new DynamicBundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042433",
                            Quantity = 1,
                            MaximumQuantity = 2,
                            IsUpgradable = false,
                            IsOptional = true
                        },
                        new DynamicBundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042431|56042431",
                            Quantity = 1,
                            MaximumQuantity = 1,
                            IsUpgradable = true,
                            IsOptional = false
                        },
                        new DynamicBundleItem
                        {
                            SellableItemId = "Entity-SellableItem-6042459|56042459",
                            Quantity = 1,
                            MaximumQuantity = 1,
                            IsUpgradable = false,
                            IsOptional = false
                        }
                    }).ConfigureAwait(false);

            var upgradeComponent6 = bundle6.GetComponent<BundleComponent>().ChildComponents.OfType<BundleItemUpgradeOptionsComponent>()
                                            .FirstOrDefault(i => i.SellableItemId.Equals("Entity-SellableItem-6042431|56042431", StringComparison.OrdinalIgnoreCase));
            upgradeComponent6?.UpgradeItems.Add(
                    new DynamicBundleItem
                    {
                        SellableItemId = "Entity-SellableItem-6042432|56042432",
                        Quantity = 1,
                        MaximumQuantity = 3,
                        IsUpgradable = false,
                        IsOptional = false
                    }
                );

            // Set image and list price for bundle
            bundle6.GetComponent<ImagesComponent>().Images.Add("1EDD55CD-03D6-44F1-90BF-FA3A00F4F4D7");

            await _commerceCommander.Pipeline<IPersistEntityPipeline>()
                .RunAsync(new PersistEntityArgument(bundle6), context).ConfigureAwait(false);

            foreach (var upgradeItem in upgradeComponent6.UpgradeItems)
            {
                await CreateBundleRelationship(bundle6.Id, upgradeItem, context).ConfigureAwait(false);
            }

            // Associate bundle to parent category
            await AssociateEntity(context.CommerceContext, new CatalogReferenceArgument(
                "Entity-Catalog-Habitat_Master",
                 "Entity-Category-Habitat_Master-Gaming",
                bundle6.Id)).ConfigureAwait(false);
        }

        private async Task AssociateEntity(CommerceContext commerceContext, CatalogReferenceArgument arg)
        {
            await _commerceCommander.ProcessWithTransaction(commerceContext,
                  () => _commerceCommander.Pipeline<IAssociateSellableItemToParentPipeline>().RunAsync(
                  arg, commerceContext.PipelineContextOptions))
              .ConfigureAwait(false);
        }

        private async Task CreateBundleRelationship(string bundleId, BundleItem bundleItem, CommercePipelineExecutionContext context)
        {
            var sellableItemId = bundleItem.SellableItemId;
            var isVariation = false;

            if (sellableItemId.Contains("|", StringComparison.OrdinalIgnoreCase))
            {
                // Variation is associated to the bundle
                var parts = sellableItemId.Split('|');

                sellableItemId = parts[0];
                isVariation = true;
            }

            // Create the bundle -> sellable item relationship
            var bundleToSellableItem = new RelationshipArgument(bundleId,
                sellableItemId,
                isVariation
                    ? CatalogConstants.BundleToSellableItemVariant
                    : CatalogConstants.BundleToSellableItem);

            await _commerceCommander.Pipeline<ICreateRelationshipPipeline>().RunAsync(bundleToSellableItem,
                    context.CommerceContext.PipelineContextOptions)
                .ConfigureAwait(false);

            // Create the sellable item -> bundle relationship to simplify cleanup
            var sellableItemToBundle = new RelationshipArgument(sellableItemId,
                bundleId,
                CatalogConstants.SellableItemToBundle);

            await _commerceCommander.Pipeline<ICreateRelationshipPipeline>().RunAsync(sellableItemToBundle,
                    context.CommerceContext.PipelineContextOptions)
                .ConfigureAwait(false);
        }

        private async Task<SellableItem> CreateBundle(
         CommerceContext commerceContext,
         string bundleType,
         string bundleId,
         string name,
         string displayName,
         string description,
         string brand = "",
         string manufacturer = "",
         string typeOfGood = "",
         string[] tags = null,
         IList<BundleItem> bundleItems = null)
        {
            var argument =
                new CreateBundleArgument(bundleType, bundleId, name, displayName, description)
                {
                    Brand = brand,
                    Manufacturer = manufacturer,
                    TypeOfGood = typeOfGood,
                    BundleItems = bundleItems
                };

            argument.Tags.AddRange(tags?.ToList() ?? new List<string>());

            var result = await _commerceCommander.ProcessWithTransaction(commerceContext,
                  () => _commerceCommander.Pipeline<ICreateBundlePipeline>().RunAsync(
                  argument, commerceContext.PipelineContextOptions))
              .ConfigureAwait(false);

            return result?.SellableItems?.FirstOrDefault();
        }
    }
}
