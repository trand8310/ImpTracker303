using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainClient.Common
{
    internal class FileHelper
    {

        /// <summary>
        /// 清理 Cef 缓存目录
        /// 规则：
        /// 1. 超过 keepRecentDays 天的文件直接删
        /// 2. 如果目录总大小超过 maxTotalSizeMb，从最旧文件开始删
        /// </summary>
        public static void CleanCefCache(string cachePath, long maxTotalSizeMb, int keepRecentDays)
        {


            try
            {
                if (string.IsNullOrWhiteSpace(cachePath))
                    return;

                if (!Directory.Exists(cachePath))
                    return;

                var dir = new DirectoryInfo(cachePath);
                var now = DateTime.Now;
                var expireTime = now.AddDays(-keepRecentDays);

                // 先删过期文件
                foreach (var file in dir.GetFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        if (file.LastWriteTime < expireTime)
                        {
                            file.IsReadOnly = false;
                            file.Delete();
                        }
                    }
                    catch
                    {
                        // Cef 运行中可能占用部分文件，忽略即可
                    }
                }

                // 删除空目录
                DeleteEmptyDirectories(cachePath);

                // 再判断总大小
                long maxBytes = maxTotalSizeMb * 1024L * 1024L;

                var files = dir.GetFiles("*", SearchOption.AllDirectories)
                               .OrderBy(f => f.LastWriteTime)
                               .ToList();

                long totalSize = files.Sum(f =>
                {
                    try { return f.Length; }
                    catch { return 0L; }
                });

                if (totalSize <= maxBytes)
                    return;

                foreach (var file in files)
                {
                    if (totalSize <= maxBytes)
                        break;

                    try
                    {
                        long len = file.Length;
                        file.IsReadOnly = false;
                        file.Delete();
                        totalSize -= len;
                    }
                    catch
                    {
                        // 文件被占用就跳过
                    }
                }

                DeleteEmptyDirectories(cachePath);
            }
            catch
            {
                // 清理失败不影响主程序启动
            }
        }
        public static void DeleteEmptyDirectories(string rootPath)
        {
            try
            {
                if (!Directory.Exists(rootPath))
                    return;

                foreach (var dir in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories)
                                             .OrderByDescending(x => x.Length))
                {
                    try
                    {
                        if (!Directory.EnumerateFileSystemEntries(dir).Any())
                        {
                            Directory.Delete(dir, false);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }
    }
}
