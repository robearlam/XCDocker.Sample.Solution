// © 2016 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Braintree;
using Braintree.Exceptions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Orders;
using Sitecore.Commerce.Plugin.Payments;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using Sitecore.Framework.TransientFaultHandling;

namespace Plugin.Sample.Payments.Braintree
{
    /// <summary>
    ///  Defines a block which creates a payment service transaction.
    /// </summary>  
    /// <seealso>
    ///   <cref>
    /// Sitecore.Framework.Pipelines.PipelineBlock{Sitecore.Commerce.Plugin.Orders.CartEmailArgument, Sitecore.Commerce.Plugin.Orders.CartEmailArgument, Sitecore.Commerce.Core.CommercePipelineExecutionContext}
    /// </cref>
    /// </seealso>
    [PipelineDisplayName(PaymentsBraintreeConstants.CreateFederatedPaymentBlock)]
    public class CreateFederatedPaymentBlock : AsyncPipelineBlock<CartEmailArgument, CartEmailArgument, CommercePipelineExecutionContext>
    {
        private readonly IRetryer _retryer;
        private readonly IBraintreeGateway _gateway;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateFederatedPaymentBlock" /> class.
        /// </summary>
        /// <param name="registry">The <see cref="IRetryerRegistry"/> to access <see cref="IRetryer"/>.</param>
        /// <param name="gateway">The gateway for Braintree to execute payments.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="registry"/> or <paramref name="gateway"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     No <see cref="IRetryer"/> with name <see cref="PaymentsBraintreeConstants.BraintreeRetryerName"/> is registered.
        /// </exception>
        public CreateFederatedPaymentBlock(IRetryerRegistry registry, IBraintreeGateway gateway)
        {
            Condition.Requires(registry, nameof(registry)).IsNotNull();
            Condition.Requires(gateway, nameof(gateway)).IsNotNull();

            if (!registry.TryGet(PaymentsBraintreeConstants.BraintreeRetryerName, out IRetryer retryer))
            {
                throw new InvalidOperationException($"{nameof(IRetryer)} with name {PaymentsBraintreeConstants.BraintreeRetryerName} is not registered.");
            }

            _retryer = retryer;
            _gateway = gateway;
        }

        /// <summary>
        /// Runs the specified argument.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// A cart with federate payment component
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="arg"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        public override async Task<CartEmailArgument> RunAsync(CartEmailArgument arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{Name}: The cart can not be null");
            Condition.Requires(context, nameof(context)).IsNotNull();

            var cart = arg.Cart;
            if (cart == null || !cart.HasComponent<FederatedPaymentComponent>())
            {
                return arg;
            }

            var payment = cart.GetComponent<FederatedPaymentComponent>();
            if (string.IsNullOrEmpty(payment.PaymentMethodNonce))
            {
                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Error,
                        PaymentsBraintreeConstants.InvalidOrMissingPropertyValueTermKey,
                        new object[]
                        {
                            "PaymentMethodNonce"
                        },
                        "Invalid or missing value for property 'PaymentMethodNonce'.").ConfigureAwait(false),
                    context);

                return arg;
            }

            var braintreeClientPolicy = context.GetPolicy<BraintreeClientPolicy>();
            if (!(await braintreeClientPolicy.IsValid(context.CommerceContext).ConfigureAwait(false)))
            {
                return arg;
            }

            try
            {
                Result<Transaction> result;

                _gateway.Environment = global::Braintree.Environment.ParseEnvironment(braintreeClientPolicy.Environment);
                _gateway.MerchantId = braintreeClientPolicy.MerchantId;
                _gateway.PublicKey = braintreeClientPolicy.PublicKey;
                _gateway.PrivateKey = braintreeClientPolicy.PrivateKey;

                var request = new TransactionRequest
                {
                    Amount = payment.Amount.Amount,
                    PaymentMethodNonce = payment.PaymentMethodNonce,
                    BillingAddress = ComponentsHelper.TranslatePartyToAddressRequest(payment.BillingParty),
                    Options = new TransactionOptionsRequest
                    {
                        SubmitForSettlement = false
                    }
                };

                if (_retryer == null)
                {
                    result = await _gateway.Transaction.SaleAsync(request).ConfigureAwait(false);
                }
                else
                {
                    result = await _retryer.ExecuteAsync(async () => await _gateway.Transaction.SaleAsync(request).ConfigureAwait(false), CancellationToken.None).ConfigureAwait(false);
                }

                if (result.IsSuccess())
                {
                    var transaction = result.Target;
                    payment.TransactionId = transaction?.Id;
                    payment.TransactionStatus = transaction?.Status?.ToString();
                    payment.PaymentInstrumentType = transaction?.PaymentInstrumentType?.ToString();

                    var cc = transaction?.CreditCard;
                    payment.MaskedNumber = cc?.MaskedNumber;
                    payment.CardType = cc?.CardType?.ToString();

                    bool validMonth = int.TryParse(cc?.ExpirationMonth, NumberStyles.Any, CultureInfo.InvariantCulture, out int month);
                    bool validYear = int.TryParse(cc?.ExpirationYear, NumberStyles.Any, CultureInfo.InvariantCulture, out int year);

                    if (validMonth)
                    {
                        payment.ExpiresMonth = month;
                    }

                    if (validYear)
                    {
                        payment.ExpiresYear = year;
                    }
                }
                else
                {
                    var errorMessages = string.Concat(result.Message, " ", result.Errors.DeepAll().Aggregate(string.Empty, (current, error) => current + ("Error: " + (int)error.Code + " - " + error.Message + "\n")));
                    context.Abort(
                        await context.CommerceContext.AddMessage(
                            context.GetPolicy<KnownResultCodes>().Error,
                            PaymentsBraintreeConstants.CreatePaymentFailedTermKey,
                            new object[]
                            {
                                "PaymentMethodNonce"
                            },
                            $"{Name}. Create payment failed :{errorMessages}").ConfigureAwait(false),
                        context);
                }

                return arg;
            }
            catch (BraintreeException ex)
            {
                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Error,
                        PaymentsBraintreeConstants.CreatePaymentFailedTermKey,
                        new object[]
                        {
                            "PaymentMethodNonce",
                            ex
                        },
                        $"{Name}. Create payment failed.").ConfigureAwait(false),
                    context);
                return arg;
            }
            catch (Exception ex)
            {
                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Error,
                        PaymentsBraintreeConstants.PaymentProcessingFailedTermKey,
                        new object[]
                        {
                            ex
                        },
                        $"{Name}. Braintree payment processing failed.").ConfigureAwait(false),
                    context);
                return arg;
            }
        }
    }
}
