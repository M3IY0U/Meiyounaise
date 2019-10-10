namespace Meiyounaise
{
    class Program
    {
        private static void Main()
        {
            using (var b = new Bot())
            {
                b.RunAsync().Wait();
            }
        }
    }
}