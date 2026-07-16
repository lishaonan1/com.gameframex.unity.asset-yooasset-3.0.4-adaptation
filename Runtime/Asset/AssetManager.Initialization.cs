using System;
using YooAsset;

namespace GameFrameX.Asset.Runtime
{
    public partial class AssetManager
    {
        public const string ConstDefaultPackageName = "DefaultPackage";

        /// <summary>
        /// 根据运行模式创建 YooAsset 3.x 初始化操作。
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        private InitializePackageOperation CreateInitializationOperationHandler(ResourcePackage resourcePackage, string hostServerURL, string fallbackHostServerURL)
        {
            switch (PlayMode)
            {
                case EPlayMode.EditorSimulateMode:
                    return InitializeYooAssetEditorSimulateMode(resourcePackage);

                case EPlayMode.OfflinePlayMode:
                    return InitializeYooAssetOfflinePlayMode(resourcePackage);

                case EPlayMode.HostPlayMode:
                    return InitializeYooAssetHostPlayMode(resourcePackage, hostServerURL, fallbackHostServerURL);

                case EPlayMode.WebPlayMode:
                    return InitializeYooAssetWebPlayMode(resourcePackage, hostServerURL, fallbackHostServerURL);

                default:
                    throw new ArgumentOutOfRangeException(nameof(PlayMode), PlayMode, $"Unsupported play mode: {PlayMode}");
            }
        }

        /// <summary>
        /// 初始化 YooAsset 编辑器模拟运行模式。
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        private InitializePackageOperation InitializeYooAssetEditorSimulateMode(ResourcePackage resourcePackage)
        {
            var simulateBuildResult = EditorSimulateBuildInvoker.Build(resourcePackage.PackageName, (int)EBundleType.VirtualAssetBundle);
            var options = new EditorSimulateModeOptions
            {
                EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult.PackageRootDirectory)
            };
            return resourcePackage.InitializePackageAsync(options);
        }

        /// <summary>
        /// 初始化 YooAsset 单机运行模式。
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        private InitializePackageOperation InitializeYooAssetOfflinePlayMode(ResourcePackage resourcePackage)
        {
            var options = new OfflinePlayModeOptions
            {
                BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters()
            };
            return resourcePackage.InitializePackageAsync(options);
        }

        /// <summary>
        /// 初始化 YooAsset WebGL 运行模式。
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        private InitializePackageOperation InitializeYooAssetWebPlayMode(ResourcePackage resourcePackage, string hostServerURL, string fallbackHostServerURL)
        {
            var remoteServices = new RemoteServices(hostServerURL, fallbackHostServerURL);
            var options = new WebPlayModeOptions
            {
                WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters(),
                WebNetworkFileSystemParameters = FileSystemParameters.CreateDefaultWebNetworkFileSystemParameters(remoteServices)
            };
            return resourcePackage.InitializePackageAsync(options);
        }

        /// <summary>
        /// 初始化 YooAsset 热更新运行模式。
        /// </summary>
        private InitializePackageOperation InitializeYooAssetHostPlayMode(ResourcePackage resourcePackage, string hostServerURL, string fallbackHostServerURL)
        {
            var remoteServices = new RemoteServices(hostServerURL, fallbackHostServerURL);
            var options = new HostPlayModeOptions
            {
                BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(),
                CacheFileSystemParameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remoteServices)
            };
            return resourcePackage.InitializePackageAsync(options);
        }
    }
}
