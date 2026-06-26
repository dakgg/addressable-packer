# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

`com.dakgg.addressable-packer` is a Unity **Editor-only** package (UPM) that extends Unity Addressables (pinned to `1.19.19`, Unity 2021.3+). It has two responsibilities:

1. **Directory-based auto grouping** — keep Addressable asset entries in sync with on-disk folders, so a group's contents are defined by a directory rather than maintained by hand.
2. **Duplicate dependency resolution** — custom Analyze rules that de-duplicate assets pulled into multiple AssetBundles, producing fewer bundles than the built-in rule.

All code lives under [Editor/](Editor/) and compiles into the `Dakgg.AddressablePacker.Editor` assembly ([Editor/Dakgg.AddressablePacker.Editor.asmdef](Editor/Dakgg.AddressablePacker.Editor.asmdef)), which is Editor-platform only and references `Unity.Addressables(.Editor)` and `Unity.ScriptableBuildPipeline(.Editor)`. There is no CLI build/test — the package is exercised inside the Unity Editor of a consuming project that adds it via UPM.

## Entry Points (Menu Items)

Defined in [Editor/DirectoryBaseGroupSchemaAssetPostprocessor.cs](Editor/DirectoryBaseGroupSchemaAssetPostprocessor.cs):

- `Latecia/Rebuild Addressables Directory Groups` (also `CONTEXT/AddressableAssetSettings → Rebuild Directory Groups`) → `DirectoryBaseSchemaPostAssetHandler.RebuildAll()`
- `Latecia/Resolve Addressables Duplicate Assets` (also `CONTEXT/AddressableAssetSettings → Resolve Duplicate Assets`) → `ResolveDuplicates()`

The Analyze rules also appear in the Addressables **Analyze** window (Window → Asset Management → Addressables → Analyze) via `[InitializeOnLoad]` registration with `AnalyzeSystem.RegisterNewRule<>`.

## Architecture

### Directory-based grouping

- `DirectoryBaseGroupSchema` ([Editor/DirectoryBaseGroupSchema.cs](Editor/DirectoryBaseGroupSchema.cs)) is an `AddressableAssetGroupSchema` you attach to an Addressable group. It stores `DirectoryPath`, `Recursive`, and a comma-separated `Extensions` filter. A group "owns" a directory by carrying this schema.
- `DirectoryBaseSchemaPostAssetHandler` builds a `DirectoryPath → (Group, Extensions)` map from every group that has a valid schema, then walks the filesystem to create/move Addressable entries.
- **Address scheme**: an asset's address is its path relative to the owning directory, minus extension, suffixed with `@{GroupName}` (see `TryGenerateAddressablePath`). Each entry also gets a single label `${GroupName}`.
- **Longest-prefix ownership**: `TryGenerateAddressablePath` strips trailing path segments and matches the *deepest* registered directory first, so nested directory-groups win over ancestor directory-groups.
- `RebuildAll()` clears only entries belonging to directory-schema groups (it never touches manually-managed groups), then re-imports from disk. `ProcessImport` deliberately **respects** entries already owned by a directory-schema group and refuses to steal entries owned by non-directory groups.

> NOTE: the class name `DirectoryBaseGroupSchemaAssetPostprocessor` implies an `AssetPostprocessor` hook, but the class currently only wires menu items — `ProcessDelete`/`ProcessMove` exist but aren't yet driven by an import callback. Re-sync happens on explicit menu invocation.

### Duplicate dependency resolution

Two custom Analyze rules, both registered at load:

- `CheckBundleDupeDependenciesV2` ([Editor/CheckBundleDupeDependenciesV2.cs](Editor/CheckBundleDupeDependenciesV2.cs)) — extends `BundleRuleBase`. Groups duplicated assets **by their set of bundle parents** and packs each set into one bundle (one label `BundleN` per parent-set, `PackTogetherByLabel`), into a group named `Duplicate Assets Sorted By Label`. Fewer bundles than the stock rule. This is the rule invoked programmatically by `ResolveDuplicates()`.
- `CheckBundleDupeDependenciesMultiIsolatedGroups` ([Editor/CheckBundleDupeDependenciesMultiIsolatedGroups.cs](Editor/CheckBundleDupeDependenciesMultiIsolatedGroups.cs)) — extends the built-in `CheckBundleDupeDependencies`. Creates an isolated `Duplicate Asset Isolation` group (StaticContent) per set of referencing groups. Available in the Analyze window only.

Both rules subclass Unity's internal analyze infrastructure, so they are sensitive to the pinned Addressables version. Bumping `com.unity.addressables` in [package.json](package.json) may break `ExtractData`/`AllBundleInputDefs`/`bundleToAssetGroup` usage and must be re-validated.

## C# Conventions

Follow the official Microsoft / .NET C# coding conventions for all code in this repo:

- [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [.NET runtime coding style](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md)

Key rules applied here:

- **Private/internal instance fields**: `_camelCase` (leading underscore). Use `s_camelCase` for private static fields. Do **not** use the Unity `m_` prefix. Unity's Inspector still nicifies `_directoryPath` → "Directory Path", so `[SerializeField]` fields keep readable labels.
- **Types, methods, properties, public members**: `PascalCase`. **Locals & parameters**: `camelCase`.
- **Allman braces**; one statement per line.
- A space follows control-flow keywords: `if (`, `for (`, `foreach (`, `while (`, `return` + value.
- `using` directives at the top of the file, `System.*` first.
- Prefer `var` when the type is apparent from the right-hand side.

When editing a `.cs` file, bring the whole file into compliance, not just the lines you touch.

## Conventions

- Files are guarded for Editor use via the asmdef platform restriction; `CheckBundleDupeDependenciesMultiIsolatedGroups.cs` additionally wraps everything in `#if UNITY_EDITOR`.
- `DirectoryBaseGroupSchema` lives in the `UnityEditor.AddressableAssets.Settings.GroupSchemas` namespace on purpose, to sit alongside Unity's built-in schemas.
- The "Latecia" menu prefix is the consuming project's name; rename it there if forking for another project.
- Every Unity asset/folder has a paired `.meta` file — never create or rename `.cs`/folders without keeping the `.meta` in sync.
