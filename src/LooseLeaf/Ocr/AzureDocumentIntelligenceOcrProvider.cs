using System.Net.Http.Headers;
using System.Text.Json;
using LooseLeaf.Abstractions;
using LooseLeaf.Models;

namespace LooseLeaf.Ocr;

public sealed class AzureDocumentIntelligenceOcrProvider : IOcrProvider
{
    private readonly string _analyzeEndpoint;
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _timeout;
    private readonly TimeSpan _pollingInterval;

    public AzureDocumentIntelligenceOcrProvider(
        string endpoint,
        string apiKey,
        HttpClient? httpClient = null,
        TimeSpan? timeout = null,
        TimeSpan? pollingInterval = null)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Azure endpoint is required.", nameof(endpoint));
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("Azure API key is required.", nameof(apiKey));
        }

        var trimmed = endpoint.TrimEnd('/');
        _analyzeEndpoint =
            $"{trimmed}/documentintelligence/documentModels/prebuilt-layout:analyze?api-version=2024-11-30";
        _apiKey = apiKey;
        _httpClient = httpClient ?? new HttpClient();
        _timeout = timeout ?? TimeSpan.FromMinutes(2);
        _pollingInterval = pollingInterval ?? TimeSpan.FromSeconds(1);
    }

    public async Task<OcrDocument> ProcessAsync(
        Stream document,
        CancellationToken cancellationToken = default)
    {
        if (document.CanSeek)
        {
            document.Position = 0;
        }

        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            await document.CopyToAsync(ms, cancellationToken);
            bytes = ms.ToArray();
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _analyzeEndpoint);
        request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
        request.Content = new ByteArrayContent(bytes);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var analyzeResponse = await _httpClient.SendAsync(request, cancellationToken);
        if (!analyzeResponse.IsSuccessStatusCode)
        {
            var body = await analyzeResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Azure analyze request failed ({(int)analyzeResponse.StatusCode}): {body}");
        }

        if (!analyzeResponse.Headers.TryGetValues("Operation-Location", out var values))
        {
            throw new InvalidOperationException("Azure response did not include Operation-Location.");
        }

        var operationLocation = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(operationLocation))
        {
            throw new InvalidOperationException("Azure response contained an empty Operation-Location.");
        }

        return await PollForResultAsync(operationLocation, cancellationToken);
    }

    private async Task<OcrDocument> PollForResultAsync(
        string operationLocation,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_timeout);
        var token = timeoutCts.Token;

        while (true)
        {
            token.ThrowIfCancellationRequested();
            using var request = new HttpRequestMessage(HttpMethod.Get, operationLocation);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
            using var response = await _httpClient.SendAsync(request, token);
            var body = await response.Content.ReadAsStringAsync(token);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Azure polling failed ({(int)response.StatusCode}): {body}");
            }

            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            var status = root.TryGetProperty("status", out var statusElement)
                ? statusElement.GetString()
                : null;

            if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase))
            {
                return ParseSucceededResult(root);
            }

            if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Azure OCR failed: {body}");
            }

            await Task.Delay(_pollingInterval, token);
        }
    }

    private static OcrDocument ParseSucceededResult(JsonElement root)
    {
        if (!root.TryGetProperty("analyzeResult", out var analyzeResult))
        {
            throw new InvalidOperationException("Azure result did not include analyzeResult.");
        }

        if (!analyzeResult.TryGetProperty("pages", out var pagesElement))
        {
            return new OcrDocument();
        }

        var pages = new List<OcrPage>();
        foreach (var page in pagesElement.EnumerateArray())
        {
            var pageNumber = page.TryGetProperty("pageNumber", out var numberElement)
                ? numberElement.GetInt32()
                : 0;
            var pageWidth = page.TryGetProperty("width", out var widthElement)
                ? (float)widthElement.GetDouble()
                : 1f;
            var pageHeight = page.TryGetProperty("height", out var heightElement)
                ? (float)heightElement.GetDouble()
                : 1f;

            var elements = new List<OcrElement>();

            if (page.TryGetProperty("lines", out var linesElement))
            {
                foreach (var line in linesElement.EnumerateArray())
                {
                    var text = line.TryGetProperty("content", out var contentElement)
                        ? contentElement.GetString() ?? string.Empty
                        : string.Empty;
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    var bounds = ParsePolygonBounds(line, pageWidth, pageHeight);
                    elements.Add(new OcrElement
                    {
                        Text = text,
                        Bounds = bounds,
                        Confidence = 1f,
                        Type = ElementType.Text
                    });
                }
            }

            if (page.TryGetProperty("selectionMarks", out var marksElement))
            {
                foreach (var mark in marksElement.EnumerateArray())
                {
                    var state = mark.TryGetProperty("state", out var stateElement)
                        ? stateElement.GetString() ?? "unknown"
                        : "unknown";
                    var confidence = mark.TryGetProperty("confidence", out var confidenceElement)
                        ? (float)confidenceElement.GetDouble()
                        : 0f;
                    var bounds = ParsePolygonBounds(mark, pageWidth, pageHeight);
                    elements.Add(new OcrElement
                    {
                        Text = state,
                        Bounds = bounds,
                        Confidence = confidence,
                        Type = ElementType.Checkbox
                    });
                }
            }

            pages.Add(new OcrPage
            {
                PageNumber = pageNumber,
                Elements = elements
            });
        }

        if (analyzeResult.TryGetProperty("tables", out var tablesElement))
        {
            foreach (var table in tablesElement.EnumerateArray())
            {
                if (!table.TryGetProperty("boundingRegions", out var regionsElement))
                {
                    continue;
                }

                var firstRegion = regionsElement.EnumerateArray().FirstOrDefault();
                if (!firstRegion.ValueKind.Equals(JsonValueKind.Object))
                {
                    continue;
                }

                var pageNumber = firstRegion.TryGetProperty("pageNumber", out var pageNumberElement)
                    ? pageNumberElement.GetInt32()
                    : 0;
                var page = pages.FirstOrDefault(p => p.PageNumber == pageNumber);
                if (page is null)
                {
                    continue;
                }

                var pageEntry = pages.First(p => p.PageNumber == pageNumber);
                var pageElements = pageEntry.Elements.ToList();

                if (table.TryGetProperty("cells", out var cellsElement))
                {
                    foreach (var cell in cellsElement.EnumerateArray())
                    {
                        var text = cell.TryGetProperty("content", out var contentElement)
                            ? contentElement.GetString() ?? string.Empty
                            : string.Empty;
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            continue;
                        }

                        var region = cell.TryGetProperty("boundingRegions", out var cellRegions)
                            ? cellRegions.EnumerateArray().FirstOrDefault()
                            : default;
                        if (!region.ValueKind.Equals(JsonValueKind.Object))
                        {
                            continue;
                        }

                        var polygonBounds = ParsePolygonBounds(region, 1f, 1f, "polygon");
                        pageElements.Add(new OcrElement
                        {
                            Text = text,
                            Bounds = polygonBounds,
                            Confidence = 1f,
                            Type = ElementType.TableCell
                        });
                    }

                    pages[pages.FindIndex(p => p.PageNumber == pageNumber)] = new OcrPage
                    {
                        PageNumber = pageEntry.PageNumber,
                        Elements = pageElements
                    };
                }
            }
        }

        return new OcrDocument
        {
            Pages = pages
        };
    }

    private static BoundingBox ParsePolygonBounds(
        JsonElement element,
        float pageWidth,
        float pageHeight,
        string polygonPropertyName = "polygon")
    {
        if (!element.TryGetProperty(polygonPropertyName, out var polygonElement))
        {
            return default;
        }

        var coordinates = polygonElement.EnumerateArray().Select(x => (float)x.GetDouble()).ToArray();
        if (coordinates.Length < 8)
        {
            return default;
        }

        var xValues = new List<float>();
        var yValues = new List<float>();
        for (var i = 0; i < coordinates.Length - 1; i += 2)
        {
            xValues.Add(coordinates[i]);
            yValues.Add(coordinates[i + 1]);
        }

        var left = xValues.Min();
        var right = xValues.Max();
        var top = yValues.Min();
        var bottom = yValues.Max();

        if (pageWidth <= 0f)
        {
            pageWidth = 1f;
        }

        if (pageHeight <= 0f)
        {
            pageHeight = 1f;
        }

        return new BoundingBox(
            X: left / pageWidth,
            Y: top / pageHeight,
            Width: (right - left) / pageWidth,
            Height: (bottom - top) / pageHeight);
    }
}
