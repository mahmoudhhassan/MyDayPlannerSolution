// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http.Headers;
using Microsoft.Identity.Client;

/// <summary>
/// Retrieves a token via the provided delegate and applies it to HTTP requests using the
/// "bearer" authentication scheme.
/// </summary>
public class BearerAuthenticationProviderWithCancellationToken
{
    private readonly IConfidentialClientApplication _client;
    private readonly string _bearerToken;

    /// <summary>
    /// Creates an instance of the <see cref="BearerAuthenticationProviderWithCancellationToken"/> class.
    /// </summary>
    public BearerAuthenticationProviderWithCancellationToken(string bearerToken)
    {
        var clientId = Environment.GetEnvironmentVariable("MsGraph__ClientId");
        var tenantId = Environment.GetEnvironmentVariable("MsGraph__TenantId");
        var clientSecret = Environment.GetEnvironmentVariable("MsGraph__ClientSecret");
        _bearerToken = bearerToken;

        // Create the MSAL confidential client application for On-Behalf-Of flow
        _client = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithTenantId(tenantId)
            .WithClientSecret(clientSecret)
            .Build();
    }

    /// <summary>
    /// Applies the token to the provided HTTP request message.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="cancellationToken"></param>
    public async Task AuthenticateRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var token = await this.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var scopes = new string[] { "https://graph.microsoft.com/.default" };
        try
        {
            // Prepare the user assertion based on the received Access Token
            var assertion = new UserAssertion(_bearerToken);

            // Try to get the token from the tokens cache
            var tokenResult = await _client
            .AcquireTokenOnBehalfOf(scopes, assertion)
            .ExecuteAsync().ConfigureAwait(false);
            
             // Provide back the OBO Access Token
            return tokenResult.AccessToken;    
        }
        catch (Exception ex)
        {
            return "";
        }
    }
}