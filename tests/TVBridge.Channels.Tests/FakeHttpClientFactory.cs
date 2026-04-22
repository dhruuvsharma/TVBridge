using System.Net;

namespace TVBridge.Channels.Tests;

/// <summary>
/// Fake IHttpClientFactory for testing channels without real HTTP calls.
/// </summary>
internal sealed class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly FakeHttpMessageHandler _handler;

    public FakeHttpClientFactory()
        : this(new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)))
    {
    }

    public FakeHttpClientFactory(HttpResponseMessage response)
        : this(new FakeHttpMessageHandler(response))
    {
    }

    public FakeHttpClientFactory(FakeHttpMessageHandler handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name) => new(_handler) { BaseAddress = null };
}

/// <summary>
/// Fake HttpMessageHandler that returns a canned response and captures the request body.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public string? LastRequestBody { get; private set; }
    public string? LastRequestUri { get; private set; }
    public HttpMethod? LastMethod { get; private set; }

    public FakeHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastMethod = request.Method;
        LastRequestUri = request.RequestUri?.ToString();

        if (request.Content is not null)
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

        return _response;
    }
}
