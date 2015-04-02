using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;

namespace Kodi.Classes
{
    public class Sync
    {
        #region Properties
        
        #endregion

        #region Constructor
        #endregion

        #region Methods

        public bool UpdateLibraries()
        {
            // set up API objects
            KodiApi kodiApi = new KodiApi(Settings.Server1Name, Settings.Server1IP, Settings.Server1APIURL, Settings.Server1APIUsername, Settings.Server1APIPassword);
            KodiApi openElecApi = new KodiApi(Settings.Server2Name, Settings.Server2IP, Settings.Server2APIURL, Settings.Server2APIUsername, Settings.Server2APIPassword);

            // read api data and save to file
            ServerMedia kodiMedia = new ServerMedia();
            kodiMedia.ReadFromAPIAndSave(kodiApi);

            ServerMedia openelecMedia = new ServerMedia();
            openelecMedia.ReadFromAPIAndSave(openElecApi);

            // load historical unnwatched API data for inactive APIs
            if (!kodiApi.IsOnline && openElecApi.IsOnline) kodiMedia.ReadFromHistoricFile(kodiApi);
            if (kodiApi.IsOnline && !openElecApi.IsOnline) openelecMedia.ReadFromHistoricFile(openElecApi);

            // updated libraries based on active APIs
            if (kodiApi.IsOnline) kodiMedia.CompareAndUpdateAPIData(openelecMedia, kodiApi);
            if (openElecApi.IsOnline) openelecMedia.CompareAndUpdateAPIData(kodiMedia, openElecApi);
            

            // delay the console window close
            this.DelayConsoleClose(Settings.SecondsBeforeClose);
            return false;
        }

        /// <summary>
        /// Count down the given amount of seconds before the window closes
        /// </summary>
        /// <param name="seconds">The amount of seconds to wait before closing</param>
        private void DelayConsoleClose(int seconds)
        {
            DateTime closeTime = DateTime.Now.AddSeconds(seconds);
            TimeSpan timeLeft = new TimeSpan();

            while (closeTime > DateTime.Now)
            {
                timeLeft = closeTime.Subtract(DateTime.Now);
                Console.Write("\rWindow closing in {0}s     ", timeLeft.Seconds);
                Thread.Sleep(1000);
            }
        }

        #endregion 
    }
}
