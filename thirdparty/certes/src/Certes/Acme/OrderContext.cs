using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme.Models;
using Certes.Acme.Resource;
using Certes.Jws;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the context for ACME order operations.
    /// </summary>
    /// <seealso cref="Certes.Acme.IOrderContext" />
    internal class OrderContext : EntityContext<Order>, IOrderContext
    {
        public OrderContext(
            IAcmeContext context,
            Uri location)
            : base(context, location)
        {
        }

        /// <summary>
        /// Gets the authorizations for this order.
        /// </summary>
        /// <returns>
        /// The list of authorizations.
        /// </returns>
        public async Task<IEnumerable<IAuthorizationContext>> Authorizations()
        {
            var order = await Resource(AcmeJsonContext.Default.Order);
            return order
                .Authorizations?
                .Select(a => new AuthorizationContext(Context, a)) ??
                Enumerable.Empty<IAuthorizationContext>();
        }

        /// <summary>
        /// Finalizes the certificate order.
        /// </summary>
        /// <param name="csr">The CSR in DER.</param>
        /// <returns>
        /// The order finalized.
        /// </returns>
        public async Task<Order> Finalize(byte[] csr)
        {
            var order = await Resource(AcmeJsonContext.Default.Order);
            var payload = new Order.Payload { Csr = JwsConvert.ToBase64String(csr) };
            var resp = await Context.HttpClient.Post(Context, order.Finalize!, payload, true, AcmeJsonContext.Default.Payload, AcmeJsonContext.Default.Order);
            return resp.Resource!;
        }

        /// <summary>
        /// Downloads the certificate chain in PEM.
        /// <param name="preferredChain">The preferred Root Certificate</param>
        /// </summary>
        /// <returns>The certificate chain in PEM.</returns>
        public async Task<CertificateChain> Download(string? preferredChain = null)
        {
            var order = await Resource(AcmeJsonContext.Default.Order);
            var resp = await Context.HttpClient.Post(Context, order.Certificate!, string.Empty, false, AcmeJsonContext.Default.String, AcmeJsonContext.Default.String);

            var defaultChain = new CertificateChain(resp.Resource!);
            if (defaultChain.MatchesPreferredChain(preferredChain) || !resp.Links!.Contains("alternate"))
                return defaultChain;

            var alternateLinks = resp.Links["alternate"].ToList();
            foreach (var alternate in alternateLinks)
            {
                resp = await Context.HttpClient.Post(Context, alternate, string.Empty, false, AcmeJsonContext.Default.String, AcmeJsonContext.Default.String);
                var chain = new CertificateChain(resp.Resource!);

                if (chain.MatchesPreferredChain(preferredChain))
                    return chain;
            }

            return defaultChain;
        }

    }
}
