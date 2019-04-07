namespace Meiyounaise
{
    class Program
    {
        private static void Main(string[] args)
        {
            using (var b = new Bot())
            {
                b.RunAsync().Wait();
            }
        }
    }
}