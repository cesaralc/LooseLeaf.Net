# Unit Test Documentation

This project uses xUnit for unit tests in `tests/DocExtract.Tests`.

## Goals

Unit tests should validate pipeline behavior without requiring live OCR or LLM services:

- OCR routing behavior by mode (`Auto`, `Always`, `Never`)
- Native text vs OCR fallback behavior
- Field-level provenance mapping (`GetSource(...)`)
- Validation behavior for extracted models

## Current test coverage

Current tests focus on core orchestration:

- `AutoMode_UsesNativeText_WhenUsable`
- `AutoMode_FallsBackToOcr_WhenNativeTextNotUsable`
- `GetSource_ReturnsFieldProvenance`

These tests use in-memory stubs for:

- `INativeTextExtractor`
- `IOcrProvider`
- `IStructuredDataExtractor`

## Running tests

From repository root:

```bash
dotnet test DocExtract.slnx
```

Or run the test project directly:

```bash
dotnet test tests/DocExtract.Tests/DocExtract.Tests.csproj
```

## Adding tests

When adding features:

1. Add or adjust unit tests first for new behavior or edge cases.
2. Keep tests deterministic and avoid network I/O in unit tests.
3. Use fakes/stubs for OCR and chat components.
4. Keep one behavioral assertion focus per test method.

## Suggested next tests

- `OcrMode.Never` throws when native text is unusable.
- `OcrMode.Always` bypasses native text.
- Validation issues are surfaced when data annotations fail.
- Azure OCR response parsing for table and selection mark mapping.
