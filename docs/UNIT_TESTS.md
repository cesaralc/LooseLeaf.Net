# Unit tests

LooseLeaf.Net is tested with xUnit. The test project lives at
[`tests/LooseLeaf.Tests`](../tests/LooseLeaf.Tests) and references the library
project directly (no mocking framework is required — collaborators are
implemented with lightweight hand-written stubs/fakes).

## Running the tests

From the repository root:

```bash
dotnet test LooseLeaf.slnx
```

Or run the test project directly:

```bash
dotnet test tests/LooseLeaf.Tests/LooseLeaf.Tests.csproj
```

To build the whole solution without running tests:

```bash
dotnet build LooseLeaf.slnx
```

## Current coverage

- **`PipelineTests`** — exercises `LooseLeafExtractor` end-to-end using stub
  implementations of `INativeTextExtractor`, `IOcrProvider`, and
  `IStructuredDataExtractor`:
  - `OcrMode.Auto` uses native PDF text when it is usable and never calls OCR.
  - `OcrMode.Auto` falls back to OCR when native text is too short/unusable.
  - `OcrMode.Never` throws when native text isn't usable.
  - `OcrMode.Always` always invokes the configured OCR provider.
  - Extraction throws a clear error when OCR is required but no provider is
    configured.
  - `ExtractAsync` validates its `path` argument.
  - `IDocumentExtractionSession` (`From(path).ExtractAsync<T>()`) delegates to
    the extractor correctly.
  - `ExtractionResult<T>.GetSource(...)` returns field-level provenance
    (`page`, `text`, `bounds`, `OcrConfidence`, `ExtractionConfidence`) when a
    field has a citation, and `null` when it does not.

- **`OptionsTests`** — covers `LooseLeafOptions` and the
  `IServiceCollection.AddLooseLeaf(...)` DI extension:
  - `OcrMode` defaults to `Auto`.
  - `AddOcrProvider` / `AddChatClient` reject `null` arguments.
  - `AddAzureDocumentIntelligence` validates the endpoint/API key and wires up
    the `AzureDocumentIntelligenceOcrProvider` factory.
  - `AddLooseLeaf` throws when no structured-data extractor/chat client is
    configured.
  - `AddLooseLeaf` successfully registers `ILooseLeafExtractor` in the
    `IServiceCollection` when a chat client is configured.

- **`ModelTests`** — covers the intermediate document/result model:
  - `ExtractionResult<T>.GetSource(...)` resolves nested property paths (e.g.
    `x => x.Vendor.Name`).
  - `Citations` returns every registered `FieldSource`.
  - `ValidateData` surfaces `ValidationIssue`s for invalid models (via
    `System.ComponentModel.DataAnnotations`) and returns no issues for valid
    ones.
  - `BoundingBox` stores normalized coordinates as provided.

- **`OcrProviderTests`** — covers the OCR provider adapters:
  - The not-yet-implemented providers (`TesseractOcrProvider`,
    `AwsTextractOcrProvider`, `GoogleDocumentAiOcrProvider`,
    `OpenAiVisionOcrProvider`) throw `NotSupportedException` until they are
    implemented.
  - `AzureDocumentIntelligenceOcrProvider` validates that the endpoint and API
    key are supplied.

## Expectations for new tests

As new OCR providers (Tesseract, AWS Textract, Google Document AI, OpenAI
Vision) and routing/fallback behavior are implemented, add tests that:

- Use fakes/stubs rather than live network calls or local binaries — no test
  should require network access, cloud credentials, or a locally installed
  OCR engine to pass.
- Assert on the intermediate `ProcessedDocument` / `DocumentPage` /
  `DocumentElement` shape returned by a provider, not on internal
  implementation details.
- Cover both the "happy path" (valid document → expected elements/bounds/
  confidence) and failure/edge cases (missing configuration, empty documents,
  cancellation).
- Keep tests fast and deterministic; use `CancellationToken` and small
  in-memory streams instead of real files where possible.
