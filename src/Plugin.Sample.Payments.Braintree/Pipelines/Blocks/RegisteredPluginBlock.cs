﻿// © 2016 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.Payments.Braintree
{
    /// <summary>
    /// Defines the registered plugin block.
    /// </summary>
    /// <seealso>
    ///     <cref>
    ///         Sitecore.Framework.Pipelines.PipelineBlock{System.Collections.Generic.IEnumerable{Sitecore.Commerce.Core.RegisteredPluginModel},
    ///         System.Collections.Generic.IEnumerable{Sitecore.Commerce.Core.RegisteredPluginModel},
    ///         Sitecore.Commerce.Core.CommercePipelineExecutionContext}
    ///     </cref>
    /// </seealso>
    [PipelineDisplayName(PaymentsBraintreeConstants.RegisteredPluginBlock)]
    public class RegisteredPluginBlock : SyncPipelineBlock<IEnumerable<RegisteredPluginModel>, IEnumerable<RegisteredPluginModel>, CommercePipelineExecutionContext>
    {
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
        /// The list of <see cref="RegisteredPluginModel"/>
        /// </returns>
        public override IEnumerable<RegisteredPluginModel> Run(IEnumerable<RegisteredPluginModel> arg, CommercePipelineExecutionContext context)
        {
            if (arg == null)
            {
                return null;
            }

            var plugins = arg.ToList();
            PluginHelper.RegisterPlugin(this, plugins);

            return plugins.AsEnumerable();
        }
    }
}
