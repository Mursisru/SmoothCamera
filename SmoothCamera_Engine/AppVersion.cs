namespace SmoothCamera_Engine
{
    public static class AppVersion
    {
        public const string ReleaseBase = "1.0.0";
        public const string VersionChannel = "PR-R";
        public const int CycleBuildNumber = 2;
        public const string ChangeLetters = "SP";
        public const int SubNumber = 52;

        public static string BuildToken => $"{VersionChannel}{CycleBuildNumber}{ChangeLetters}{SubNumber}";
        public static string Display => $"{ReleaseBase} Build {BuildToken}";
    }
}
