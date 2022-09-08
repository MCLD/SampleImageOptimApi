namespace ConsoleImageOptimApi
{
    internal class OptionException : Exception
    {
        public OptionException() : base()
        {
        }

        public OptionException(string? message) : base(message)
        {
        }

        public OptionException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}