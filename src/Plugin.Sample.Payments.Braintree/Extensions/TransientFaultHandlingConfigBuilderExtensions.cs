// © 2020 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.TransientFaultHandling;
using Sitecore.Framework.TransientFaultHandling.EntLib;

namespace Plugin.Sample.Payments.Braintree
{
    /// <summary>
    /// Contains extension methods for <see cref="ITransientFaultHandlingConfigBuilder"/>.
    /// </summary>
    public static class TransientFaultHandlingConfigBuilderExtensions
    {
        /// <summary>
        /// Adds Transient Fault Handling support for Braintree payment provider.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="ITransientFaultHandlingConfigBuilder"/> to configure Braintree payment provider.
        /// </param>
        /// <param name="retryStrategy">
        ///     The <see cref="RetryStrategy"/> for transient errors occuring during requests to Braintree.
        /// </param>
        /// <returns>The <paramref name="builder"/>.</returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="builder"/> or <paramref name="retryStrategy"/> is <see langword="null"/>.
        /// </exception>
        public static ITransientFaultHandlingConfigBuilder AddTransientFaultHandlingForBraintree(this ITransientFaultHandlingConfigBuilder builder, RetryStrategy retryStrategy)
        {
            Condition.Requires(builder, nameof(builder)).IsNotNull();
            Condition.Requires(retryStrategy, nameof(retryStrategy)).IsNotNull();

            return builder.AddRetryer(PaymentsBraintreeConstants.BraintreeRetryerName, IsTransientError, retryStrategy);
        }

        private static bool IsTransientError(Exception ex)
        {
            bool isWebEx = ex is WebException;
            bool isWebSocketEx = ex is WebSocketException;
            bool isHttpRequestEx = ex is HttpRequestException;

            return isWebEx || isWebSocketEx || isHttpRequestEx;
        }
    }
}
