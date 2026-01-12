using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MLogger
{
    [CreateAssetMenu(fileName = "MLoggerSettings", menuName = "MLogger/Settings", order = 1)]
    public class MLoggerSettings : ScriptableObject
    {
        [SerializeField] private MLoggerConfig config = new();

        public MLoggerConfig Config
        {
            get
            {
                config ??= MLoggerConfig.CreateDefault();
                return config;
            }
            set => config = value;
        }

        private static MLoggerSettings _instance;
        private const string SettingsPath = "Assets/Plugins/MLogger/Resources/MLoggerSettings.asset";

        public static MLoggerSettings Instance
        {
            get
            {
                _instance ??= Resources.Load<MLoggerSettings>("MLoggerSettings");
                return _instance;
            }
        }

#if UNITY_EDITOR
        public static MLoggerSettings GetOrCreateSettings()
        {
            var settings = Instance;
            if (settings == null)
            {
                settings = CreateInstance<MLoggerSettings>();
                settings.config = MLoggerConfig.CreateDefault();

                var directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory ??
                                              throw new InvalidOperationException("Can't get settings path."));
                }

                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
                _instance = settings;
            }

            return settings;
        }
#endif
    }
}