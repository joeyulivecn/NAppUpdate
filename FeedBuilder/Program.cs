using CommandLine;
using CommandLine.Text;
using System;
using System.Windows.Forms;
using System.IO;
using System.Linq;

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
                    if (args.Contains("-n")) options.AddExtension = false;

                    // Build Feed
                    var builder = new FeedCliBuilder();
                    builder.Run(options);
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

        [Option('d', "dir", Required = true, HelpText = "Input directory to be processed.")]
        public string OutputFolder { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output path of feed xml.")]
        public string FeedXML { get; set; }

        [Option('e', "ext", DefaultValue = true, HelpText = "Add extension to file.")]
        public bool AddExtension { get; set; }

        [Option('n', "ne", DefaultValue = true, HelpText = "no extension append to file.")]
        public bool NoExtension { get; set; }

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
