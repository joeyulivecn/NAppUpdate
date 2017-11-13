using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NAppUpdate.Framework;
using NAppUpdate.Framework.Sources;
using System.IO;
using NAppUpdate.Framework.Common;

namespace WinFormsProgressSample
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();

			progressBar1.Minimum = 0;
			progressBar1.Maximum = 100;
			progressBar1.Step = 1;

			// UpdateManager initialization
			UpdateManager updManager = UpdateManager.Instance;
			updManager.UpdateFeedReader = new DummyReader();
			updManager.UpdateSource = new MemorySource(string.Empty);
            updManager.Config.DestinationFolder = @"c:\SampleApp";
            updManager.Config.TempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SampleApp\\Updates");
            updManager.Config.BackupFolder = @"c:\SampleApp_bak";
            updManager.ReportProgress += updManager_ReportProgress;
            updManager.ReinstateIfRestarted();

            this.Load += Form1_Load;
		}

        void Form1_Load(object sender, EventArgs e)
        {
            UpdateManager updManager = UpdateManager.Instance;
            if (updManager.State != UpdateManager.UpdateProcessState.NotChecked)
            {
                MessageBox.Show("Update process has already initialized; current state: " + updManager.State.ToString());
                return;
            }

            lblOverview.Text = "BeginCheckForUpdates";

            //updManager.BeginCheckForUpdates(
            //    asyncResult => UpdateManager.Instance.BeginPrepareUpdates(ar2 =>
            //    {
            //        if (updManager.UpdatesAvailable == 0)
            //        {
            //            MessageBox.Show("Your software is up to date");
            //            this.Close();
            //            return;
            //        }

            //        updManager.BeginPrepareUpdates(OnPrepareUpdatesCompleted, null);
            //    },
            //    null),
            //    null);

            try
            {
                lblOverview.Text = "CheckForUpdates";

                // Check for updates - returns true if relevant updates are found (after processing all the tasks and
                // conditions)
                // Throws exceptions in case of bad arguments or unexpected results

                updManager.CheckForUpdates();

            }
            catch (Exception ex)
            {
                if (ex is NAppUpdateException)
                {
                    // This indicates a feed or network error; ex will contain all the info necessary
                    // to deal with that
                }
                else MessageBox.Show(ex.ToString());
                return;
            }

            if (updManager.UpdatesAvailable == 0)
            {
                MessageBox.Show("Your software is up to date");
                return;
            }

            DialogResult dr = MessageBox.Show(string.Format("Updates are available to your software ({0} total). Do you want to download and prepare them now? You can always do this at a later time.", updManager.UpdatesAvailable), "Software updates available", MessageBoxButtons.YesNo);
            lblOverview.Text = "BeginPrepareUpdates";
            if (dr == DialogResult.Yes) updManager.BeginPrepareUpdates(OnPrepareUpdatesCompleted, null);

            //updManager.BeginPrepareUpdates(OnPrepareUpdatesCompleted, null);
        }

        private void OnPrepareUpdatesCompleted(IAsyncResult asyncResult)
        {
            try
            {
                ((UpdateProcessAsyncResult)asyncResult).EndInvoke();
            }
            catch (Exception ex)
            {
                string message = string.Empty;
                if (ex.InnerException != null)
                {
                    message = ex.InnerException.ToString();
                }
                else
                {
                    message = ex.ToString();
                }

                MessageBox.Show(string.Format("Updates preperation failed. Check the feed and try again.{0}{1}", Environment.NewLine, message));
                return;
            }

            // Get a local pointer to the UpdateManager instance
            UpdateManager updManager = UpdateManager.Instance;

            DialogResult dr = MessageBox.Show("Updates are ready to install. Do you wish to install them now?", "Software updates ready", MessageBoxButtons.YesNo);

            if (dr != DialogResult.Yes) return;
            // This is a synchronous method by design, make sure to save all user work before calling
            // it as it might restart your application
            try
            {
                updManager.ApplyUpdates(false, true, false);
                MessageBox.Show("DONE!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error while trying to install software updates{0}{1}", Environment.NewLine, ex));
            }
        }

        void updManager_ReportProgress(UpdateProgressInfo status)
        {
            lblDetails.Invoke(new Action(() => lblDetails.Text = status.Message));
            lblOverview.Invoke(new Action(() => lblOverview.Text = string.Format("Phase: {0}, executing task #{1}: {2}",
                                                           UpdateManager.Instance.State,
                                                           status.TaskId,
                                                           status.TaskDescription
                                                )));

            progressBar1.Invoke(new Action(() =>
            {
                progressBar1.Value = status.Percentage;
            }));
        }
	}
}
