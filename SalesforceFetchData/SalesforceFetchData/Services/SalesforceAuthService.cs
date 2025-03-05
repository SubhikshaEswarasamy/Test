using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

public class SalesforceAuthService
{
    private readonly IConfiguration _config;
    public SalesforceAuthService(IConfiguration config) => _config = config;

    public async Task<string> GetAccessToken()
    {
        using (var client = new HttpClient())
        {
            var credentials = _config.GetSection("Salesforce").Get<SalesforceCredentials>();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", credentials.ClientId),
                new KeyValuePair<string, string>("client_secret", credentials.ClientSecret),
                new KeyValuePair<string, string>("username", credentials.Username),
                new KeyValuePair<string, string>("password", credentials.Password + credentials.SecurityToken)
            });

            var response = await client.PostAsync(credentials.AuthUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();
            var authResponse = JsonConvert.DeserializeObject<SalesforceAuthResponse>(responseString);
            return authResponse.AccessToken;
        }
    }
}

public class SalesforceCredentials
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string SecurityToken { get; set; }
    public string AuthUrl { get; set; }
    public string InstanceUrl { get; set; }
}

public class SalesforceAuthResponse
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }
}