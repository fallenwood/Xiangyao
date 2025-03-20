namespace Certes.Acme;

using System;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Certes.Acme.Models;
using Certes.Jws;
using Certes.Properties;

/// <summary>
/// Supports HTTP operations for ACME servers.
/// </summary>
public interface IAcmeHttpClient
{
    /// <summary>
    /// Gets the nonce for next request.
    /// </summary>
    /// <returns>
    /// The nonce.
    /// </returns>
    Task<string> ConsumeNonce();

    /// <summary>
    /// Posts the data to the specified URI.
    /// </summary>
    /// <typeparam name="TRequest">The type of expected result</typeparam>
    /// <typeparam name="TResponse">The type of expected result</typeparam>
    /// <param name="uri">The URI.</param>
    /// <param name="payload">The payload.</param>
    /// <param name="requestJsonTypeInfo">The request JSON type information.</param>
    /// <param name="responseJsonTypeInfo">The response JSON type information.</param>
    /// <returns>The response from ACME server.</returns>
    Task<AcmeHttpResponse<TResponse>> Post<TRequest, TResponse>(
        Uri uri,
        TRequest payload,
        JsonTypeInfo<TRequest> requestJsonTypeInfo,
        JsonTypeInfo<TResponse> responseJsonTypeInfo);

    /// <summary>
    /// Gets the data from specified URI.
    /// </summary>
    /// <typeparam name="TResponse">The type of expected result</typeparam>
    /// <param name="uri">The URI.</param>
    /// <param name="responseJsonTypeInfo">The response JSON type information.</param>
    /// <returns>The response from ACME server.</returns>
    Task<AcmeHttpResponse<TResponse>> Get<TResponse>(Uri uri, JsonTypeInfo<TResponse> responseJsonTypeInfo);
}

/// <summary>
/// Extension methods for <see cref="IAcmeHttpClient"/>.
/// </summary>
internal static class IAcmeHttpClientExtensions
{
    /// <summary>
    /// Posts the data to the specified URI.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TResponse">The type of expected result</typeparam>
    /// <param name="client">The client.</param>
    /// <param name="context">The context.</param>
    /// <param name="location">The URI.</param>
    /// <param name="entity">The payload.</param>
    /// <param name="ensureSuccessStatusCode">if set to <c>true</c>, throw exception if the request failed.</param>
    /// <param name="entityJsonTypeInfo"></param>
    /// <param name="responseJsonTypeInfo"></param>
    /// <returns>
    /// The response from ACME server.
    /// </returns>
    /// <exception cref="Exception">
    /// If the HTTP request failed and <paramref name="ensureSuccessStatusCode"/> is <c>true</c>.
    /// </exception>
    internal static async Task<AcmeHttpResponse<TResponse>> Post<TEntity, TResponse>(this IAcmeHttpClient client,
        IAcmeContext context,
        Uri location,
        TEntity entity,
        bool ensureSuccessStatusCode,
        JsonTypeInfo<TEntity> entityJsonTypeInfo,
        JsonTypeInfo<TResponse> responseJsonTypeInfo)
    {
        var payload = await context.Sign(entity, location, entityJsonTypeInfo);
        var response = await client.Post(location, payload, AcmeJsonContext.Default.JwsPayload, responseJsonTypeInfo);
        var retryCount = context.BadNonceRetryCount;
        while (response.Error?.Status == System.Net.HttpStatusCode.BadRequest &&
            response.Error.Type?.CompareTo("urn:ietf:params:acme:error:badNonce") == 0 &&
            retryCount-- > 0)
        {
            payload = await context.Sign(entity, location, entityJsonTypeInfo);
            response = await client.Post(location, payload, AcmeJsonContext.Default.JwsPayload, responseJsonTypeInfo);
        }

        if (ensureSuccessStatusCode && response.Error != null)
        {
            throw new AcmeRequestException(
                string.Format(Strings.ErrorFetchResource, location),
                response.Error);
        }

        return response;
    }

    /// <summary>
    /// Posts the data to the specified URI.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse">The type of expected result</typeparam>
    /// <param name="client">The client.</param>
    /// <param name="jwsSigner">The jwsSigner used to sign the payload.</param>
    /// <param name="location">The URI.</param>
    /// <param name="entity">The payload.</param>
    /// <param name="ensureSuccessStatusCode">if set to <c>true</c>, throw exception if the request failed.</param>
    /// <param name="requestJsonTypeInfo">The request JSON type information.</param>
    /// <param name="responseJsonTypeInfo">The response JSON type information.</param>
    /// <param name="retryCount">Number of retries on badNonce errors (default = 1)</param>
    /// <returns>
    /// The response from ACME server.
    /// </returns>
    /// <exception cref="Exception">
    /// If the HTTP request failed and <paramref name="ensureSuccessStatusCode"/> is <c>true</c>.
    /// </exception>
    internal static async Task<AcmeHttpResponse<TResponse>> Post<TRequest, TResponse>(
        this IAcmeHttpClient client,
        JwsSigner jwsSigner,
        Uri location,
        TRequest entity,
        bool ensureSuccessStatusCode,
        JsonTypeInfo<TRequest> requestJsonTypeInfo,
        JsonTypeInfo<TResponse> responseJsonTypeInfo,
        int retryCount = 1)
    {
        var payload = jwsSigner.Sign(entity, requestJsonTypeInfo, url: location,  nonce: await client.ConsumeNonce());
        var response = await client.Post(location, payload, AcmeJsonContext.Default.JwsPayload, responseJsonTypeInfo);

        while (response.Error?.Status == System.Net.HttpStatusCode.BadRequest &&
            response.Error.Type?.CompareTo("urn:ietf:params:acme:error:badNonce") == 0 &&
            retryCount-- > 0)
        {
            payload = jwsSigner.Sign(entity, requestJsonTypeInfo, url: location, nonce: await client.ConsumeNonce());
            response = await client.Post(location, payload, AcmeJsonContext.Default.JwsPayload, responseJsonTypeInfo);
        }

        if (ensureSuccessStatusCode && response.Error != null)
        {
            throw new AcmeRequestException(
                string.Format(Strings.ErrorFetchResource, location),
                response.Error);
        }

        return response;
    }
}
