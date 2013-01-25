﻿using System.Configuration;
using Nancy;
using SpreedlyCoreSharp.Domain;
using SpreedlyCoreSharp.Html;
using SpreedlyCoreSharp.Request;

namespace SpreedlyCoreSharp.WebSample.Modules
{
    public class Payment3DSecureModule : NancyModule
    {
        public Payment3DSecureModule(ICoreService service): base("/3d-secure")
        {
            Get["/"] = parameters =>
            {
                return View["TakePayment", new TransactionViewModel()
                {
                    ApiLogin = service.APILogin,
                    RedirectUrl = ConfigurationManager.AppSettings["PublicWebUrl"] + "/3d-secure/redirect"
                }];
            };

            // First redirect from Core saying further processing
            // We then redirect or show the 3d secure form
            Get["/redirect"] = parameters =>
            {
                var transaction = service.ProcessPayment(new ProcessPaymentRequest
                {
                    Attempt3DSecure = true,
                    Amount = 100,
                    CurrencyCode = CurrencyCode.GBP,
                    PaymentMethodToken = Request.Query.token,
                    RedirectUrl = ConfigurationManager.AppSettings["PublicWebUrl"] + "/3d-secure/redirect-after",
                    CallbackUrl = ConfigurationManager.AppSettings["PublicWebUrl"] + "/3d-secure/callback"
                });

                if (transaction.State == "pending" && !string.IsNullOrWhiteSpace(transaction.CheckoutUrl))
                {
                    return Response.AsRedirect(transaction.RedirectUrl);
                }

                if (transaction.State == "pending" && !string.IsNullOrWhiteSpace(transaction.CheckoutForm))
                {
                    return View["Transaction3DSecureForm", transaction.CheckoutForm];
                }

                if (transaction.Succeeded)
                {
                    return View["Success"];
                }

                var viewModel = new TransactionViewModel();

                viewModel.PopulateFromTransaction(transaction, ConfigurationManager.AppSettings["PublicWebUrl"] + "/3d-secure/redirect", service.APILogin);

                return View["TakePayment", viewModel];
            };

            // Redirect after 3d secure
            Get["/redirect-after"] = parameters =>
            {
                Transaction transaction = service.GetTransaction(Request.Query.transaction_token);

                if (transaction.Succeeded)
                {
                    return View["Success"];
                }

                var viewModel = new TransactionViewModel();

                viewModel.PopulateFromTransaction(transaction, ConfigurationManager.AppSettings["PublicWebUrl"] + "/3d-secure/redirect", service.APILogin);

                return View["TakePayment", viewModel];
            };

            // Callback after 3d secure, happens if something goes wrong/cancels also
            Post["/callback"] = parameters =>
            {
                return "Pingback for token: " + Request.Query.token;
            };
        }
    }
}