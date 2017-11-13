
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FeedBuilder
{
    [Serializable]
    public class FeedBuildConfiguration
    {
        public string FeedXML { get; set; }
        public string OutputFolder { get; set; }
        public bool CleanUp { get; set; }
        public string BaseURL { get; set; }
        public bool CompareHash { get; set; }
        public bool CompareDate { get; set; }
        public bool CompareSize { get; set; }
        public bool CompareVersion { get; set; }
        public string IncludeFileTypes { get; set; }
        public bool CopyFiles { get; set; }
        public string AddExtension { get; set; }
    }
}
