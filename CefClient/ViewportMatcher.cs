using System;
using System.Linq;

namespace CefClient
{
    public enum DevicePlatform
    {
        Android,
        iPhone
    }

    public sealed class DeviceProfileResult
    {
        public DevicePlatform Platform { get; set; }

        public int PhysicalWidth { get; set; }
        public int PhysicalHeight { get; set; }

        public int CssWidth { get; set; }
        public int CssHeight { get; set; }

        public float DeviceScaleFactor { get; set; }

        public double DprX { get; set; }
        public double DprY { get; set; }

        public double Score { get; set; }

        public override string ToString()
        {
            return string.Format(
                "Platform={0}, Physical={1}x{2}, CSS={3}x{4}, DPR={5:F3}, Score={6:F6}",
                Platform,
                PhysicalWidth,
                PhysicalHeight,
                CssWidth,
                CssHeight,
                DeviceScaleFactor,
                Score
            );
        }
    }

    public static class ViewportMatcher
    {
        private sealed class DeviceProfile
        {
            public DevicePlatform Platform { get; private set; }
            public int CssW { get; private set; }
            public int CssH { get; private set; }

            public DeviceProfile(DevicePlatform platform, int cssW, int cssH)
            {
                Platform = platform;
                CssW = cssW;
                CssH = cssH;
            }
        }

        // 主流 Android / iPhone CSS 档位
        private static readonly DeviceProfile[] Profiles =
        {
            // =========================
            // Android
            // =========================
            new DeviceProfile(DevicePlatform.Android, 360, 800),
            new DeviceProfile(DevicePlatform.Android, 360, 780),
            new DeviceProfile(DevicePlatform.Android, 360, 760),
            new DeviceProfile(DevicePlatform.Android, 360, 740),

            new DeviceProfile(DevicePlatform.Android, 393, 851),
            new DeviceProfile(DevicePlatform.Android, 393, 873),
            new DeviceProfile(DevicePlatform.Android, 392, 872),
            new DeviceProfile(DevicePlatform.Android, 390, 844),

            new DeviceProfile(DevicePlatform.Android, 412, 915),
            new DeviceProfile(DevicePlatform.Android, 412, 891),
            new DeviceProfile(DevicePlatform.Android, 412, 869),

            new DeviceProfile(DevicePlatform.Android, 384, 854),
            new DeviceProfile(DevicePlatform.Android, 411, 914),

            // =========================
            // iPhone
            // 常见 Safari CSS 视口
            // =========================
            new DeviceProfile(DevicePlatform.iPhone, 320, 568), // iPhone SE 1代
            new DeviceProfile(DevicePlatform.iPhone, 375, 667), // 6/7/8/SE2/SE3
            new DeviceProfile(DevicePlatform.iPhone, 375, 812), // X/XS/11 Pro/12 mini/13 mini
            new DeviceProfile(DevicePlatform.iPhone, 390, 844), // 12/12 Pro/13/13 Pro/14
            new DeviceProfile(DevicePlatform.iPhone, 393, 852), // 14 Pro
            new DeviceProfile(DevicePlatform.iPhone, 414, 736), // 6+/7+/8+
            new DeviceProfile(DevicePlatform.iPhone, 414, 896), // XR/11/11 Pro Max
            new DeviceProfile(DevicePlatform.iPhone, 428, 926), // 12 Pro Max/13 Pro Max/14 Plus
            new DeviceProfile(DevicePlatform.iPhone, 430, 932), // 14 Pro Max/15 Pro Max
            new DeviceProfile(DevicePlatform.iPhone, 393, 852), // 15 / 15 Pro 可近似这一档
            new DeviceProfile(DevicePlatform.iPhone, 430, 932), // 16 Pro Max 可近似这一档
            new DeviceProfile(DevicePlatform.iPhone, 402, 874)  // 新一点 Pro 系列可兜底近似
        };

        // 常见 Android DPR
        private static readonly float[] AndroidCommonDprs =
        {
            2.5f,
            2.625f,
            2.75f,
            3.0f,
            3.5f
        };

        // 常见 iPhone DPR
        private static readonly float[] IPhoneCommonDprs =
        {
            2.0f,
            3.0f
        };

        public static DeviceProfileResult Match(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException("width/height", "分辨率必须大于 0");

            int physicalWidth = Math.Min(width, height);
            int physicalHeight = Math.Max(width, height);

            DeviceProfileResult best = null;

            for (int i = 0; i < Profiles.Length; i++)
            {
                DeviceProfile p = Profiles[i];

                double dprX = (double)physicalWidth / p.CssW;
                double dprY = (double)physicalHeight / p.CssH;
                double avgDpr = (dprX + dprY) / 2.0;

                if (!IsReasonableDpr(p.Platform, avgDpr))
                    continue;

                // 主评分：横纵 DPR 越接近越好
                double score = Math.Abs(dprX - dprY);

                // 轻微倾向常见宽度
                score += GetWidthPenalty(p.Platform, p.CssW);

                // 轻微倾向常见 DPR
                score += GetDprPenalty(p.Platform, avgDpr);

                var result = new DeviceProfileResult
                {
                    Platform = p.Platform,
                    PhysicalWidth = physicalWidth,
                    PhysicalHeight = physicalHeight,
                    CssWidth = p.CssW,
                    CssHeight = p.CssH,
                    DeviceScaleFactor = NormalizeDpr(p.Platform, (float)avgDpr),
                    DprX = dprX,
                    DprY = dprY,
                    Score = score
                };

                if (best == null || result.Score < best.Score)
                    best = result;
            }

            if (best == null)
            {
                // 兜底逻辑：
                // 优先尝试像 iPhone 就落到 390 宽 / 3x 或 375 宽 / 3x
                // 否则按 Android 360 宽来兜
                if (physicalWidth <= 1300)
                {
                    float iphoneDpr = 3.0f;
                    int iphoneCssW = (int)Math.Round(physicalWidth / iphoneDpr);
                    int iphoneCssH = (int)Math.Round(physicalHeight / iphoneDpr);

                    if (iphoneCssW >= 320 && iphoneCssW <= 430)
                    {
                        return new DeviceProfileResult
                        {
                            Platform = DevicePlatform.iPhone,
                            PhysicalWidth = physicalWidth,
                            PhysicalHeight = physicalHeight,
                            CssWidth = iphoneCssW,
                            CssHeight = iphoneCssH,
                            DeviceScaleFactor = iphoneDpr,
                            DprX = iphoneDpr,
                            DprY = iphoneDpr,
                            Score = 999
                        };
                    }
                }

                float dpr = (float)physicalWidth / 360f;
                int cssHeight = (int)Math.Round(physicalHeight / dpr);

                return new DeviceProfileResult
                {
                    Platform = DevicePlatform.Android,
                    PhysicalWidth = physicalWidth,
                    PhysicalHeight = physicalHeight,
                    CssWidth = 360,
                    CssHeight = cssHeight,
                    DeviceScaleFactor = NormalizeDpr(DevicePlatform.Android, dpr),
                    DprX = dpr,
                    DprY = dpr,
                    Score = 999
                };
            }

            return best;
        }

        public static DeviceProfileResult MatchAndroid(int width, int height)
        {
            return MatchByPlatform(width, height, DevicePlatform.Android);
        }

        public static DeviceProfileResult MatchIPhone(int width, int height)
        {
            return MatchByPlatform(width, height, DevicePlatform.iPhone);
        }

        private static DeviceProfileResult MatchByPlatform(int width, int height, DevicePlatform platform)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException("width/height", "分辨率必须大于 0");

            int physicalWidth = Math.Min(width, height);
            int physicalHeight = Math.Max(width, height);

            DeviceProfileResult best = null;

            for (int i = 0; i < Profiles.Length; i++)
            {
                DeviceProfile p = Profiles[i];
                if (p.Platform != platform)
                    continue;

                double dprX = (double)physicalWidth / p.CssW;
                double dprY = (double)physicalHeight / p.CssH;
                double avgDpr = (dprX + dprY) / 2.0;

                if (!IsReasonableDpr(platform, avgDpr))
                    continue;

                double score = Math.Abs(dprX - dprY);
                score += GetWidthPenalty(platform, p.CssW);
                score += GetDprPenalty(platform, avgDpr);

                var result = new DeviceProfileResult
                {
                    Platform = p.Platform,
                    PhysicalWidth = physicalWidth,
                    PhysicalHeight = physicalHeight,
                    CssWidth = p.CssW,
                    CssHeight = p.CssH,
                    DeviceScaleFactor = NormalizeDpr(platform, (float)avgDpr),
                    DprX = dprX,
                    DprY = dprY,
                    Score = score
                };

                if (best == null || result.Score < best.Score)
                    best = result;
            }

            if (best != null)
                return best;

            if (platform == DevicePlatform.iPhone)
            {
                float dpr = 3.0f;
                int cssWidth = (int)Math.Round(physicalWidth / dpr);
                int cssHeight = (int)Math.Round(physicalHeight / dpr);

                return new DeviceProfileResult
                {
                    Platform = DevicePlatform.iPhone,
                    PhysicalWidth = physicalWidth,
                    PhysicalHeight = physicalHeight,
                    CssWidth = cssWidth,
                    CssHeight = cssHeight,
                    DeviceScaleFactor = NormalizeDpr(DevicePlatform.iPhone, dpr),
                    DprX = dpr,
                    DprY = dpr,
                    Score = 999
                };
            }
            else
            {
                float dpr = (float)physicalWidth / 360f;
                int cssHeight = (int)Math.Round(physicalHeight / dpr);

                return new DeviceProfileResult
                {
                    Platform = DevicePlatform.Android,
                    PhysicalWidth = physicalWidth,
                    PhysicalHeight = physicalHeight,
                    CssWidth = 360,
                    CssHeight = cssHeight,
                    DeviceScaleFactor = NormalizeDpr(DevicePlatform.Android, dpr),
                    DprX = dpr,
                    DprY = dpr,
                    Score = 999
                };
            }
        }

        private static bool IsReasonableDpr(DevicePlatform platform, double dpr)
        {
            if (platform == DevicePlatform.iPhone)
                return dpr >= 1.9 && dpr <= 3.2;

            return dpr >= 2.4 && dpr <= 3.6;
        }

        private static double GetWidthPenalty(DevicePlatform platform, int cssWidth)
        {
            if (platform == DevicePlatform.iPhone)
            {
                if (cssWidth == 390) return 0.0000;
                if (cssWidth == 393) return 0.0004;
                if (cssWidth == 375) return 0.0008;
                if (cssWidth == 430) return 0.0010;
                if (cssWidth == 428) return 0.0012;
                if (cssWidth == 414) return 0.0015;
                if (cssWidth == 320) return 0.0020;
                if (cssWidth == 402) return 0.0022;
                return 0.0030;
            }

            if (cssWidth == 360) return 0.0000;
            if (cssWidth == 393) return 0.0010;
            if (cssWidth == 392) return 0.0012;
            if (cssWidth == 390) return 0.0015;
            if (cssWidth == 412) return 0.0020;
            if (cssWidth == 411) return 0.0022;
            if (cssWidth == 384) return 0.0030;
            return 0.0050;
        }

        private static double GetDprPenalty(DevicePlatform platform, double dpr)
        {
            float[] commonDprs = platform == DevicePlatform.iPhone
                ? IPhoneCommonDprs
                : AndroidCommonDprs;

            double nearestDiff = commonDprs.Min(x => Math.Abs(x - dpr));
            return nearestDiff * 0.01;
        }

        private static float NormalizeDpr(DevicePlatform platform, float dpr)
        {
            float[] commonDprs = platform == DevicePlatform.iPhone
                ? IPhoneCommonDprs
                : AndroidCommonDprs;

            for (int i = 0; i < commonDprs.Length; i++)
            {
                if (Math.Abs(dpr - commonDprs[i]) <= 0.08f)
                    return commonDprs[i];
            }

            return (float)Math.Round(dpr, 3, MidpointRounding.AwayFromZero);
        }
    }
}