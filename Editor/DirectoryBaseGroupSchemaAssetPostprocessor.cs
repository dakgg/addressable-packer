using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Settings.GroupSchemas
{
    public class DirectoryBaseGroupSchemaAssetPostprocessor
    {
        [MenuItem("Addressable Packer/Rebuild Addressables Directory Groups")]
        static void RebuildAllFromMenu()
        {
            if (AddressableAssetSettingsDefaultObject.Settings == null) return;
            var importer = new DirectoryBaseSchemaPostAssetHandler(AddressableAssetSettingsDefaultObject.Settings);
            importer.RebuildAll();
        }

        [MenuItem("CONTEXT/AddressableAssetSettings/Rebuild Directory Groups")]
        static void RebuildAllFromSettings(MenuCommand menuCommand)
        {
            var importer = new DirectoryBaseSchemaPostAssetHandler(menuCommand.context as AddressableAssetSettings);
            importer.RebuildAll();
        }

        [MenuItem("Addressable Packer/Resolve Addressables Duplicate Assets")]
        static void CheckDuplicatesFromMenu()
        {
            if (AddressableAssetSettingsDefaultObject.Settings == null) return;
            var importer = new DirectoryBaseSchemaPostAssetHandler(AddressableAssetSettingsDefaultObject.Settings);
            importer.ResolveDuplicates();
        }

        [MenuItem("CONTEXT/AddressableAssetSettings/Resolve Duplicate Assets")]
        static void CheckDuplicatesFromSettings(MenuCommand menuCommand)
        {
            var importer = new DirectoryBaseSchemaPostAssetHandler(menuCommand.context as AddressableAssetSettings);
            importer.ResolveDuplicates();
        }
    }

    public class DirectoryBaseSchemaPostAssetHandler
    {
        readonly Dictionary<string, AssetGroupInfo> _dictionary = new();
        readonly AddressableAssetSettings _settings;
        bool _wasWindowOpened = false;

        public DirectoryBaseSchemaPostAssetHandler(AddressableAssetSettings settings)
        {
            _settings = settings;
            foreach (var group in _settings.groups)
            {
                var schema = group.GetSchema<DirectoryBaseGroupSchema>();
                if (schema == null || !schema.IsValid()) continue;

                if (!_dictionary.TryAdd(schema.DirectoryPath, new AssetGroupInfo
                    {
                        Group = schema.Group,
                        Extensions = schema.Extensions
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => $".{s.Trim().ToLowerInvariant()}")
                            .Distinct()
                            .ToHashSet()
                    }))
                {
                    Debug.LogWarning($"schema {schema.name}'s path is already included");
                }
            }
        }

        struct AssetGroupInfo
        {
            public AddressableAssetGroup Group;
            public HashSet<string> Extensions;
        }

        struct AssetEntryInfo
        {
            public string Address;
            public AddressableAssetGroup Group;
            public string AssetPath;
        }

        public void ResolveDuplicates()
        {
            CloseAddressableWindow();
            // Resolve duplication issues.
            var rule = new CheckBundleDupeDependenciesV2();
            rule.RefreshAnalysis(_settings);
            rule.FixIssues(_settings);

            EditorApplication.delayCall += OnComplete;
        }

        void CloseAddressableWindow()
        {
            var type = Type.GetType(
                "UnityEditor.AddressableAssets.GUI.AddressableAssetsWindow, Unity.Addressables.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            var window = EditorWindow.GetWindow(type);
            _wasWindowOpened = window != null;
            if (_wasWindowOpened) window.Close();
        }

        void OnComplete()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Complete", "Operation Complete", "Confirm");
                if (_wasWindowOpened) EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
            }
        }

        public void RebuildAll()
        {
            CloseAddressableWindow();
            // Clear only related entries.
            foreach (var group in _settings.groups)
            {
                var dirSchema = group.GetSchema<DirectoryBaseGroupSchema>();
                if (dirSchema == null) continue;

                foreach (var entry in group.entries.ToArray())
                {
                    _settings.RemoveAssetEntry(entry.guid);
                }
            }

            var pathList = new List<string>();

            // Clear only related entries.
            foreach (var group in _settings.groups)
            {
                var dirSchema = group.GetSchema<DirectoryBaseGroupSchema>();
                if (dirSchema == null) continue;

                GetFilesInDirectoryInternal(pathList, dirSchema.DirectoryPath, dirSchema.Recursive);
                pathList.Sort((a, b) => string.Compare(b, a, StringComparison.InvariantCulture));

                foreach (var assetPath in pathList)
                {
                    if (!TryGenerateAddressablePath(assetPath, out var entry)) continue;
                    if (group != entry.Group) continue;
                    ProcessImport(entry);
                }

                pathList.Clear();
            }

            EditorApplication.delayCall += OnComplete;
        }

        bool ProcessImport(AssetEntryInfo info)
        {
            var assetGuid = AssetDatabase.AssetPathToGUID(info.AssetPath);
            var prevEntry = _settings.FindAssetEntry(assetGuid);

            // Respect a previously added entry.
            if (prevEntry != null)
            {
                var prevSchema = prevEntry.parentGroup.GetSchema<DirectoryBaseGroupSchema>();
                if (prevSchema == null) return false;
            }

            var entry = _settings.CreateOrMoveEntry(assetGuid, info.Group, true, true);
            if (entry == null) return false;

            var labelString = $"${info.Group.name}";
            _settings.AddLabel(labelString);
            entry.address = info.Address;
            entry.labels.Clear();
            entry.labels.Add(labelString);

            return true;
        }

        bool ProcessDelete(string assetPath)
        {
            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = _settings.FindAssetEntry(assetGuid);
            if (entry == null) return false;
            _settings.RemoveAssetEntry(assetGuid);
            return true;
        }

        bool ProcessMove(string assetFrom, string assetTo)
        {
            var fromAddressable = TryGenerateAddressablePath(assetFrom, out _);
            var toAddressable = TryGenerateAddressablePath(assetTo, out var toEntry);

            // Move or create.
            if (toAddressable)
            {
                return ProcessImport(toEntry);
            }

            // Out of addressable from addressable.
            if (fromAddressable)
            {
                return ProcessDelete(assetTo);
            }

            return false;
        }

        bool TryGenerateAddressablePath(string assetPath, out AssetEntryInfo info)
        {
            var dirPath = assetPath;
            int lastIndex;

            while ((lastIndex = dirPath.LastIndexOf('/')) >= 0)
            {
                dirPath = dirPath.Remove(lastIndex);
                if (!_dictionary.TryGetValue(dirPath, out var schema)) continue;
                if (schema.Extensions.Count > 0 && !schema.Extensions.Contains(Path.GetExtension(assetPath).ToLowerInvariant())) continue;

                var address = assetPath.Remove(assetPath.LastIndexOf('.')).Remove(0, dirPath.Length + 1);
                address = $"{address}@{schema.Group.name}";

                info = new AssetEntryInfo
                {
                    AssetPath = assetPath,
                    Address = address,
                    Group = schema.Group
                };
                return true;
            }

            info = default;
            return false;
        }

        static void GetFilesInDirectoryInternal(List<string> results, string directory, bool recursive)
        {
            var dir = new DirectoryInfo(Path.GetFullPath(directory));
            if (!dir.Exists) return;

            var files = dir.GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                var currentFile = files[i];
                var unityPath = CombinePath(directory, currentFile.Name);

                if (unityPath.EndsWith(".meta")) continue;
                results.Add(unityPath);
            }

            if (recursive)
            {
                foreach (var subDir in dir.GetDirectories())
                {
                    var subdirName = $"{directory}/{subDir.Name}";
                    GetFilesInDirectoryInternal(results, subdirName, true);
                }
            }

            return;

            static string CombinePath(params string[] args)
            {
                var combined = Path.Combine(args);
                if (Path.DirectorySeparatorChar == '\\') combined = combined.Replace('\\', '/');
                return combined;
            }
        }
    }
}
