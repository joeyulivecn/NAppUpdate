using CommandLine;
using CommandLine.Text;
using System;
using System.Windows.Forms;
using System.IO;

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
                var options = new Options();
                if (CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    // Values are available here
                    if (!File.Exists(options.ConfigFileName)) Console.WriteLine("Config file does not exist. {0}", options.ConfigFileName);

                    var builder = new FeedCliBuilder(options);
                    builder.Run();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
		}
	}

    public class Options
    {
        [Option('f', "file", Required = true, HelpText = "Config file name.")]
        public string ConfigFileName { get; set; }

        [Option('d', "dir", Required = true,  HelpText = "Input directory to be processed.")]
        public string OutputFolder { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output path of feed xml.")]
        public string FeedXML { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

}
