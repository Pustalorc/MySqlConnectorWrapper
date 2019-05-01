namespace Pustalorc.Libraries.AbstractDatabase
{
    public abstract class AbstractDatabaseConfiguration
    {
        public string DatabaseAddress;
        public string DatabaseName;
        public string DatabasePassword;
        public ushort DatabasePort;
        public string DatabaseUsername;
        public bool UseCache;
        public bool UseSeparateThread;

        protected AbstractDatabaseConfiguration()
        {
            DatabaseAddress = "localhost";
            DatabaseUsername = "root";
            DatabasePassword = "password";
            DatabaseName = "database";
            DatabasePort = 3306;
            UseSeparateThread = true;
            UseCache = true;
        }
    }
}