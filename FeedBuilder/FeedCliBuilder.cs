using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace FeedBuilder
{
    /// <summary>
    /// for console only
    /// </summary>
    public class FeedCliBuilder
    {
        private string ConfigFileName { get; set; }
        private FeedBuildConfiguration config = null;
        private Dictionary<string, FileInfoEx> fileList = new Dictionary<string,FileInfoEx>();

        public void Run(Options options)
        {
            ConfigFileName = options.ConfigFileName;

            var serializer = new XmlSerializer(typeof(FeedBuildConfiguration));
            using (Stream stream = new FileStream(ConfigFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                config = serializer.Deserialize(stream) as FeedBuildConfiguration;
            }

            config.OutputFolder = options.OutputFolder;
            config.FeedXML = options.FeedXML;

            ReadFiles();
            Build(options);
        }

        private void Build(Options options)
        {
            Console.WriteLine("Building NAppUpdater feed '{0}'", config.BaseURL.Trim());

            // If the target folder doesn't exist, create a path to it
            string dest = config.FeedXML.Trim();
            var destDir = Directory.GetParent(new FileInfo(dest).FullName);
            if (!Directory.Exists(destDir.FullName)) Directory.CreateDirectory(destDir.FullName);

            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "utf-8", null);

            doc.AppendChild(dec);
            XmlElement feed = doc.CreateElement("Feed");
            if (!string.IsNullOrEmpty(config.BaseURL.Trim())) feed.SetAttribute("BaseUrl", config.BaseURL.Trim());
            doc.AppendChild(feed);

            XmlElement tasks = doc.CreateElement("Tasks");

            Console.WriteLine("Processing feed items");
            int itemsCopied = 0;
            int itemsCleaned = 0;
            int itemsSkipped = 0;
            int itemsFailed = 0;
            int itemsMissingConditions = 0;
            foreach (var thisItem in fileList)
            {
                string destFile = "";
                string folder = "";
                string filename = "";
                try
                {
                    folder = Path.GetDirectoryName(config.FeedXML);
                    filename = thisItem.Key;
                    if (folder != null) destFile = Path.Combine(folder, filename);
                }
                catch { }
                if (destFile == "" || folder == "" || filename == "")
                {
                    string msg = string.Format("The file could not be pathed:\nFolder:'{0}'\nFile:{1}", folder, filename);
                    Console.WriteLine(msg);
                    continue;
                }

                var fileInfoEx = thisItem.Value;
                XmlElement task = doc.CreateElement("FileUpdateTask");
                task.SetAttribute("localPath", fileInfoEx.RelativeName);
                if (!string.IsNullOrEmpty(config.AddExtension))
                    task.SetAttribute("updateTo", fileInfoEx.RelativeName + "." + config.AddExtension.Trim());
                // generate FileUpdateTask metadata items
                task.SetAttribute("lastModified", fileInfoEx.FileInfo.LastWriteTime.ToFileTime().ToString(CultureInfo.InvariantCulture));
                task.SetAttribute("fileSize", fileInfoEx.FileInfo.Length.ToString(CultureInfo.InvariantCulture));
                if (!string.IsNullOrEmpty(fileInfoEx.FileVersion)) task.SetAttribute("version", fileInfoEx.FileVersion);

                XmlElement conds = doc.CreateElement("Conditions");
                XmlElement cond;

                //File Exists
                cond = doc.CreateElement("FileExistsCondition");
                cond.SetAttribute("type", "or-not");
                conds.AppendChild(cond);

                //Version
                if (config.CompareVersion && !string.IsNullOrEmpty(fileInfoEx.FileVersion))
                {
                    cond = doc.CreateElement("FileVersionCondition");
                    cond.SetAttribute("type", "or");
                    cond.SetAttribute("what", "below");
                    cond.SetAttribute("version", fileInfoEx.FileVersion);
                    conds.AppendChild(cond);
                }

                //Size
                if (config.CompareSize)
                {
                    cond = doc.CreateElement("FileSizeCondition");
                    cond.SetAttribute("type", "or-not");
                    cond.SetAttribute("what", "is");
                    cond.SetAttribute("size", fileInfoEx.FileInfo.Length.ToString(CultureInfo.InvariantCulture));
                    conds.AppendChild(cond);
                }

                //Date
                if (config.CompareDate)
                {
                    cond = doc.CreateElement("FileDateCondition");
                    cond.SetAttribute("type", "or");
                    cond.SetAttribute("what", "older");
                    // local timestamp, not UTC
                    cond.SetAttribute("timestamp", fileInfoEx.FileInfo.LastWriteTime.ToFileTime().ToString(CultureInfo.InvariantCulture));
                    conds.AppendChild(cond);
                }

                //Hash
                if (config.CompareHash)
                {
                    cond = doc.CreateElement("FileChecksumCondition");
                    cond.SetAttribute("type", "or-not");
                    cond.SetAttribute("checksumType", "sha256");
                    cond.SetAttribute("checksum", fileInfoEx.Hash);
                    conds.AppendChild(cond);
                }

                if (conds.ChildNodes.Count == 0) itemsMissingConditions++;
                task.AppendChild(conds);
                tasks.AppendChild(task);

                // rename file with AddExtension
                if (!config.CopyFiles && 
                    options.AddExtension && 
                    !string.IsNullOrEmpty(config.AddExtension))
                {
                    var fullName = fileInfoEx.FileInfo.FullName;
                    File.Move(fullName, string.Format("{0}.{1}", fullName, config.AddExtension));
                }

                // copy files
                if (config.CopyFiles)
                {
                    if (CopyFile(fileInfoEx.FileInfo.FullName, destFile)) itemsCopied++;
                    else itemsFailed++;
                }
            }

            feed.AppendChild(tasks);
            doc.Save(config.FeedXML.Trim());

            // open the outputs folder if we're running from the GUI or 
            // we have an explicit command line option to do so
            Console.WriteLine("Done building feed.");
            if (itemsCopied > 0) Console.WriteLine("{0,5} items copied", itemsCopied);
            if (itemsCleaned > 0) Console.WriteLine("{0,5} items cleaned", itemsCleaned);
            if (itemsSkipped > 0) Console.WriteLine("{0,5} items skipped", itemsSkipped);
            if (itemsFailed > 0) Console.WriteLine("{0,5} items failed", itemsFailed);
            if (itemsMissingConditions > 0) Console.WriteLine("{0,5} items without any conditions", itemsMissingConditions);
        }

        private bool CopyFile(string sourceFile, string destFile)
        {
            // If the target folder doesn't exist, create the path to it
            var fi = new FileInfo(destFile);
            var d = Directory.GetParent(fi.FullName);
            if (!Directory.Exists(d.FullName)) CreateDirectoryPath(d.FullName);
            if (!string.IsNullOrEmpty(config.AddExtension.Trim())) destFile += "." + config.AddExtension.Trim();
            // Copy with delayed retry
            int retries = 3;
            while (retries > 0)
            {
                try
                {
                    if (File.Exists(destFile)) File.Delete(destFile);
                    File.Copy(sourceFile, destFile);
                    retries = 0; // success
                    return true;
                }
                catch (IOException)
                {
                    // Failed... let's try sleeping a bit (slow disk maybe)
                    if (retries-- > 0) Thread.Sleep(200);
                }
                catch (UnauthorizedAccessException)
                {
                    // same handling as IOException
                    if (retries-- > 0) Thread.Sleep(200);
                }
            }
            return false;
        }

        private void CreateDirectoryPath(string directoryPath)
        {
            // Create the folder/path if it doesn't exist, with delayed retry
            int retries = 3;
            while (retries > 0 && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                if (retries-- < 3) Thread.Sleep(200);
            }
        }

        private void ReadFiles()
        {
            string outputDir = config.OutputFolder.Trim();

            if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir))
            {
                return;
            }

            if (!outputDir.EndsWith("\\"))
            {
                outputDir += "\\";
            }

            FileSystemEnumerator enumerator = new FileSystemEnumerator(outputDir, config.IncludeFileTypes, true);
            foreach (FileInfo fi in enumerator.Matches())
            {
                string filePath = fi.FullName;

                FileInfoEx fileInfo = new FileInfoEx(filePath, outputDir.Length);
                fileList.Add(fileInfo.RelativeName, fileInfo);
            }
        }
    }
}
