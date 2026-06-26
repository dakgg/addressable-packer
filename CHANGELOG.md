# Changelog

All notable changes to this package are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-06-26

### Added
- `DirectoryBaseGroupSchema` ("Auto Asset Directory") — define an Addressable group's contents by an on-disk folder, with recursive and extension-filter options.
- Menu items to rebuild directory-based groups and resolve duplicate assets (`Latecia/...` and `CONTEXT/AddressableAssetSettings/...`).
- `CheckBundleDupeDependenciesV2` Analyze rule — packs duplicated assets by their set of bundle parents into a single labeled group.
- `CheckBundleDupeDependenciesMultiIsolatedGroups` Analyze rule — isolates duplicates into per-referencing-group bundles.
