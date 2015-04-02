using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kodi.Classes
{
    public class Settings
    {
        /// <summary>
        /// The name used to identify the 1st server
        /// </summary>
        public static string Server1Name
        {
            get
            {
                return ConfigurationManager.AppSettings["Server1Name"] ?? string.Empty;
            }
        }
        /// <summary>
        /// The IP address to the 1st server
        /// </summary>
        public static string Server1IP
        {
            get
            {
                return ConfigurationManager.AppSettings["Server1IP"] ?? string.Empty;
            }
        }
        /// <summary>
        /// The API url and port to the 1st server
        /// </summary>
        public static string Server1APIURL
        {
            get
            {
                return ConfigurationManager.AppSettings["Server1APIURL"];
            }
        }
        /// <summary>
        /// The API username to access the 1st server
        /// </summary>
        public static string Server1APIUsername
        {
            get
            {
                return ConfigurationManager.AppSettings["Server1APIUsername"] ?? string.Empty;
            }
        }
        /// <summary>
        /// The API password to access the 1st server
        /// </summary>
        public static string Server1APIPassword
        {
            get
            {
                return ConfigurationManager.AppSettings["Server1APIPassword"] ?? string.Empty;
            }
        }
        // <summary>
        /// The name used to identify the 2nd server
        /// </summary>
        public static string Server2Name
        {
            get
            {
                return ConfigurationManager.AppSettings["Server2Name"] ?? string.Empty;
            }
        }
        /// <summary>
        /// The IP address to the 2nd server
        /// </summary>
        public static string Server2IP
        {
            get
            {
                return ConfigurationManager.AppSettings["Server2IP"] ?? string.Empty;
            }
        }
        /// <summary>
        /// The API url and port to the 2nd server
        /// </summary>
        public static string Server2APIURL
        {
            get
            {
                return ConfigurationManager.AppSettings["Server2APIURL"];
            }
        }
        /// <summary>
        /// The API username to access the 2nd server
        /// </summary>
        public static string Server2APIUsername
        {
            get
            {
                return ConfigurationManager.AppSettings["Server2APIUsername"] ?? string.Empty;
            }
        }
        /// <summary>
        /// The API password to access the 2nd server
        /// </summary>
        public static string Server2APIPassword
        {
            get
            {
                return ConfigurationManager.AppSettings["Server2APIPassword"] ?? string.Empty;
            }
        }
        /// <summary>
        /// The amount of seconds to wait before closing the console window
        /// </summary>
        public static int SecondsBeforeClose
        {
            get
            {
                return int.Parse(ConfigurationManager.AppSettings["SecondsBeforeClose"]);
            }
        }
    }
}