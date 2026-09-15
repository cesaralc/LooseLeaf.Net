# LooseLeaf.Net (MVP)

LooseLeaf.Net is an open-source .NET library that turns messy, real-world
documents — PDFs, scans, photos of forms — into strongly-typed C# objects,
with field-level provenance back to the exact page and location the data came
from.

```csharp
var result = await extractor
    .From("invoice.pdf")
    .ExtractAsync<Invoice>();

result.Data.Total;              // 12847.32m
result.GetSource(x => x.Total); // page 3, bounds, OCR + extraction confidence
```

The developer doesn't need to know (or care) whether the PDF had embedded
text, was scanned, or came from a phone camera — LooseLeaf.Net figures that
out and normalizes everything into one pipeline before handing it to an LLM
for extraction.

## Why LooseLeaf.Net was created

Most "extract structured data from a document" code in .NET today looks like:
call an OCR/vision API by hand, string-concat the raw text, send it to an
LLM, `JsonSerializer.Deserialize` the response, and hope the shape matches.
That approach has a few recurring problems:

- **It re-runs OCR on documents that don't need it.** A digitally generated
  PDF (an invoice exported from QuickBooks, a contract from DocuSign) already
  contains real text. Sending it through an OCR/vision API anyway adds cost,
  latency, and a new source of transcription errors.
- **It throws away layout.** Plain extracted text loses page numbers,
  coordinates, tables, and checkboxes — so a downstream UI can never say
  *"here is exactly where the AI found this number on the page,"* whether
  that page was native text, a scan, or a document produced with tables and
  selection marks by Azure Document Intelligence.
- **It's tied to one vendor.** Code written directly against Azure Document
  Intelligence, AWS Textract, or Google Document AI can't easily switch (or
  fall back) to another provider, or to a free local engine like Tesseract.
- **It has no provenance.** Once you have `invoice.Total = 12847.32m`, there's
  no way to answer "where in the source document did that number come from?"
  without re-parsing everything by hand.

LooseLeaf.Net exists to solve those problems once, as a reusable library,
instead of every team re-solving them inside application code.

## What LooseLeaf.Net simplifies

- **OCR becomes optional and automatic.** `OcrMode.Auto` (the default) uses
  native PDF text when it's usable and only invokes OCR when it isn't. You
  can force `Always` (e.g. to normalize handwriting/scans consistently) or
  `Never` (e.g. to fail fast if a document isn't machine-readable text).
- **Every OCR provider looks the same to your code.** Azure Document
  Intelligence, and (soon) AWS Textract, Google Document AI, Tesseract, and
  OpenAI Vision all get normalized into the same `ProcessedDocument` /
  `DocumentPage` / `DocumentElement` model, so the rest of the pipeline —
  and your application code — never needs a provider-specific branch.
- **You get a typed model, not raw JSON.** `ExtractAsync<Invoice>()` returns
  an `Invoice`, with `DataAnnotations`-based validation issues surfaced
  alongside the data instead of a runtime `KeyNotFoundException`.
- **You get provenance for free.** `result.GetSource(x => x.Total)` returns
  the page, source text, normalized bounding box, OCR confidence, and
  extraction confidence for that field — enough for a UI to draw a highlight
  box directly on the source PDF/image.
- **Switching or combining OCR vendors is a config change**, not a rewrite —
  `services.AddLooseLeaf(options => options.AddAzureDocumentIntelligence(...))`
  is the only place a specific vendor is named.

## Use cases

- **Invoice / bill processing** — extract vendor, line items, totals, and due
  dates from vendor-supplied PDFs of wildly different layouts, with a
  citation for every dollar amount so a human reviewer can verify it at a
  glance.
- **Insurance claims and intake forms** — many claim forms are scanned faxes
  full of checkboxes and signatures. OCR providers like Azure Document
  Intelligence report `Checkbox`/`Signature` elements; LooseLeaf.Net surfaces
  them through the same `DocumentElement.Type` your code already reads for
  plain text.
- **Contracts and legal documents** — pull structured fields (parties,
  effective dates, renewal terms) out of long native-text PDFs using the fast
  native-text path (no OCR, no per-page vendor cost) with `OcrMode.Auto`.
- **Receipts and expense reports** — photos from a phone camera have no
  embedded text at all; `OcrMode.Auto` automatically routes these through
  OCR while still using the native-text path for anything emailed as a PDF.
- **Identity / KYC documents** — extract structured fields from IDs or
  utility bills where every field needs an audit trail (`GetSource`) showing
  exactly where on the document a value was read from, for compliance review.
- **Table-heavy reports** — `ElementType.Table` / `TableCell` let you rebuild
  tabular data (e.g. line items, rate schedules) instead of only getting a
  flattened wall of text.

In every case, the calling code is the same shape:

```csharp
var result = await extractor.From(path).ExtractAsync<TModel>();
```

Only the target model (`Invoice`, `ClaimForm`, `Contract`, `Receipt`, ...)
and the OCR provider configuration change.

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
