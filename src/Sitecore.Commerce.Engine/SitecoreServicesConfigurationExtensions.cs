﻿// © 2018 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using Microsoft.Extensions.DependencyInjection;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Coupons;
using Sitecore.Commerce.Plugin.Fulfillment;
using Sitecore.Commerce.Plugin.Payments;
using Sitecore.Commerce.Plugin.Promotions;
using Sitecore.Commerce.Plugin.Tax;
using Sitecore.Framework.Configuration;
using Sitecore.Framework.Pipelines.Definitions.Extensions;

namespace Sitecore.Commerce.Engine
{
    /// <summary>
    /// The sitecore services configuration xtensions.
    /// </summary>
    public static class SitecoreServiceConfigurationExtensions
    {
        /// <summary>
        /// The configure commerce pipelines.
        /// </summary>
        /// <param name="services">
        /// The sitecore services configuration.
        /// </param>
        /// <returns>
        /// The <see cref="ISitecoreServicesConfiguration"/>.
        /// </returns>
        public static ISitecoreServicesConfiguration ConfigureCommercePipelines(this ISitecoreServicesConfiguration services)
        {
            services.Pipelines(config => config
                .ConfigurePipeline<IPopulateValidateCartPipeline>(builder => builder
                    .Add<ValidateCartCouponsBlock>().After<PopulateCartLineItemsBlock>())
                .ConfigurePipeline<ICalculateCartPipeline>(builder => builder
                    .Add<CalculateCartSubTotalsBlock>()
                    .Add<CalculateCartLinesFulfillmentBlock>()
                    .Add<CalculateCartFulfillmentBlock>()
                    .Add<CalculateCartPromotionsBlock>()
                    .Add<RemoveUnwatedFreeGiftsFromCartBlock>()
                    .Add<CalculateCartLinesTaxBlock>()
                    .Add<CalculateCartTaxBlock>()
                    .Add<CalculateCartTotalsBlock>()
                    .Add<CalculateCartPaymentsBlock>()
                    .Add<WriteCartTotalsToContextBlock>())
                .ConfigurePipeline<IAddPaymentsPipeline>(builder =>
                    builder.Add<ValidateCartHasFulfillmentBlock>().After<ValidateCartAndPaymentsBlock>()));

            return services;
        }
    }
}
