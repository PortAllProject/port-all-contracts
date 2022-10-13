using AElf.Client.Services;

namespace AElf.Client;

public partial class AElfClient : IDisposable
{
    private readonly IHttpService _httpService;
    // aelf node endpoint.
    private readonly string _baseUrl;
    private string? _userName;
    private string? _password;

    public AElfClient(string baseUrl, int timeOut = 60, string? userName = null, string? password = null,
        bool useCamelCase = false)
    {
        _httpService = new HttpService(timeOut, useCamelCase);
        _baseUrl = baseUrl;
        _userName = userName;
        _password = password;
    }

    private bool _disposed;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        Dispose(true);
        _disposed = true;
    }

    /// <summary>
    /// Disposes the resources associated with the object.
    /// </summary>
    /// <param name="disposing"><c>true</c> if called by a call to the <c>Dispose</c> method; otherwise false.</param>
    protected virtual void Dispose(bool disposing)
    {
    }
}