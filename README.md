# Addressable Packer

Editor tooling for Unity Addressables. Two things:

1. **Directory-based auto grouping** — define an Addressable group's contents by an on-disk folder instead of maintaining entries by hand.
2. **Duplicate dependency resolution** — custom Analyze rules that de-duplicate assets pulled into multiple AssetBundles, producing fewer bundles than the built-in rule.

Editor-only. Targets Unity 2021.3+ and `com.unity.addressables` 1.19.19.

## Installation

Add via UPM using the Git URL (Window → Package Manager → **+** → *Add package from git URL*):

```
https://github.com/dakgg/addressable-packer.git
```

Or add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.dakgg.addressable-packer": "https://github.com/dakgg/addressable-packer.git"
  }
}
```

## Usage

### Directory-based groups

1. Create an Addressable group (Window → Asset Management → Addressables → Groups).
2. Add the **Auto Asset Directory** schema to the group (Add Schema → *Auto Asset Directory*).
3. Set:
   - **Directory Path** — the folder the group owns (e.g. `Assets/Art/Characters`).
   - **Recursive** — include subfolders (default on).
   - **Extensions** — optional comma-separated filter, e.g. `png, prefab` (empty = all).
4. Run **`Latecia → Rebuild Addressables Directory Groups`** (or *CONTEXT → AddressableAssetSettings → Rebuild Directory Groups*).

Entries are created/moved to match the folder. Each entry's address is its path relative to the owning directory (without extension) suffixed with `@{GroupName}`, plus a `${GroupName}` label. Nested directory-groups take precedence over ancestor directory-groups. Rebuild only touches groups that carry the schema — manually-managed groups are left alone.

### Resolve duplicate dependencies

- Run **`Latecia → Resolve Addressables Duplicate Assets`** to run the *Check Duplicate Bundle Dependencies V2* rule and auto-fix: duplicated assets are grouped by their set of bundle parents and packed by label into a `Duplicate Assets Sorted By Label` group (fewer bundles than the stock rule).
- Both custom rules are also available in the Addressables **Analyze** window:
  - *Check Duplicate Bundle Dependencies V2*
  - *Check Duplicate Bundle Dependencies Multi-Isolated Groups*

## Notes

- The `Latecia` menu prefix is project-specific; rename it in `Editor/DirectoryBaseGroupSchemaAssetPostprocessor.cs` if forking.
- The Analyze rules subclass Unity's internal analyze infrastructure and are sensitive to the pinned Addressables version. Re-validate if you bump `com.unity.addressables`.

## License

MIT — see [LICENSE](LICENSE).
