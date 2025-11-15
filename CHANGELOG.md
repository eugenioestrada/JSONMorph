# Changelog

All notable changes to this project will be documented in this file.

## [1.1.0] - 2025-11-14
- Added text diff (`td`) operation for string properties to reduce patch size when generating patches.
- Updated documentation and README guidance to describe the new operation payload.
- Extended unit tests to cover text diff generation and application.

## [1.0.0] - 2025-11-14
- Initial open-source release.
- Added `JsonMorph.GeneratePatch` for producing compact JSON patch documents.
- Added `JsonMorph.ApplyPatch` for applying patches to JSON payloads.
- Comprehensive unit tests covering nested objects, arrays, formatting, and error handling.


