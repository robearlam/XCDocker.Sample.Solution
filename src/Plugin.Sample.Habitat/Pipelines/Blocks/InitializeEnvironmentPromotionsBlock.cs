﻿// © 2016 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Coupons;
using Sitecore.Commerce.Plugin.Fulfillment;
using Sitecore.Commerce.Plugin.Promotions;
using Sitecore.Commerce.Plugin.Rules;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.Habitat
{
    /// <summary>
    /// Defines a block which bootstraps promotions.
    /// </summary>
    /// <seealso>
    ///     <cref>
    ///         Sitecore.Framework.Pipelines.PipelineBlock{System.String, System.String,
    ///         Sitecore.Commerce.Core.CommercePipelineExecutionContext}
    ///     </cref>
    /// </seealso>
    [PipelineDisplayName("Habitat.InitializeEnvironmentPromotionsBlock")]
    public class InitializeEnvironmentPromotionsBlock : AsyncPolicyTriggerConditionalPipelineBlock<string, string>
    {
        private readonly IPersistEntityPipeline _persistEntityPipeline;
        private readonly IAddPromotionBookPipeline _addBookPipeline;
        private readonly IAddPromotionPipeline _addPromotionPipeline;
        private readonly IAddQualificationPipeline _addQualificationPipeline;
        private readonly IAddBenefitPipeline _addBenefitPipeline;
        private readonly IAddPublicCouponPipeline _addPublicCouponPipeline;
        private readonly IAddPromotionItemPipeline _addPromotionItemPipeline;
        private readonly IAssociateCatalogToBookPipeline _associateCatalogToBookPipeline;
        private readonly IAddPromotionFreeGiftPipeline _addFreeGiftPipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeEnvironmentPromotionsBlock"/> class.
        /// </summary>
        /// <param name="persistEntityPipeline">The persist entity pipeline.</param>
        /// <param name="addBookPipeline">The add book pipeline.</param>
        /// <param name="addPromotionPipeline">The add promotion pipeline.</param>
        /// <param name="addQualificationPipeline">The add qualification pipeline.</param>
        /// <param name="addBenefitPipeline">The add benefit pipeline.</param>
        /// <param name="addPromotionItemPipeline">The add promotion item pipeline.</param>
        /// <param name="addPublicCouponPipeline">The add public coupon pipeline.</param>
        /// <param name="associateCatalogToBookPipeline">The add public coupon pipeline.</param>
        /// <param name="addFreeGiftPipeline">The add free gift pipeline.</param>
        public InitializeEnvironmentPromotionsBlock(
            IPersistEntityPipeline persistEntityPipeline,
            IAddPromotionBookPipeline addBookPipeline,
            IAddPromotionPipeline addPromotionPipeline,
            IAddQualificationPipeline addQualificationPipeline,
            IAddBenefitPipeline addBenefitPipeline,
            IAddPromotionItemPipeline addPromotionItemPipeline,
            IAddPublicCouponPipeline addPublicCouponPipeline,
            IAssociateCatalogToBookPipeline associateCatalogToBookPipeline,
            IAddPromotionFreeGiftPipeline addFreeGiftPipeline)
        {
            _persistEntityPipeline = persistEntityPipeline;
            _addBookPipeline = addBookPipeline;
            _addPromotionPipeline = addPromotionPipeline;
            _addQualificationPipeline = addQualificationPipeline;
            _addBenefitPipeline = addBenefitPipeline;
            _addPromotionItemPipeline = addPromotionItemPipeline;
            _addPublicCouponPipeline = addPublicCouponPipeline;
            _associateCatalogToBookPipeline = associateCatalogToBookPipeline;
            _addFreeGiftPipeline = addFreeGiftPipeline;
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
            var artifactSet = "Environment.Habitat.Promotions-1.0";

            // Check if this environment has subscribed to this Artifact Set
            if (!context.GetPolicy<EnvironmentInitializationPolicy>()
                .InitialArtifactSets.Contains(artifactSet))
            {
                return arg;
            }

            context.Logger.LogInformation($"{Name}.InitializingArtifactSet: ArtifactSet={artifactSet}");

            var book =
                await _addBookPipeline.RunAsync(
                    new AddPromotionBookArgument("Habitat_PromotionBook")
                    {
                        DisplayName = "Habitat Promotion Book",
                        Description = "This is the Habitat promotion book"
                    },
                    context).ConfigureAwait(false);

            await CreateCartFreeShippingPromotion(book, context).ConfigureAwait(false);
            await CreateCartExclusive5PctOffCouponPromotion(book, context).ConfigureAwait(false);
            await CreateCartExclusive5OffCouponPromotion(book, context).ConfigureAwait(false);
            await CreateCartExclusiveOptixCameraPromotion(book, context).ConfigureAwait(false);
            await CreateCart15PctOffCouponPromotion(book, context).ConfigureAwait(false);
            await CreateDisabledPromotion(book, context).ConfigureAwait(false);

            var date = DateTimeOffset.UtcNow;
            await CreateCart10PctOffCouponPromotion(book, context, date).ConfigureAwait(false);
            Thread.Sleep(1); //// TO ENSURE CREATING DATE IS DIFFERENT BETWEEN THESE TWO PROMOTIONS
            await CreateCart10OffCouponPromotion(book, context, date).ConfigureAwait(false);

            await CreateLineTouchScreenPromotion(book, context).ConfigureAwait(false);
            await CreateLineTouchScreen5OffPromotion(book, context).ConfigureAwait(false);
            await CreateLineVistaPhonePriorityPromotion(book, context).ConfigureAwait(false);
            await CreateLineExclusiveMiraLaptopPromotion(book, context).ConfigureAwait(false);
            await CreateLineExclusive20PctOffCouponPromotion(book, context).ConfigureAwait(false);
            await CreateLineExclusive20OffCouponPromotion(book, context).ConfigureAwait(false);
            await CreateLine5PctOffCouponPromotion(book, context).ConfigureAwait(false);
            await CreateLine5OffCouponPromotion(book, context).ConfigureAwait(false);
            await CreateLineLaptopPricePromotion(book, context).ConfigureAwait(false);
            await AssociateCatalogToBook(book.Name, "Habitat_Master", context).ConfigureAwait(false);
            await CreateAutomaticFreeGiftPromotion(book, context).ConfigureAwait(false);
            await CreateManualFreeGiftPromotion(book, context).ConfigureAwait(false);

            return arg;
        }

        /// <summary>
        /// Creates the cart free shipping promotion.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task CreateCartFreeShippingPromotion(PromotionBook book, CommercePipelineExecutionContext context)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "CartFreeShippingPromotion", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1), "Free Shipping", "Free Shipping")
                    {
                        DisplayName = "Free Shipping",
                        Description = "Free shipping when Cart subtotal of $100 or more"
                    },
                    context).ConfigureAwait(false);

            promotion =
                await _addQualificationPipeline.RunAsync(
                    new PromotionConditionModelArgument(
                        promotion,
                        new ConditionModel
                        {
                            ConditionOperator = "And",
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartSubtotalCondition,
                            Name = CartsConstants.CartSubtotalCondition,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    IsOperator = true,
                                    Name = "Operator",
                                    Value = "Sitecore.Framework.Rules.DecimalGreaterThanEqualToOperator",
                                    DisplayType = "Sitecore.Framework.Rules.IBinaryOperator`2[[System.Decimal],[System.Decimal]], Sitecore.Framework.Rules.Abstractions"
                                },
                                new PropertyModel
                                {
                                    Name = "Subtotal",
                                    Value = "100",
                                    IsOperator = false,
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion =
                await _addQualificationPipeline.RunAsync(
                    new PromotionConditionModelArgument(
                        promotion,
                        new ConditionModel
                        {
                            ConditionOperator = "And",
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = FulfillmentConstants.CartHasFulfillmentCondition,
                            Name = FulfillmentConstants.CartHasFulfillmentCondition,
                            Properties = new List<PropertyModel>()
                        }),
                    context).ConfigureAwait(false);

            await _addBenefitPipeline.RunAsync(
                new PromotionActionModelArgument(
                    promotion,
                    new ActionModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        LibraryId = FulfillmentConstants.CartFreeShippingAction,
                        Name = FulfillmentConstants.CartFreeShippingAction
                    }),
                context).ConfigureAwait(false);

            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates cart exclusive 5 percent off coupon promotion.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task CreateCartExclusive5PctOffCouponPromotion(PromotionBook book, CommercePipelineExecutionContext context)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "Cart5PctOffExclusiveCouponPromotion", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddYears(1), "5% Off Cart (Exclusive Coupon)", "5% Off Cart (Exclusive Coupon)")
                    {
                        IsExclusive = true,
                        DisplayName = "5% Off Cart (Exclusive Coupon)",
                        Description = "5% off Cart with subtotal of $10 or more (Exclusive Coupon)"
                    },
                    context).ConfigureAwait(false);

            promotion =
                await _addQualificationPipeline.RunAsync(
                    new PromotionConditionModelArgument(
                        promotion,
                        new ConditionModel
                        {
                            ConditionOperator = "And",
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartAnyItemSubtotalCondition,
                            Name = CartsConstants.CartAnyItemSubtotalCondition,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    IsOperator = true,
                                    Name = "Operator",
                                    Value = "Sitecore.Framework.Rules.DecimalGreaterThanEqualToOperator",
                                    DisplayType = "Sitecore.Framework.Rules.IBinaryOperator`2[[System.Decimal],[System.Decimal]], Sitecore.Framework.Rules.Abstractions"
                                },
                                new PropertyModel
                                {
                                    Name = "Subtotal",
                                    Value = "10",
                                    IsOperator = false,
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion =
                await _addBenefitPipeline.RunAsync(
                    new PromotionActionModelArgument(
                        promotion,
                        new ActionModel
                        {
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartSubtotalPercentOffAction,
                            Name = CartsConstants.CartSubtotalPercentOffAction,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    Name = "PercentOff",
                                    Value = "5",
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion = await _addPublicCouponPipeline.RunAsync(new AddPublicCouponArgument(promotion, "HABRTRNEC5P"), context).ConfigureAwait(false);
            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the cart exclusive5 off coupon promotion.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task CreateCartExclusive5OffCouponPromotion(PromotionBook book, CommercePipelineExecutionContext context)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "Cart5OffExclusiveCouponPromotion", DateTimeOffset.UtcNow.AddDays(-3), DateTimeOffset.UtcNow.AddYears(1), "$5 Off Cart (Exclusive Coupon)", "$5 Off Cart (Exclusive Coupon)")
                    {
                        IsExclusive = true,
                        DisplayName = "$5 Off Cart (Exclusive Coupon)",
                        Description = "$5 off Cart with subtotal of $10 or more (Exclusive Coupon)"
                    },
                    context).ConfigureAwait(false);

            promotion =
                await _addQualificationPipeline.RunAsync(
                    new PromotionConditionModelArgument(
                        promotion,
                        new ConditionModel
                        {
                            ConditionOperator = "And",
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartAnyItemSubtotalCondition,
                            Name = CartsConstants.CartAnyItemSubtotalCondition,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    IsOperator = true,
                                    Name = "Operator",
                                    Value = "Sitecore.Framework.Rules.DecimalGreaterThanEqualToOperator",
                                    DisplayType = "Sitecore.Framework.Rules.IBinaryOperator`2[[System.Decimal],[System.Decimal]], Sitecore.Framework.Rules.Abstractions"
                                },
                                new PropertyModel
                                {
                                    Name = "Subtotal",
                                    Value = "10",
                                    IsOperator = false,
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion =
                await _addBenefitPipeline.RunAsync(
                    new PromotionActionModelArgument(
                        promotion,
                        new ActionModel
                        {
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartSubtotalAmountOffAction,
                            Name = CartsConstants.CartSubtotalAmountOffAction,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    Name = "AmountOff",
                                    Value = "5",
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion = await _addPublicCouponPipeline.RunAsync(new AddPublicCouponArgument(promotion, "HABRTRNEC5A"), context).ConfigureAwait(false);
            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates cart exclusive optix camera promotion.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task CreateCartExclusiveOptixCameraPromotion(PromotionBook book, CommercePipelineExecutionContext context)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "CartOptixCameraExclusivePromotion", DateTimeOffset.UtcNow.AddDays(-4), DateTimeOffset.UtcNow.AddYears(1), "Optix Camera 50% Off Cart (Exclusive)", "Optix Camera 50% Off Cart (Exclusive)")
                    {
                        IsExclusive = true,
                        DisplayName = "Optix Camera 50% Off Cart (Exclusive)",
                        Description = "50% off Cart when buying Optix Camera (Exclusive)"
                    },
                    context).ConfigureAwait(false);

            promotion = await _addPromotionItemPipeline.RunAsync(
                new PromotionItemArgument(
                    promotion,
                    "Habitat_Master|7042071|"),
                context).ConfigureAwait(false);

            await _addBenefitPipeline.RunAsync(
                new PromotionActionModelArgument(
                    promotion,
                    new ActionModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        LibraryId = CartsConstants.CartSubtotalPercentOffAction,
                        Name = CartsConstants.CartSubtotalPercentOffAction,
                        Properties = new List<PropertyModel>
                        {
                            new PropertyModel
                            {
                                Name = "PercentOff",
                                Value = "50",
                                IsOperator = false,
                                DisplayType = "System.Decimal"
                            }
                        }
                    }),
                context).ConfigureAwait(false);

            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates cart 15 percent off coupon promotion.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task CreateCart15PctOffCouponPromotion(PromotionBook book, CommercePipelineExecutionContext context)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "Cart15PctOffCouponPromotion", DateTimeOffset.UtcNow.AddDays(-5), DateTimeOffset.UtcNow.AddYears(1), "15% Off Cart (Coupon)", "15% Off Cart (Coupon)")
                    {
                        DisplayName = "15% Off Cart (Coupon)",
                        Description = "15% off Cart with subtotal of $50 or more (Coupon)"
                    },
                    context).ConfigureAwait(false);

            promotion =
                await _addQualificationPipeline.RunAsync(
                    new PromotionConditionModelArgument(
                        promotion,
                        new ConditionModel
                        {
                            ConditionOperator = "And",
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartSubtotalCondition,
                            Name = CartsConstants.CartSubtotalCondition,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    IsOperator = true,
                                    Name = "Operator",
                                    Value = "Sitecore.Framework.Rules.DecimalGreaterThanEqualToOperator",
                                    DisplayType = "Sitecore.Framework.Rules.IBinaryOperator`2[[System.Decimal],[System.Decimal]], Sitecore.Framework.Rules.Abstractions"
                                },
                                new PropertyModel
                                {
                                    Name = "Subtotal",
                                    Value = "50",
                                    IsOperator = false,
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion =
                await _addBenefitPipeline.RunAsync(
                    new PromotionActionModelArgument(
                        promotion,
                        new ActionModel
                        {
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartSubtotalPercentOffAction,
                            Name = CartsConstants.CartSubtotalPercentOffAction,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    Name = "PercentOff",
                                    Value = "15",
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion = await _addPublicCouponPipeline.RunAsync(new AddPublicCouponArgument(promotion, "HABRTRNC15P"), context).ConfigureAwait(false);
            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the cart10 PCT off coupon promotion.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        /// <param name="date">The date.</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task CreateCart10PctOffCouponPromotion(PromotionBook book, CommercePipelineExecutionContext context, DateTimeOffset date)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "Cart10PctOffCouponPromotion", date, date.AddYears(1), "10% Off Cart (Coupon)", "10% Off Cart (Coupon)")
                    {
                        DisplayName = "10% Off Cart (Coupon)",
                        Description = "10% off Cart with subtotal of $50 or more (Coupon)"
                    },
                    context).ConfigureAwait(false);

            promotion =
                await _addQualificationPipeline.RunAsync(
                    new PromotionConditionModelArgument(
                        promotion,
                        new ConditionModel
                        {
                            ConditionOperator = "And",
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartSubtotalCondition,
                            Name = CartsConstants.CartSubtotalCondition,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    IsOperator = true,
                                    Name = "Operator",
                                    Value = "Sitecore.Framework.Rules.DecimalGreaterThanEqualToOperator",
                                    DisplayType = "Sitecore.Framework.Rules.IBinaryOperator`2[[System.Decimal],[System.Decimal]], Sitecore.Framework.Rules.Abstractions"
                                },
                                new PropertyModel
                                {
                                    Name = "Subtotal",
                                    Value = "50",
                                    IsOperator = false,
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion =
                await _addBenefitPipeline.RunAsync(
                    new PromotionActionModelArgument(
                        promotion,
                        new ActionModel
                        {
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartSubtotalPercentOffAction,
                            Name = CartsConstants.CartSubtotalPercentOffAction,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    Name = "PercentOff",
                                    Value = "10",
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion = await _addPublicCouponPipeline.RunAsync(new AddPublicCouponArgument(promotion, "HABRTRNC10P"), context).ConfigureAwait(false);
            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the cart10 off coupon promotion.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        /// <param name="date">The date.</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task CreateCart10OffCouponPromotion(PromotionBook book, CommercePipelineExecutionContext context, DateTimeOffset date)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "Cart10OffCouponPromotion", date, date.AddYears(1), "$10 Off Cart (Coupon)", "$10 Off Cart (Coupon)")
                    {
                        DisplayName = "$10 Off Cart (Coupon)",
                        Description = "$10 off Cart with subtotal of $50 or more (Coupon)"
                    },
                    context).ConfigureAwait(false);

            promotion =
                await _addQualificationPipeline.RunAsync(
                    new PromotionConditionModelArgument(
                        promotion,
                        new ConditionModel
                        {
                            ConditionOperator = "And",
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartSubtotalCondition,
                            Name = CartsConstants.CartSubtotalCondition,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    IsOperator = true,
                                    Name = "Operator",
                                    Value = "Sitecore.Framework.Rules.DecimalGreaterThanEqualToOperator",
                                    DisplayType = "Sitecore.Framework.Rules.IBinaryOperator`2[[System.Decimal],[System.Decimal]], Sitecore.Framework.Rules.Abstractions"
                                },
                                new PropertyModel
                                {
                                    Name = "Subtotal",
                                    Value = "50",
                                    IsOperator = false,
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion =
                await _addBenefitPipeline.RunAsync(
                    new PromotionActionModelArgument(
                        promotion,
                        new ActionModel
                        {
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartSubtotalAmountOffAction,
                            Name = CartsConstants.CartSubtotalAmountOffAction,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    Name = "AmountOff",
                                    Value = "10",
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion = await _addPublicCouponPipeline.RunAsync(new AddPublicCouponArgument(promotion, "HABRTRNC10A"), context).ConfigureAwait(false);
            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the disabled promotion.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task CreateDisabledPromotion(PromotionBook book, CommercePipelineExecutionContext context)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "DisabledPromotion", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1), "Disabled", "Disabled")
                    {
                        DisplayName = "Disabled",
                        Description = "Disabled"
                    },
                    context).ConfigureAwait(false);

            promotion =
                await _addQualificationPipeline.RunAsync(
                    new PromotionConditionModelArgument(
                        promotion,
                        new ConditionModel
                        {
                            ConditionOperator = "And",
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartSubtotalCondition,
                            Name = CartsConstants.CartSubtotalCondition,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    IsOperator = true,
                                    Name = "Operator",
                                    Value = "Sitecore.Framework.Rules.DecimalGreaterThanEqualToOperator",
                                    DisplayType = "Sitecore.Framework.Rules.IBinaryOperator`2[[System.Decimal],[System.Decimal]], Sitecore.Framework.Rules.Abstractions"
                                },
                                new PropertyModel
                                {
                                    Name = "Subtotal",
                                    Value = "5",
                                    IsOperator = false,
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion =
                await _addBenefitPipeline.RunAsync(
                    new PromotionActionModelArgument(
                        promotion,
                        new ActionModel
                        {
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartSubtotalPercentOffAction,
                            Name = CartsConstants.CartSubtotalPercentOffAction,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    Name = "PercentOff",
                                    Value = "100",
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion.SetPolicy(new DisabledPolicy());
            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
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

        /// <summary>
        /// Creates line Touch Screen promotion.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        private async Task CreateLineTouchScreenPromotion(PromotionBook book, CommercePipelineExecutionContext context)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "LineHabitat34withTouchScreenPromotion", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1), "Habitat Touch Screen 50% Off", "Habitat Touch Screen 50% Off")
                    {
                        DisplayName = "Habitat Touch Screen 50% Off",
                        Description = "50% off the Habitat 34.0 Cubic Refrigerator with Touchscreen item"
                    },
                    context).ConfigureAwait(false);

            promotion = await _addPromotionItemPipeline.RunAsync(
                new PromotionItemArgument(
                    promotion,
                    "Habitat_Master|6042588|"),
                context).ConfigureAwait(false);

            await _addBenefitPipeline.RunAsync(
                new PromotionActionModelArgument(
                    promotion,
                    new ActionModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        LibraryId = CartsConstants.CartItemSubtotalPercentOffAction,
                        Name = CartsConstants.CartItemSubtotalPercentOffAction,
                        Properties = new List<PropertyModel>
                        {
                            new PropertyModel
                            {
                                Name = "PercentOff",
                                Value = "50",
                                IsOperator = false,
                                DisplayType = "System.Decimal"
                            },
                            new PropertyModel
                            {
                                Name = "TargetItemId",
                                Value = "Habitat_Master|6042588|",
                                IsOperator = false,
                                DisplayType = "System.String"
                            }
                        }
                    }),
                context).ConfigureAwait(false);

            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the line laptop price promotion.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        /// <returns>Task.</returns>
        private async Task CreateLineLaptopPricePromotion(PromotionBook book, CommercePipelineExecutionContext context)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "LineHabitatLaptopPricePromotion", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1), "Pay only $5", "Pay only $5")
                    {
                        DisplayName = "Pay only $5",
                        Description = "Pay only $5"
                    },
                    context).ConfigureAwait(false);

            promotion = await _addPromotionItemPipeline.RunAsync(
                new PromotionItemArgument(
                    promotion,
                    "Habitat_Master|6042178|"),
                context).ConfigureAwait(false);

            await _addBenefitPipeline.RunAsync(
                new PromotionActionModelArgument(
                    promotion,
                    new ActionModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        LibraryId = CartsConstants.CartItemSellPriceAction,
                        Name = CartsConstants.CartItemSellPriceAction,
                        Properties = new List<PropertyModel>
                        {
                            new PropertyModel
                            {
                                Name = "SellPrice",
                                Value = "5",
                                IsOperator = false,
                                DisplayType = "System.Decimal"
                            },
                            new PropertyModel
                            {
                                Name = "TargetItemId",
                                Value = "Habitat_Master|6042178|",
                                IsOperator = false,
                                DisplayType = "System.String"
                            }
                        }
                    }),
                context).ConfigureAwait(false);

            promotion = await _addPublicCouponPipeline.RunAsync(new AddPublicCouponArgument(promotion, "HABSELLPRICE"), context).ConfigureAwait(false);
            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the line TOuch Screen 5 off promotion.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task CreateLineTouchScreen5OffPromotion(PromotionBook book, CommercePipelineExecutionContext context)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "LineHabitat34withTouchScreen5OffPromotion", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddYears(1), "Habitat Touch Screen $5 Off Item", "Habitat Touch Screen $5 Off Item")
                    {
                        DisplayName = "Habitat Touch Screen $5 Off",
                        Description = "$5 off the Habitat 34.0 Cubic Refrigerator with Touchscreen item"
                    },
                    context).ConfigureAwait(false);

            promotion = await _addPromotionItemPipeline.RunAsync(
                new PromotionItemArgument(
                    promotion,
                    "Habitat_Master|6042588|"),
                context).ConfigureAwait(false);

            await _addBenefitPipeline.RunAsync(
                new PromotionActionModelArgument(
                    promotion,
                    new ActionModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        LibraryId = CartsConstants.CartItemSubtotalAmountOffAction,
                        Name = CartsConstants.CartItemSubtotalAmountOffAction,
                        Properties = new List<PropertyModel>
                        {
                            new PropertyModel
                            {
                                Name = "AmountOff",
                                Value = "5",
                                IsOperator = false,
                                DisplayType = "System.Decimal"
                            },
                            new PropertyModel
                            {
                                Name = "TargetItemId",
                                Value = "Habitat_Master|6042588|",
                                IsOperator = false,
                                DisplayType = "System.String"
                            }
                        }
                    }),
                context).ConfigureAwait(false);

            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }

        private async Task CreateLineVistaPhonePriorityPromotion(PromotionBook book, CommercePipelineExecutionContext context)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "LineVistaPhonePriority20OffPromotion", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddYears(1), "Habitat Vista Phone 20% Off Item", "Habitat Vista Phone 20% Off Item")
                    {
                        DisplayName = "Habitat Vista Phone 20% Off",
                        Description = "20% off the Habitat Vista 550 Flip Phone with 1.8MP Camera item",
                        Priority = 5
                    },
                    context).ConfigureAwait(false);

            promotion = await _addPromotionItemPipeline.RunAsync(
                new PromotionItemArgument(
                    promotion,
                    "Habitat_Master|6042335|"),
                context).ConfigureAwait(false);

            await _addBenefitPipeline.RunAsync(
                new PromotionActionModelArgument(
                    promotion,
                    new ActionModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        LibraryId = CartsConstants.CartItemSubtotalPercentOffAction,
                        Name = CartsConstants.CartItemSubtotalPercentOffAction,
                        Properties = new List<PropertyModel>
                        {
                            new PropertyModel
                            {
                                Name = "PercentOff",
                                Value = "20",
                                IsOperator = false,
                                DisplayType = "System.Decimal"
                            },
                            new PropertyModel
                            {
                                Name = "TargetItemId",
                                Value = "Habitat_Master|6042335|",
                                IsOperator = false,
                                DisplayType = "System.String"
                            }
                        }
                    }),
                context).ConfigureAwait(false);

            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the line mire laptop exclusive promotion.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task CreateLineExclusiveMiraLaptopPromotion(PromotionBook book, CommercePipelineExecutionContext context)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "LineMiraLaptopExclusivePromotion", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddYears(1), "Mira Laptop 50% Off Item (Exclusive)", "Mira Laptop 50% Off Item (Exclusive)")
                    {
                        DisplayName = "Mira Laptop 50% Off Item (Exclusive)",
                        Description = "50% off the Mira Laptop item (Exclusive)",
                        IsExclusive = true
                    },
                    context).ConfigureAwait(false);

            promotion = await _addPromotionItemPipeline.RunAsync(
                new PromotionItemArgument(
                    promotion,
                    "Habitat_Master|6042179|"),
                context).ConfigureAwait(false);

            await _addBenefitPipeline.RunAsync(
                new PromotionActionModelArgument(
                    promotion,
                    new ActionModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        LibraryId = CartsConstants.CartItemSubtotalPercentOffAction,
                        Name = CartsConstants.CartItemSubtotalPercentOffAction,
                        Properties = new List<PropertyModel>
                        {
                            new PropertyModel
                            {
                                Name = "PercentOff",
                                Value = "50",
                                IsOperator = false,
                                DisplayType = "System.Decimal"
                            },
                            new PropertyModel
                            {
                                Name = "TargetItemId",
                                Value = "Habitat_Master|6042179|",
                                IsOperator = false,
                                DisplayType = "System.String"
                            }
                        }
                    }),
                context).ConfigureAwait(false);

            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates line exclusive 20 percent off coupon promotion.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task CreateLineExclusive20PctOffCouponPromotion(PromotionBook book, CommercePipelineExecutionContext context)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "Line20PctOffExclusiveCouponPromotion", DateTimeOffset.UtcNow.AddDays(-3), DateTimeOffset.UtcNow.AddYears(1), "20% Off Item (Exclusive Coupon)", "20% Off Item (Exclusive Coupon)")
                    {
                        IsExclusive = true,
                        DisplayName = "20% Off Item (Exclusive Coupon)",
                        Description = "20% off any item with subtotal of $50 or more (Exclusive Coupon)"
                    },
                    context).ConfigureAwait(false);

            promotion =
                await _addQualificationPipeline.RunAsync(
                    new PromotionConditionModelArgument(
                        promotion,
                        new ConditionModel
                        {
                            ConditionOperator = "And",
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartAnyItemSubtotalCondition,
                            Name = CartsConstants.CartAnyItemSubtotalCondition,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    IsOperator = true,
                                    Name = "Operator",
                                    Value = "Sitecore.Framework.Rules.DecimalGreaterThanEqualToOperator",
                                    DisplayType = "Sitecore.Framework.Rules.IBinaryOperator`2[[System.Decimal],[System.Decimal]], Sitecore.Framework.Rules.Abstractions"
                                },
                                new PropertyModel
                                {
                                    Name = "Subtotal",
                                    Value = "25",
                                    IsOperator = false,
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion =
                await _addBenefitPipeline.RunAsync(
                    new PromotionActionModelArgument(
                        promotion,
                        new ActionModel
                        {
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartAnyItemSubtotalPercentOffAction,
                            Name = CartsConstants.CartAnyItemSubtotalPercentOffAction,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    Name = "PercentOff",
                                    Value = "20",
                                    DisplayType = "System.Decimal"
                                },
                                new PropertyModel
                                {
                                    IsOperator = true,
                                    Name = "Operator",
                                    Value = "Sitecore.Framework.Rules.DecimalGreaterThanEqualToOperator",
                                    DisplayType = "Sitecore.Framework.Rules.IBinaryOperator`2[[System.Decimal],[System.Decimal]], Sitecore.Framework.Rules.Abstractions"
                                },
                                new PropertyModel
                                {
                                    Name = "Subtotal",
                                    Value = "25",
                                    IsOperator = false,
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion = await _addPublicCouponPipeline.RunAsync(new AddPublicCouponArgument(promotion, "HABRTRNEL20P"), context).ConfigureAwait(false);
            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the line exclusive $20 off coupon promotion.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task CreateLineExclusive20OffCouponPromotion(PromotionBook book, CommercePipelineExecutionContext context)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "Line20OffExclusiveCouponPromotion", DateTimeOffset.UtcNow.AddDays(-4), DateTimeOffset.UtcNow.AddYears(1), "$20 Off Item (Exclusive Coupon)", "$20 Off Item (Exclusive Coupon)")
                    {
                        IsExclusive = true,
                        DisplayName = "$20 Off Item (Exclusive Coupon)",
                        Description = "$20 off any item with subtotal of $50 or more (Exclusive Coupon)"
                    },
                    context).ConfigureAwait(false);

            promotion =
                await _addQualificationPipeline.RunAsync(
                    new PromotionConditionModelArgument(
                        promotion,
                        new ConditionModel
                        {
                            ConditionOperator = "And",
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartAnyItemSubtotalCondition,
                            Name = CartsConstants.CartAnyItemSubtotalCondition,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    IsOperator = true,
                                    Name = "Operator",
                                    Value = "Sitecore.Framework.Rules.DecimalGreaterThanEqualToOperator",
                                    DisplayType = "Sitecore.Framework.Rules.IBinaryOperator`2[[System.Decimal],[System.Decimal]], Sitecore.Framework.Rules.Abstractions"
                                },
                                new PropertyModel
                                {
                                    Name = "Subtotal",
                                    Value = "25",
                                    IsOperator = false,
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion =
                await _addBenefitPipeline.RunAsync(
                    new PromotionActionModelArgument(
                        promotion,
                        new ActionModel
                        {
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartAnyItemSubtotalAmountOffAction,
                            Name = CartsConstants.CartAnyItemSubtotalAmountOffAction,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    Name = "AmountOff",
                                    Value = "20",
                                    DisplayType = "System.Decimal"
                                },
                                new PropertyModel
                                {
                                    IsOperator = true,
                                    Name = "Operator",
                                    Value = "Sitecore.Framework.Rules.DecimalGreaterThanEqualToOperator",
                                    DisplayType = "Sitecore.Framework.Rules.IBinaryOperator`2[[System.Decimal],[System.Decimal]], Sitecore.Framework.Rules.Abstractions"
                                },
                                new PropertyModel
                                {
                                    Name = "Subtotal",
                                    Value = "25",
                                    IsOperator = false,
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion = await _addPublicCouponPipeline.RunAsync(new AddPublicCouponArgument(promotion, "HABRTRNEL20A"), context).ConfigureAwait(false);
            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates line 5 percent off coupon promotion.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task CreateLine5PctOffCouponPromotion(PromotionBook book, CommercePipelineExecutionContext context)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "Line5PctOffCouponPromotion", DateTimeOffset.UtcNow.AddDays(-5), DateTimeOffset.UtcNow.AddYears(1), "5% Off Item (Coupon)", "5% Off Item (Coupon)")
                    {
                        DisplayName = "5% Off Item (Coupon)",
                        Description = "5% off any item with subtotal of 10$ or more (Coupon)"
                    },
                    context).ConfigureAwait(false);

            promotion =
                await _addQualificationPipeline.RunAsync(
                    new PromotionConditionModelArgument(
                        promotion,
                        new ConditionModel
                        {
                            ConditionOperator = "And",
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartAnyItemSubtotalCondition,
                            Name = CartsConstants.CartAnyItemSubtotalCondition,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    IsOperator = true,
                                    Name = "Operator",
                                    Value = "Sitecore.Framework.Rules.DecimalGreaterThanEqualToOperator",
                                    DisplayType = "Sitecore.Framework.Rules.IBinaryOperator`2[[System.Decimal],[System.Decimal]], Sitecore.Framework.Rules.Abstractions"
                                },
                                new PropertyModel
                                {
                                    Name = "Subtotal",
                                    Value = "10",
                                    IsOperator = false,
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion =
                await _addBenefitPipeline.RunAsync(
                    new PromotionActionModelArgument(
                        promotion,
                        new ActionModel
                        {
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartAnyItemSubtotalPercentOffAction,
                            Name = CartsConstants.CartAnyItemSubtotalPercentOffAction,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    Name = "PercentOff",
                                    Value = "5",
                                    DisplayType = "System.Decimal"
                                },
                                new PropertyModel
                                {
                                    IsOperator = true,
                                    Name = "Operator",
                                    Value = "Sitecore.Framework.Rules.DecimalGreaterThanEqualToOperator",
                                    DisplayType = "Sitecore.Framework.Rules.IBinaryOperator`2[[System.Decimal],[System.Decimal]], Sitecore.Framework.Rules.Abstractions"
                                },
                                new PropertyModel
                                {
                                    Name = "Subtotal",
                                    Value = "10",
                                    IsOperator = false,
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion = await _addPublicCouponPipeline.RunAsync(new AddPublicCouponArgument(promotion, "HABRTRNL5P"), context).ConfigureAwait(false);
            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates line 5 amount off coupon promotion.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="context">The context.</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task CreateLine5OffCouponPromotion(PromotionBook book, CommercePipelineExecutionContext context)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "Line5OffCouponPromotion", DateTimeOffset.UtcNow.AddDays(-6), DateTimeOffset.UtcNow.AddYears(1), "$5 Off Item (Coupon)", "$5 Off Item (Coupon)")
                    {
                        DisplayName = "$5 Off Item (Coupon)",
                        Description = "$5 off any item with subtotal of $10 or more (Coupon)"
                    },
                    context).ConfigureAwait(false);

            promotion =
                await _addQualificationPipeline.RunAsync(
                    new PromotionConditionModelArgument(
                        promotion,
                        new ConditionModel
                        {
                            ConditionOperator = "And",
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartAnyItemSubtotalCondition,
                            Name = CartsConstants.CartAnyItemSubtotalCondition,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    IsOperator = true,
                                    Name = "Operator",
                                    Value = "Sitecore.Framework.Rules.DecimalGreaterThanEqualToOperator",
                                    DisplayType = "Sitecore.Framework.Rules.IBinaryOperator`2[[System.Decimal],[System.Decimal]], Sitecore.Framework.Rules.Abstractions"
                                },
                                new PropertyModel
                                {
                                    Name = "Subtotal",
                                    Value = "10",
                                    IsOperator = false,
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion =
                await _addBenefitPipeline.RunAsync(
                    new PromotionActionModelArgument(
                        promotion,
                        new ActionModel
                        {
                            Id = Guid.NewGuid().ToString(),
                            LibraryId = CartsConstants.CartAnyItemSubtotalAmountOffAction,
                            Name = CartsConstants.CartAnyItemSubtotalAmountOffAction,
                            Properties = new List<PropertyModel>
                            {
                                new PropertyModel
                                {
                                    Name = "AmountOff",
                                    Value = "5",
                                    DisplayType = "System.Decimal"
                                },
                                new PropertyModel
                                {
                                    IsOperator = true,
                                    Name = "Operator",
                                    Value = "Sitecore.Framework.Rules.DecimalGreaterThanEqualToOperator",
                                    DisplayType = "Sitecore.Framework.Rules.IBinaryOperator`2[[System.Decimal],[System.Decimal]], Sitecore.Framework.Rules.Abstractions"
                                },
                                new PropertyModel
                                {
                                    Name = "Subtotal",
                                    Value = "10",
                                    IsOperator = false,
                                    DisplayType = "System.Decimal"
                                }
                            }
                        }),
                    context).ConfigureAwait(false);

            promotion = await _addPublicCouponPipeline.RunAsync(new AddPublicCouponArgument(promotion, "HABRTRNL5A"), context).ConfigureAwait(false);
            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }

        private async Task CreateAutomaticFreeGiftPromotion(PromotionBook book, CommercePipelineExecutionContext context)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "Odyssey64GB4GAutoFreeGift", DateTimeOffset.UtcNow.AddDays(-6), DateTimeOffset.UtcNow.AddYears(1), "Wireless Skinnybuds Headphones Free Gift.", "Wireless Skinnybuds Headphones Free Gift.")
                    {
                        DisplayName = "Odyssey 64 GB 4G LTE Free Gift",
                        Description = "Get Wireless Skinnybuds Headphones when you buy an Odyssey 64 GB 4G LTE."
                    },
                    context).ConfigureAwait(false);

            promotion = await _addPromotionItemPipeline.RunAsync(
                new PromotionItemArgument(
                    promotion,
                    "Habitat_Master|6042318|"),
                context).ConfigureAwait(false);

            await _addBenefitPipeline.RunAsync(
                new PromotionActionModelArgument(
                    promotion,
                    new ActionModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        LibraryId = nameof(CartFreeGiftAction),
                        Name = nameof(CartFreeGiftAction),
                        Properties = new List<PropertyModel>
                        {
                            new PropertyModel
                            {
                                Name = "AddToCartAutomatically",
                                Value = "True",
                                IsOperator = false,
                                DisplayType = "System.String"
                            },
                            new PropertyModel
                            {
                                Name = "MaximumQuantity",
                                Value = "0",
                                IsOperator = false,
                                DisplayType = "System.String"
                            }
                        }
                    }),
                context).ConfigureAwait(false);

            await _addFreeGiftPipeline.RunAsync(
                new PromotionFreeGiftArgument(promotion, "Habitat_Master|6042061|56042061"), context.CommerceContext.PipelineContextOptions).ConfigureAwait(false);

            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }

        private async Task CreateManualFreeGiftPromotion(PromotionBook book, CommercePipelineExecutionContext context)
        {
            var promotion =
                await _addPromotionPipeline.RunAsync(
                    new AddPromotionArgument(book, "Odyssey16GB4GManualFreeGift", DateTimeOffset.UtcNow.AddDays(-6), DateTimeOffset.UtcNow.AddYears(1), "Odyssey 16GB 4G Free Gift Selection.", "Odyssey 16GB 4G Free Gift Selection.")
                    {
                        DisplayName = "Odyssey 16 GB 4G LTE Free Gift selection",
                        Description = "Get Free Headphones when you buy an Odyssey 16 GB 4G."
                    },
                    context).ConfigureAwait(false);

            promotion = await _addPromotionItemPipeline.RunAsync(
                new PromotionItemArgument(
                    promotion,
                    "Habitat_Master|6042308|"),
                context).ConfigureAwait(false);

            await _addBenefitPipeline.RunAsync(
                new PromotionActionModelArgument(
                    promotion,
                    new ActionModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        LibraryId = nameof(CartFreeGiftAction),
                        Name = nameof(CartFreeGiftAction),
                        Properties = new List<PropertyModel>
                        {
                            new PropertyModel
                            {
                                Name = "AddToCartAutomatically",
                                Value = "False",
                                IsOperator = false,
                                DisplayType = "System.String"
                            },
                            new PropertyModel
                            {
                                Name = "MaximumQuantity",
                                Value = "2",
                                IsOperator = false,
                                DisplayType = "System.String"
                            }
                        }
                    }),
                context).ConfigureAwait(false);

            await _addFreeGiftPipeline.RunAsync(
                new PromotionFreeGiftArgument(promotion, "Habitat_Master|6042063|56042063"), context.CommerceContext.PipelineContextOptions).ConfigureAwait(false);

            await _addFreeGiftPipeline.RunAsync(
                new PromotionFreeGiftArgument(promotion, "Habitat_Master|6042062|56042062"), context.CommerceContext.PipelineContextOptions).ConfigureAwait(false);

            await _addFreeGiftPipeline.RunAsync(
                new PromotionFreeGiftArgument(promotion, "Habitat_Master|6042069|56042069"), context.CommerceContext.PipelineContextOptions).ConfigureAwait(false);

            promotion.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
            await _persistEntityPipeline.RunAsync(new PersistEntityArgument(promotion), context).ConfigureAwait(false);
        }
    }
}
