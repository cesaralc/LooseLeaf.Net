# LooseLeaf.Net (MVP)

LooseLeaf.Net is an open-source .NET library for strongly-typed extraction from documents.

Pipeline:

```text
Document
   ↓
Native text check (PDF)
   ├── usable → direct text path
   └── not usable → OCR provider
               ↓
       text + coordinates/layout
               ↓
        intermediate document model
               ↓
              LLM
               ↓
      ExtractAsync<T>()
               ↓
   validation + citations
```

## Current MVP

- `ExtractAsync<T>()` API
- OCR mode:
  - `Auto` (default)
  - `Always`
  - `Never`
- Pluggable OCR provider contract (`IOcrProvider`)
- Intermediate model:
  - `ProcessedDocument`
  - `DocumentPage`
  - `DocumentElement`
  - `BoundingBox`
  - `ElementType`
- Field-level source provenance:
  - `result.GetSource(x => x.Total)`
  - page, bounds, OCR confidence, extraction confidence
- First OCR provider:
  - Azure AI Document Intelligence (REST implementation)

Planned providers:

- Tesseract
- AWS Textract
- Google Document AI
- OpenAI Vision

## Installation

```bash
dotnet add package LooseLeaf.Net
```

## Basic usage

```csharp
using LooseLeaf;
using LooseLeaf.Abstractions;
using LooseLeaf.Models;

services.AddLooseLeaf(options =>
{
    options.OcrMode = OcrMode.Auto;
    options.AddAzureDocumentIntelligence(endpoint, apiKey);
    options.AddChatClient(chatClient); // IChatCompletionClient
});

var result = await extractor
    .From("invoice.pdf")
    .ExtractAsync<Invoice>();

var totalSource = result.GetSource(x => x.Total);
```

## Testing

See [docs/UNIT_TESTS.md](docs/UNIT_TESTS.md) for unit test coverage and instructions on running the test suite.

## Open source development plan

1. Finalize provider-independent intermediate model for text/layout/tables.
2. Add first-party integration package for `Microsoft.Extensions.AI` chat clients.
3. Implement Tesseract local provider.
4. Implement AWS Textract and Google Document AI adapters.
5. Add OCR confidence-based routing/fallback.
6. Add page-level routing and retries.
7. Add benchmarking, resilience tests, and sample UI highlighting.
