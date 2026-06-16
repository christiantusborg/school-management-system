using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using SharedLibrary.Basics.Opaque.Domains;

namespace SharedLibrary.Basics.Opaque.Api.Infrastructure;

/// <summary>
/// Sends mail through the Gmail API as a Workspace user, using a service
/// account with domain-wide delegation (impersonation). Builds the MIME with
/// the shared <see cref="EmailMimeBuilder"/>, then base64url-encodes it and
/// POSTs to the Gmail REST send endpoint with a delegated access token.
/// </summary>
internal static class GmailEmailDispatcher
{
    private const string Scope = "https://www.googleapis.com/auth/gmail.send";
    private static readonly HttpClient Http = new();

    public static async Task SendAsync(
        string serviceAccountJson,
        string impersonatedUser,
        EmailMessage msg,
        string defaultFromEmail,
        string defaultFromName,
        CancellationToken ct)
    {
        var mime = EmailMimeBuilder.Build(msg, defaultFromEmail, defaultFromName);
        using var ms = new MemoryStream();
        await mime.WriteToAsync(ms, ct);
        var raw = Convert.ToBase64String(ms.ToArray())
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var credential = GoogleCredential.FromJson(serviceAccountJson)
            .CreateScoped(Scope)
            .CreateWithUser(impersonatedUser);
        var token = await ((ITokenAccess)credential).GetAccessTokenForRequestAsync(cancellationToken: ct);

        var url = $"https://gmail.googleapis.com/gmail/v1/users/{Uri.EscapeDataString(impersonatedUser)}/messages/send";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(new { raw }), Encoding.UTF8, "application/json");

        using var resp = await Http.SendAsync(request, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Gmail API send failed ({(int)resp.StatusCode}): {err}");
        }
    }
}
