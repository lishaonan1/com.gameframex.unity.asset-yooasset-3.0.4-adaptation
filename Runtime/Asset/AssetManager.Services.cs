using System.Collections.Generic;
using YooAsset;

namespace GameFrameX.Asset.Runtime
{
    public partial class AssetManager
    {
        [UnityEngine.Scripting.Preserve]
        private class RemoteServices : IRemoteService
        {
            [UnityEngine.Scripting.Preserve] public string HostServer { get; }
            [UnityEngine.Scripting.Preserve] public string FallbackHostServer { get; }

            [UnityEngine.Scripting.Preserve]
            public RemoteServices(string hostServer, string fallbackHostServer)
            {
                HostServer = hostServer;
                FallbackHostServer = fallbackHostServer;
            }

            [UnityEngine.Scripting.Preserve]
            public IReadOnlyList<string> GetRemoteUrls(string fileName)
            {
                var urls = new List<string>(2)
                {
                    CombineRemoteUrl(HostServer, fileName)
                };
                if (string.Equals(HostServer, FallbackHostServer) == false)
                {
                    urls.Add(CombineRemoteUrl(FallbackHostServer, fileName));
                }

                return urls;
            }

            private static string CombineRemoteUrl(string hostServer, string fileName)
            {
                if (string.IsNullOrEmpty(hostServer))
                {
                    return fileName ?? string.Empty;
                }

                if (string.IsNullOrEmpty(fileName))
                {
                    return hostServer;
                }

                return $"{hostServer.TrimEnd('/')}/{fileName.TrimStart('/')}";
            }
        }
    }
}
