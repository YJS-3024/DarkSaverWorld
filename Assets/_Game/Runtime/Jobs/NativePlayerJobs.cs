namespace DarkSaver.Jobs
{
    // Later installation: source levelptl IDs 100..172, with 300 levels for the three 10th-stage classes.
    public static class NativePlayerJobs
    {
        public const int First = 100;
        public const int Last = 172;
        public static bool Contains(int jobId) => jobId >= First && jobId <= Last;
        public static int MaximumLevel(int jobId) => jobId >= 170 && jobId <= Last ? 300 : 100;
    }
}
