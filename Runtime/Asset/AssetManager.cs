using System;
using System.Threading.Tasks;
using GameFrameX.Runtime;
using YooAsset;
using Object = UnityEngine.Object;

namespace GameFrameX.Asset.Runtime
{
    /// <summary>
    /// 资源组件。
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public partial class AssetManager : GameFrameworkModule, IAssetManager
    {
        /// <summary>
        /// 默认包名称
        /// </summary>
        public string DefaultPackageName { get; set; } = ConstDefaultPackageName;

        /// <summary>
        /// 最大并发下载数量
        /// </summary>
        public int DownloadingMaxNum { get; set; }

        /// <summary>
        /// 失败重试次数
        /// </summary>
        public int FailedTryAgain { get; set; }

        /// <summary>
        /// 文件验证等级
        /// </summary>
        public EFileVerifyLevel VerifyLevel { get; set; }

        /// <summary>
        /// 操作系统最大时间片（单位：毫秒）
        /// </summary>
        public long Milliseconds { get; set; }

        private ResourcePackage GetDefaultResourcePackage()
        {
            return YooAssets.GetPackage(DefaultPackageName);
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public void Initialize()
        {
            Log.Info($"资源系统运行模式：{PlayMode}");
            YooAssets.Initialize();
            YooAssets.SetAsyncOperationMaxTimeSlice(Milliseconds > 0 ? Milliseconds : 30);
            // YooAssets.SetCacheSystemCachedFileVerifyLevel(EVerifyLevel.High);
            // YooAssets.SetDownloadSystemBreakpointResumeFileSize(4096 * 8);

            Log.Info("Asset Init Over");
        }


        /// <summary>
        /// 初始化操作。
        /// </summary>
        /// <param name="packageName">包名称</param>
        /// <param name="hostServerURL">热更链接URL。</param>
        /// <param name="fallbackHostServerURL">备用热更链接URL</param>
        /// <param name="isDefaultPackage">是否是默认包</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public Task<bool> InitPackageAsync(string packageName, string hostServerURL, string fallbackHostServerURL, bool isDefaultPackage = false)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            GameFrameworkGuard.NotNull(packageName, nameof(packageName));
            GameFrameworkGuard.NotNull(hostServerURL, nameof(hostServerURL));
            GameFrameworkGuard.NotNull(fallbackHostServerURL, nameof(fallbackHostServerURL));

            // 创建默认的资源包
            YooAssets.TryGetPackage(packageName, out var resourcePackage);
            if (resourcePackage == null)
            {
                resourcePackage = YooAssets.CreatePackage(packageName);
            }

            if (isDefaultPackage)
            {
                DefaultPackageName = packageName;
                // 设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
                YooAssets.SetDefaultPackage(resourcePackage);
            }

            var initializationOperationHandler = CreateInitializationOperationHandler(resourcePackage, hostServerURL, fallbackHostServerURL);
            initializationOperationHandler.Completed += asyncOperationBase =>
            {
                if (asyncOperationBase.Error == null && asyncOperationBase.Status == EOperationStatus.Succeeded && asyncOperationBase.IsDone)
                {
                    taskCompletionSource.TrySetResult(true);
                }
                else
                {
                    taskCompletionSource.TrySetException(new Exception(asyncOperationBase.Error));
                }
            };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        [UnityEngine.Scripting.Preserve]
        public void UnloadAsset(string assetPath)
        {
            GameFrameworkGuard.NotNull(assetPath, nameof(assetPath));
            var package = YooAssets.GetPackage(DefaultPackageName);
            package.TryUnloadUnusedAsset(assetPath);
        }

        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <param name="assetPath">资源路径</param>
        [UnityEngine.Scripting.Preserve]
        public void UnloadAsset(string packageName, string assetPath)
        {
            GameFrameworkGuard.NotNull(packageName, nameof(packageName));
            GameFrameworkGuard.NotNull(assetPath, nameof(assetPath));
            var package = YooAssets.GetPackage(packageName);
            package.TryUnloadUnusedAsset(assetPath);
        }


        /// <summary>
        /// 强制回收所有资源
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        [UnityEngine.Scripting.Preserve]
        public void UnloadAllAssetsAsync(string packageName = null)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                packageName = ConstDefaultPackageName;
            }

            var package = YooAssets.GetPackage(packageName);
            if (package != null)
            {
                package.UnloadAllAssetsAsync();
            }
        }

        /// <summary>
        /// 卸载无用资源
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        [UnityEngine.Scripting.Preserve]
        public void UnloadUnusedAssetsAsync(string packageName = null)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                packageName = ConstDefaultPackageName;
            }

            var package = YooAssets.GetPackage(packageName);
            if (package != null)
            {
                package.UnloadUnusedAssetsAsync();
            }
        }

        /// <summary>
        /// 清理所有资源
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        [UnityEngine.Scripting.Preserve]
        public void ClearAllBundleFilesAsync(string packageName = null)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                packageName = ConstDefaultPackageName;
            }

            var package = YooAssets.GetPackage(packageName);
            if (package != null)
            {
                package.UnloadAllAssetsAsync();
                package.ClearCacheAsync(new ClearCacheOptions(ClearCacheMethods.ClearAllBundleFiles));
            }
        }

        /// <summary>
        /// 清理无用资源包文件
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        [UnityEngine.Scripting.Preserve]
        public void ClearUnusedBundleFilesAsync(string packageName = null)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                packageName = ConstDefaultPackageName;
            }

            var package = YooAssets.GetPackage(packageName);
            if (package != null)
            {
                package.ClearCacheAsync(new ClearCacheOptions(ClearCacheMethods.ClearUnusedBundleFiles));
            }
        }


        #region 异步加载子资源对象

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public Task<SubAssetsHandle> LoadSubAssetsAsync(AssetInfo assetInfo)
        {
            var taskCompletionSource = new TaskCompletionSource<SubAssetsHandle>();
            var assetHandle = GetDefaultResourcePackage().LoadSubAssetsAsync(assetInfo);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type"></param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public Task<SubAssetsHandle> LoadSubAssetsAsync(string path, Type type)
        {
            var taskCompletionSource = new TaskCompletionSource<SubAssetsHandle>();
            var assetHandle = GetDefaultResourcePackage().LoadSubAssetsAsync(path, type);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public Task<SubAssetsHandle> LoadSubAssetsAsync<T>(string path) where T : Object
        {
            var taskCompletionSource = new TaskCompletionSource<SubAssetsHandle>();
            var assetHandle = GetDefaultResourcePackage().LoadSubAssetsAsync<T>(path);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        #endregion

        #region 异步加载子资源对象

        /// <summary>
        /// 同步加载子资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public SubAssetsHandle LoadSubAssetSync(AssetInfo assetInfo)
        {
            return GetDefaultResourcePackage().LoadSubAssetsSync(assetInfo);
        }

        /// <summary>
        /// 同步加载子资源对象
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type"></param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public SubAssetsHandle LoadSubAssetSync(string path, Type type)
        {
            return GetDefaultResourcePackage().LoadSubAssetsSync(path, type);
        }

        /// <summary>
        /// 同步加载子资源对象
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public SubAssetsHandle LoadSubAssetSync<T>(string path) where T : Object
        {
            return GetDefaultResourcePackage().LoadSubAssetsSync<T>(path);
        }

        #endregion

        #region 异步加载原生文件

        /// <summary>
        /// 异步加载原生文件
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public Task<AssetHandle> LoadRawFileAsync(AssetInfo assetInfo)
        {
            var taskCompletionSource = new TaskCompletionSource<AssetHandle>();
            var assetHandle = GetDefaultResourcePackage().LoadAssetAsync(assetInfo);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载原生文件
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public Task<AssetHandle> LoadRawFileAsync(string path)
        {
            var taskCompletionSource = new TaskCompletionSource<AssetHandle>();
            var assetHandle = GetDefaultResourcePackage().LoadAssetAsync<RawFileObject>(path);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        #endregion

        #region 同步加载原生文件

        /// <summary>
        /// 同步加载原生文件
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public AssetHandle LoadRawFileSync(AssetInfo assetInfo)
        {
            return GetDefaultResourcePackage().LoadAssetSync(assetInfo);
        }

        /// <summary>
        /// 同步加载原生文件
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public AssetHandle LoadRawFileSync(string path)
        {
            return GetDefaultResourcePackage().LoadAssetSync<RawFileObject>(path);
        }

        #endregion


        #region 异步加载资源

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public Task<AssetHandle> LoadAssetAsync(AssetInfo assetInfo)
        {
            var taskCompletionSource = new TaskCompletionSource<AssetHandle>();
            var assetHandle = GetDefaultResourcePackage().LoadAssetAsync(assetInfo);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type">资源类型</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public Task<AssetHandle> LoadAssetAsync(string path, Type type)
        {
            var taskCompletionSource = new TaskCompletionSource<AssetHandle>();
            var assetHandle = GetDefaultResourcePackage().LoadAssetAsync(path, type);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载全部资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public Task<AllAssetsHandle> LoadAllAssetsAsync<T>(string path) where T : Object
        {
            var taskCompletionSource = new TaskCompletionSource<AllAssetsHandle>();
            var assetHandle = GetDefaultResourcePackage().LoadAllAssetsAsync<T>(path);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载全部资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type">资源类型</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public Task<AllAssetsHandle> LoadAllAssetsAsync(string path, Type type)
        {
            var taskCompletionSource = new TaskCompletionSource<AllAssetsHandle>();
            var assetHandle = GetDefaultResourcePackage().LoadAllAssetsAsync(path, type);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载资源包内所有资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        [UnityEngine.Scripting.Preserve]
        public Task<AllAssetsHandle> LoadAllAssetsAsync(string path)
        {
            var taskCompletionSource = new TaskCompletionSource<AllAssetsHandle>();
            var assetHandle = GetDefaultResourcePackage().LoadAllAssetsAsync(path);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载资源包内所有资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        [UnityEngine.Scripting.Preserve]
        public Task<AllAssetsHandle> LoadAllAssetsAsync(AssetInfo assetInfo)
        {
            var taskCompletionSource = new TaskCompletionSource<AllAssetsHandle>();
            var assetHandle = GetDefaultResourcePackage().LoadAllAssetsAsync(assetInfo);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        [UnityEngine.Scripting.Preserve]
        public SubAssetsHandle LoadSubAssetsAsync(string path)
        {
            return GetDefaultResourcePackage().LoadSubAssetsAsync(path);
        }


        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public Task<AssetHandle> LoadAssetAsync(string path)
        {
            var taskCompletionSource = new TaskCompletionSource<AssetHandle>();
            var assetHandle = GetDefaultResourcePackage().LoadAssetAsync(path);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public Task<AssetHandle> LoadAssetAsync<T>(string path) where T : Object
        {
            var taskCompletionSource = new TaskCompletionSource<AssetHandle>();
            var assetHandle = GetDefaultResourcePackage().LoadAssetAsync<T>(path);

            void OnAssetHandleOnCompleted(AssetHandle handle)
            {
                taskCompletionSource.TrySetResult(handle);
            }

            assetHandle.Completed += OnAssetHandleOnCompleted;
            return taskCompletionSource.Task;
        }

        #endregion

        #region 同步加载资源

        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        [UnityEngine.Scripting.Preserve]
        public AllAssetsHandle LoadAllAssetsSync(string path)
        {
            return GetDefaultResourcePackage().LoadAllAssetsSync(path);
        }

        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源的定位地址</param>
        [UnityEngine.Scripting.Preserve]
        public AllAssetsHandle LoadAllAssetsSync<T>(string path) where T : Object
        {
            return GetDefaultResourcePackage().LoadAllAssetsSync<T>(path);
        }

        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        /// <param name="type">子对象类型</param>
        [UnityEngine.Scripting.Preserve]
        public AllAssetsHandle LoadAllAssetsSync(string path, Type type)
        {
            return GetDefaultResourcePackage().LoadAllAssetsSync(path, type);
        }

        /// <summary>
        /// 同步加载包内全部资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public AllAssetsHandle LoadAllAssetsSync(AssetInfo assetInfo)
        {
            return GetDefaultResourcePackage().LoadAllAssetsSync(assetInfo);
        }

        /// <summary>
        /// 同步加载子资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public SubAssetsHandle LoadSubAssetSync(string path)
        {
            return GetDefaultResourcePackage().LoadSubAssetsSync(path);
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public AssetHandle LoadAssetSync(string path)
        {
            return GetDefaultResourcePackage().LoadAssetSync(path);
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type"></param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public AssetHandle LoadAssetSync(string path, Type type)
        {
            return GetDefaultResourcePackage().LoadAssetSync(path, type);
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public AssetHandle LoadAssetSync(AssetInfo assetInfo)
        {
            return GetDefaultResourcePackage().LoadAssetSync(assetInfo);
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public AssetHandle LoadAssetSync<T>(string path) where T : Object
        {
            return GetDefaultResourcePackage().LoadAssetSync<T>(path);
        }

        #endregion

        #region 加载场景

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="sceneMode">场景模式</param>
        /// <param name="activateOnLoad">是否加载完成自动激活</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public Task<SceneHandle> LoadSceneAsync(string path, UnityEngine.SceneManagement.LoadSceneMode sceneMode, bool activateOnLoad = true)
        {
            var taskCompletionSource = new TaskCompletionSource<SceneHandle>();
            var sceneHandle = GetDefaultResourcePackage().LoadSceneAsync(path, sceneMode, UnityEngine.SceneManagement.LocalPhysicsMode.None, activateOnLoad);
            sceneHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="assetInfo">资源路径</param>
        /// <param name="sceneMode">场景模式</param>
        /// <param name="activateOnLoad">是否加载完成自动激活</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public Task<SceneHandle> LoadSceneAsync(AssetInfo assetInfo, UnityEngine.SceneManagement.LoadSceneMode sceneMode, bool activateOnLoad = true)
        {
            var taskCompletionSource = new TaskCompletionSource<SceneHandle>();
            var sceneHandle = GetDefaultResourcePackage().LoadSceneAsync(assetInfo, sceneMode, UnityEngine.SceneManagement.LocalPhysicsMode.None, activateOnLoad);
            sceneHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        #endregion

        #region 资源包

        /// <summary>
        /// 创建资源包
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public ResourcePackage CreateAssetsPackage(string packageName)
        {
            return YooAssets.CreatePackage(packageName);
        }

        /// <summary>
        /// 尝试获取资源包
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public ResourcePackage TryGetAssetsPackage(string packageName)
        {
            YooAssets.TryGetPackage(packageName, out var resourcePackage);
            return resourcePackage;
        }

        /// <summary>
        /// 检查资源包是否存在
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public bool HasAssetsPackage(string packageName)
        {
            return YooAssets.ContainsPackage(packageName);
        }

        /// <summary>
        /// 获取资源包
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public ResourcePackage GetAssetsPackage(string packageName)
        {
            return YooAssets.GetPackage(packageName);
        }

        #endregion

        /// <summary>
        /// 是否需要下载
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public bool IsNeedDownload(AssetInfo assetInfo)
        {
            return GetDefaultResourcePackage().GetDownloadSize(assetInfo) > 0;
        }

        /// <summary>
        /// 是否需要下载
        /// </summary>
        /// <param name="path">资源地址</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public bool IsNeedDownload(string path)
        {
            return GetDefaultResourcePackage().GetDownloadSize(path) > 0;
        }

        /// <summary>
        /// 获取资源信息
        /// </summary>
        /// <param name="assetTags">资源标签列表</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public AssetInfo[] GetAssetInfos(string[] assetTags)
        {
            return GetDefaultResourcePackage().GetAssetInfos(assetTags);
        }

        /// <summary>
        /// 获取资源信息
        /// </summary>
        /// <param name="assetTag">资源标签</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public AssetInfo[] GetAssetInfos(string assetTag)
        {
            return GetDefaultResourcePackage().GetAssetInfos(assetTag);
        }

        /// <summary>
        /// 获取资源信息
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public AssetInfo GetAssetInfo(string path)
        {
            return GetDefaultResourcePackage().GetAssetInfo(path);
        }

        /// <summary>
        /// 检查指定的资源路径是否有效。
        /// </summary>
        /// <param name="path">要检查的资源路径。</param>
        /// <returns>如果资源路径有效，则返回 true；否则返回 false。</returns>
        [UnityEngine.Scripting.Preserve]
        public bool HasAssetPath(string path)
        {
            return GetDefaultResourcePackage().IsLocationValid(path);
        }

        /// <summary>
        /// 设置默认资源包
        /// </summary>
        /// <param name="resourcePackage">资源信息</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public void SetDefaultAssetsPackage(ResourcePackage resourcePackage)
        {
            DefaultPackageName = resourcePackage.PackageName;
            YooAssets.SetDefaultPackage(resourcePackage);
        }


        protected override void Update(float elapseSeconds, float realElapseSeconds)
        {
        }

        protected override void Shutdown()
        {
        }

        /// <summary>
        /// 获取或设置运行模式。
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public EPlayMode PlayMode { get; private set; }

        /// <summary>
        /// 设置运行模式
        /// </summary>
        /// <param name="playMode">运行模式</param>
        [UnityEngine.Scripting.Preserve]
        public void SetPlayMode(EPlayMode playMode)
        {
            PlayMode = playMode;
        }
    }
}
