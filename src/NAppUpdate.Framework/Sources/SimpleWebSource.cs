﻿using System;
using NAppUpdate.Framework.Common;
using NAppUpdate.Framework.Utils;
using System.Net;
using System.IO;

namespace NAppUpdate.Framework.Sources
{
    public class SimpleWebSource : IUpdateSource
    {
        public IWebProxy Proxy { get; set; }
        public string FeedUrl { get; set; }

        internal SimpleWebSource()
        {
            Proxy = null;
        }

        public SimpleWebSource(string feedUrl)
        {
            if (string.IsNullOrEmpty(feedUrl))
            {
                throw new ArgumentException("feedUrl is empty");
            }

            FeedUrl = feedUrl;
            Proxy = null;
        }

        #region IUpdateSource Members

        /// <summary>
        /// Speed up web request by trying to use dns to resolve ip address.  If it fails then fail early.
        /// </summary>
        /// <returns></returns>
        private void TryResolvingHost()
        {
            var uri = new Uri(FeedUrl);
            try
            {
                Dns.GetHostEntry(uri.Host);
            }
            catch (Exception e)
            {
                // eat this exception since sometimes the network works but this method failed...
                UpdateManager.Instance.Logger.Log(string.Format("Failed to resolve {0}. Check your connectivity. {1}{2}", uri.Host),
                    WebExceptionStatus.ConnectFailure, Environment.NewLine, e);
            }
        }

        public string GetUpdatesFeed()
        {
            // Comment out TryResolvingHost(), because it does not work for some environment...
            //TryResolvingHost();

            var data = string.Empty;

            using (var wc = new WebClient())
            {
                data = wc.DownloadString(FeedUrl);
            }

            return data;
        }

        public bool GetData(string url, string baseUrl, Action<UpdateProgressInfo> onProgress, ref string tempLocation)
        {
            FileDownloader fd;
            // A baseUrl of http://testserver/somefolder with a file linklibrary.dll was resulting in a webrequest to http://testserver/linklibrary
            // The trailing slash is required for the Uri parser to resolve correctly.
            if (!string.IsNullOrEmpty(baseUrl) && !baseUrl.EndsWith("/")) baseUrl += "/";
            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                fd = new FileDownloader(url);
            else if (Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute))
                fd = new FileDownloader(new Uri(new Uri(baseUrl, UriKind.Absolute), url));
            else
                fd = string.IsNullOrEmpty(baseUrl) ? new FileDownloader(url) : new FileDownloader(new Uri(new Uri(baseUrl), url));

            fd.Proxy = Proxy;

            if (string.IsNullOrEmpty(tempLocation) || !Directory.Exists(Path.GetDirectoryName(tempLocation)))
                // WATCHOUT!!! Files downloaded to a path specified by GetTempFileName may be deleted on
                // application restart, and as such cannot be relied on for cold updates, only for hot-swaps or
                // files requiring pre-processing
                tempLocation = Path.GetTempFileName();

            return fd.DownloadToFile(tempLocation, onProgress);
        }

        #endregion
    }
}
