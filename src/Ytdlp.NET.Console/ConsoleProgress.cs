internal partial class Program
{
    internal static class ConsoleProgress
    {
        private static int _lastPercent = -1;

        public static void Update(double percent, string? extraInfo = null)
        {
            int current = (int)Math.Round(percent);
            if (current == _lastPercent && extraInfo == null) return;

            _lastPercent = current;

            // Build bar: [=====     ] 50%
            int barWidth = 30;
            int filled = (int)(barWidth * percent / 100);
            string bar = new string('=', filled) + new string(' ', barWidth - filled);

            string line = $"\r[{bar}] {current,3}%  {extraInfo ?? ""}";

            Console.Write(line.PadRight(Console.BufferWidth - 1));
        }

        public static void Clear()
        {
            _lastPercent = -1;
            Console.Write("\r" + new string(' ', Console.BufferWidth - 1) + "\r");
        }

        public static void Complete(string message = "Done!")
        {
            Console.WriteLine($"\r{message.PadRight(Console.BufferWidth - 1)}");
            _lastPercent = -1;
        }
    }

}