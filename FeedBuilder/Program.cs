using System;
using System.Windows.Forms;

namespace FeedBuilder
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
        static void Main(string[] args)
		{
            try
            {
                var builder = new FeedCliBuilder();
                builder.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.ReadKey();
		}
	}
}
