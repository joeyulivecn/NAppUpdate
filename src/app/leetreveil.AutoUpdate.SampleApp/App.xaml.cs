﻿using System.Windows;
using leetreveil.AutoUpdate.Core.UpdateCheck;

namespace leetreveil.AutoUpdate.SampleApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            //check for update asyncronously
            //TODO: move the update checking to somewhere after the app has started, i.e main window

            //TODO: make this code asyncronous
            var updateChecker = new UpdateChecker();

            //TODO: fix it so we dont have to download file updates from the internet and just point to a file on disk in the xml file (easier to test)
            if (updateChecker.CheckForUpdate("sampleappupdatefeed.xml"))
            {
                //ask user if he wants to update or not
                new UpdateWindow(updateChecker.Update).Show();
            }
        }
    }
}
