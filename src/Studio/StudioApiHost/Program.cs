namespace StudioApiHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = StudioApiHostApplication.BuildApp(args);

            app.Run();
        }
    }
}
