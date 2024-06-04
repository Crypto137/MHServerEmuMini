namespace MHServerEmuMini
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Title = $"MHServerEmuMini ({ServerApp.VersionInfo})";
            ServerApp.Instance.Run();
        }
    }
}
