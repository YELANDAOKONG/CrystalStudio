using System.Net;
using System.Text;

using CrystalStudio.Configuration;
using CrystalStudio.Council;
using CrystalStudio.Interfaces;

namespace CrystalStudio.Compatible;

/// <summary>
/// Local OpenAI-compatible HTTP front for every loaded council.
/// </summary>
public sealed class CompatibleListener : IDisposable
{
    private readonly HttpListener _listener;
    private readonly CouncilCatalog _catalog;
    private readonly Dictionary<string, CouncilSession> _sessions;

    public CompatibleListener(CouncilCatalog catalog, IMemberClientFactory clients)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(clients);

        _catalog = catalog;
        _sessions = new Dictionary<string, CouncilSession>(StringComparer.OrdinalIgnoreCase);
        foreach (var council in catalog.Councils)
        {
            _sessions[council.AdvertisedModel] = new CouncilSession(council, clients);
        }

        _listener = new HttpListener();
        _listener.Prefixes.Add(NormalizePrefix(catalog.ListenPrefix));
    }

    public Uri Prefix => new(_listener.Prefixes.First());

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _listener.Start();
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync().WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                _ = HandleSafeAsync(context, cancellationToken);
            }
        }
        finally
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
            }
        }
    }

    public void Dispose()
    {
        ((IDisposable)_listener).Dispose();
    }

    private async Task HandleSafeAsync(
        HttpListenerContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            await HandleAsync(context, cancellationToken);
        }
        catch (Exception)
        {
            TryClose(context);
        }
    }

    private async Task HandleAsync(
        HttpListenerContext context,
        CancellationToken cancellationToken)
    {
        var request = context.Request;
        var response = context.Response;
        response.Headers["Access-Control-Allow-Origin"] = "*";
        var path = request.Url?.AbsolutePath.TrimEnd('/') ?? string.Empty;

        if (request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 204;
            response.Close();
            return;
        }

        if (request.HttpMethod == "GET" && path is "/health")
        {
            response.StatusCode = 200;
            response.ContentType = "application/json";
            await WriteAsciiAsync(response, "{\"status\":\"ok\"}", cancellationToken);
            return;
        }

        if (request.HttpMethod == "GET" && path is "/v1/models" or "/models")
        {
            response.StatusCode = 200;
            response.ContentType = "application/json";
            var writer = new CompatibleWriter(
                response.OutputStream,
                "models",
                _catalog.Default.AdvertisedModel,
                streamResponse: false);
            await writer.WriteModelsAsync(_catalog.AdvertisedModels, cancellationToken);
            response.Close();
            return;
        }

        if (request.HttpMethod == "POST" && path is "/v1/chat/completions" or "/chat/completions")
        {
            await HandleCompletionsAsync(context, cancellationToken);
            return;
        }

        response.StatusCode = 404;
        response.ContentType = "application/json";
        var missing = new CompatibleWriter(
            response.OutputStream,
            "error",
            _catalog.Default.AdvertisedModel,
            streamResponse: false);
        await missing.WriteErrorAsync("Not found.", cancellationToken);
        response.Close();
    }

    private async Task HandleCompletionsAsync(
        HttpListenerContext context,
        CancellationToken cancellationToken)
    {
        var response = context.Response;
        string body;
        using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
        {
            body = await reader.ReadToEndAsync(cancellationToken);
        }

        if (!RequestTranslator.TryRead(body, out var chat, out var stream, out var model, out var error))
        {
            await WriteClientErrorAsync(response, error, cancellationToken);
            return;
        }

        if (!_catalog.TryGet(model, out var council))
        {
            await WriteClientErrorAsync(
                response,
                $"Unknown model '{model.Trim()}'. Available models: {FormatModels()}.",
                cancellationToken);
            return;
        }

        var advertised = council.AdvertisedModel;
        var session = _sessions[advertised];
        var completionId = "chatcmpl-" + Guid.NewGuid().ToString("N")[..24];
        response.StatusCode = 200;
        response.SendChunked = stream;
        response.ContentType = stream ? "text/event-stream" : "application/json";
        response.Headers["Cache-Control"] = "no-cache";

        using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var writer = new CompatibleWriter(response.OutputStream, completionId, advertised, stream);
        var observer = new ProgressLog(async (delta, token) =>
        {
            try
            {
                await writer.WriteThinkingAsync(delta, token);
            }
            catch (Exception)
            {
                requestCts.Cancel();
            }
        });

        try
        {
            var action = await session.RunAsync(chat, observer, requestCts.Token);
            if (!stream)
            {
                action = new CouncilAction(
                    action.Outcome,
                    action.Text,
                    action.ToolCalls,
                    observer.Text,
                    action.Usage);
            }

            await writer.WriteActionAsync(action, requestCts.Token);
        }
        catch (OperationCanceledException)
        {
            if (!stream && response.OutputStream.CanWrite)
            {
                await writer.WriteErrorAsync("The council request was cancelled.", cancellationToken);
            }
        }
        finally
        {
            response.Close();
        }
    }

    private async Task WriteClientErrorAsync(
        HttpListenerResponse response,
        string message,
        CancellationToken cancellationToken)
    {
        response.StatusCode = 400;
        response.ContentType = "application/json";
        var errors = new CompatibleWriter(
            response.OutputStream,
            "error",
            _catalog.Default.AdvertisedModel,
            streamResponse: false);
        await errors.WriteErrorAsync(message, cancellationToken);
        response.Close();
    }

    private string FormatModels() => string.Join(", ", _catalog.AdvertisedModels);

    private static string NormalizePrefix(Uri listen)
    {
        var text = listen.AbsoluteUri;
        return text.EndsWith('/') ? text : text + "/";
    }

    private static async Task WriteAsciiAsync(
        HttpListenerResponse response,
        string text,
        CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, cancellationToken);
        response.Close();
    }

    private static void TryClose(HttpListenerContext context)
    {
        try
        {
            context.Response.StatusCode = 500;
            context.Response.Close();
        }
        catch (Exception)
        {
            // The client already disconnected.
        }
    }
}
