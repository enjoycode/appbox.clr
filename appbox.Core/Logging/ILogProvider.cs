namespace appbox.Logging
{
    public interface ILogProvider
    {
        void Write(LogLevel level, string file, int line, string method, string msg);
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error
    }
}

