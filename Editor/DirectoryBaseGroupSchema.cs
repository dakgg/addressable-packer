using System.ComponentModel;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Settings.GroupSchemas
{
    /// <summary>
    /// Schema for the auto directory base
    /// </summary>
    [DisplayName("Auto Asset Directory")]
    public class DirectoryBaseGroupSchema : AddressableAssetGroupSchema
    {
        [Tooltip("Directory that contains assets")]
        [SerializeField]
        string _directoryPath = string.Empty;
        
        [Tooltip("Enable recursive search for assets")]
        [SerializeField]
        bool _recursive = true;
        
        [Tooltip("File extensions for asset search")]
        [SerializeField]
        string _extensions = string.Empty;
        
        /// <summary>
        /// Gets or sets the directory path for assets in resource folders.
        /// </summary>
        public string DirectoryPath
        {
            get => _directoryPath;
            set
            {
                if (_directoryPath != value)
                {
                    _directoryPath = value;
                    SetDirty(true);
                }
            }
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the search for assets is recursive.
        /// </summary>
        public bool Recursive
        {
            get => _recursive;
            set
            {
                if (_recursive != value)
                {
                    _recursive = value;
                    SetDirty(true);
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the file extensions to consider during asset search (comma-separated).
        /// </summary>
        public string Extensions
        {
            get => _extensions;
            set
            {
                if (_extensions != value)
                {
                    _extensions = value;
                    SetDirty(true);
                }
            }
        }
        
        /// <summary>
        /// Does schema have valid path
        /// </summary>
        public bool IsValid()
        {
            return AssetDatabase.IsValidFolder(_directoryPath);
        }
    }
}
