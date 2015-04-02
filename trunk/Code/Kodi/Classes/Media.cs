using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;
using RestSharp;

namespace Kodi.Classes
{
    /// <summary>
    /// An object to keep track of the media found on the server
    /// </summary>
    public class ServerMedia
    {
        #region Properties

        /// <summary>
        /// The movie results for the server
        /// </summary>
        public MediaResult<MovieResult> MovieResult { get; set; }
        /// <summary>
        /// The episodes results for the server
        /// </summary>
        public MediaResult<EpisodesResult> EpisodeResult { get; set; }
        /// <summary>
        /// Flag to indicate if both results have values returned from the API call
        /// </summary>
        public bool HasResults
        {
            get
            {
                if (this.MovieResult != null && this.EpisodeResult!= null)
                {
                    if (this.MovieResult.Result != null && this.EpisodeResult.Result != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        /// <summary>
        /// The object used to lock to json file for writing
        /// </summary>
        private static object lockObject = new object();
        /// <summary>
        /// The object used to lock to json log file for writing
        /// </summary>
        private static object logLockObject = new object();

        #endregion
        #region Constructor

        /// <summary>
        /// The constructor for the class
        /// </summary>
        public ServerMedia()
        { 
        }

        /// <summary>
        /// The constructor for the class
        /// </summary>
        /// <param name="movieResult">The movie results for the server</param>
        /// <param name="episodeResult">The episodes results for the server</param>
        public ServerMedia(MediaResult<MovieResult> movieResult, MediaResult<EpisodesResult> episodeResult)
            : this()
        {
            this.MovieResult = movieResult;
            this.EpisodeResult = episodeResult;
        }

        #endregion
        #region Methods

        /// <summary>
        /// Access the API on the server and write the details of the media to the HDD 
        /// </summary>
        /// <param name="kodiApi">The API class to use to interact with Kodi</param>
        /// <returns>A object containing the movie and episode data from the API</returns>
        public void ReadFromAPIAndSave(KodiApi kodiApi)
        {
            // make sure server is online
            bool serverOnline = Network.PingHost(kodiApi.ServerIP);
            Utilities.Message("Pinging server {0} {1}", kodiApi.ServerIP, (serverOnline) ? "succeeded" : "failed");

            if (serverOnline)
            {
                kodiApi.SetIsOnline();
                if (kodiApi.IsOnline)
                {
                    Utilities.Message("Reading media on {0} and saving to file", kodiApi.Name);
                
                    // get the media data from the API
                    this.MovieResult = kodiApi.GetResults<MovieResult>();
                    this.EpisodeResult = kodiApi.GetResults<EpisodesResult>();

                    // make sure API requests worked
                    if (this.HasResults)
                    {
                        Utilities.Message(1, "Found {0} movie(s)", this.MovieResult.Result.Count);
                        Utilities.Message(2, "{0} watched", this.MovieResult.Result.CountWatched);
                        Utilities.Message(2, "{0} resumable", this.MovieResult.Result.CountResumable);
                        Utilities.Message(1, "Found {0} episode(s)", this.EpisodeResult.Result.Count);
                        Utilities.Message(2, "{0} watched", this.EpisodeResult.Result.CountWatched);
                        Utilities.Message(2, "{0} resumable", this.EpisodeResult.Result.CountResumable);

                        // write the media to the HDD
                        ServerMedia serverMedia = new ServerMedia(this.MovieResult, this.EpisodeResult);
                        
                        string filePath = serverMedia.FilePath(kodiApi.ServerIP);
                        string directoryPath = new FileInfo(filePath).Directory.FullName;

                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                            Utilities.Message(1, "Directory {0} created to store media data", directoryPath);
                        }
                        serverMedia.SaveToFile(kodiApi.ServerIP);

                        Utilities.Message(1, "Saved media on {0} to {1}", kodiApi.Name, filePath);
                    }
                    else
                    {
                        Utilities.Message("API request to {0} failed using URL {1}", kodiApi.Name, kodiApi.ServerAPIURL);
                    }
                }
                else
                {
                    Utilities.Message("{0} API is offline", kodiApi.Name);
                }
            }            
                        
            Console.WriteLine("");
        }

        /// <summary>
        /// Read the previously saved media results to from the HDD
        /// </summary>
        /// <param name="kodiApi">The object containing the API details</param>
        /// <returns>The list of media files</returns>
        public void ReadFromHistoricFile(KodiApi kodiApi)
        {
            if (!kodiApi.IsOnline)
            {
                // read the previously saved file media data
                Utilities.Message("Loading {0}'s previously saved media file", kodiApi.Name);
                this.ReadFromFile(kodiApi.ServerIP);

                if (this.HasResults)
                {
                    Utilities.Message(1, "Found {0} movie(s)", this.MovieResult.Result.Count);
                    Utilities.Message(2, "{0} watched", this.MovieResult.Result.CountWatched);
                    Utilities.Message(2, "{0} resumable", this.MovieResult.Result.CountResumable);
                    Utilities.Message(1, "Found {0} episode(s)", this.EpisodeResult.Result.Count);
                    Utilities.Message(2, "{0} watched", this.EpisodeResult.Result.CountWatched);
                    Utilities.Message(2, "{0} resumable", this.EpisodeResult.Result.CountResumable);
                }
                else
                {
                    Utilities.Message(1, "There was an issue serializing the content in {0}", this.FilePath(kodiApi.ServerIP));
                }

                Console.WriteLine("");
            }
        }
        
        /// <summary>
        /// Update the watched media with the data saved in the JSON files.
        /// </summary>
        /// <param name="otherServerMedia">The other server's media information to compare with</param>
        /// <param name="kodiApi">The object containing the API details</param>
        public void CompareAndUpdateAPIData(ServerMedia otherServerMedia, KodiApi kodiApi)
        {
            List<string> processedFiles = new List<string>();
            if (this.HasResults && otherServerMedia.HasResults)
            {
                Utilities.Message("Syncing {0} watched media", kodiApi.Name);
                
                // compare and format data
                List<Movie> otherMovies = otherServerMedia.MovieResult.Result.Movies;
                List<Movie> missingMovies = this.MovieResult.Result.Movies.Except(otherMovies, new KodiMovieEqualityComparer()).ToList();

                foreach (Movie movie in missingMovies) 
                    movie.Update(otherMovies, x => x.Label == movie.Label && x.Year == movie.Year);

                this.MismatchMessage<Movie>(missingMovies, "movie", false);
                                
                // send data to API
                foreach (Movie movie in missingMovies.Where(x => x.IsOutDated))
                    this.UpdateMedia<MovieResult>(kodiApi, movie.MovieId, movie.IsOutDatedResumePosition, movie.IsOutDatedWatched, movie.Resume.Position, movie.Playcount, string.Format("{0} {1}", movie.Label, movie.Year), processedFiles);

                // compare and format data
                List<Episode> otherEpisodes = otherServerMedia.EpisodeResult.Result.Episodes;
                List<Episode> missingEpisodes = this.EpisodeResult.Result.Episodes.Except(otherEpisodes, new KodiEpisodeEqualityComparer()).ToList();

                foreach (Episode episode in missingEpisodes)
                    episode.Update(otherEpisodes, x => x.Label == episode.Label && x.ShowTitle == episode.ShowTitle);

                this.MismatchMessage<Episode>(missingEpisodes, "episode", true);

                // send data to API
                foreach (Episode episode in missingEpisodes.Where(x => x.IsOutDated))
                    this.UpdateMedia<EpisodesResult>(kodiApi, episode.EpisodeId, episode.IsOutDatedResumePosition, episode.IsOutDatedWatched, episode.Resume.Position, episode.Playcount, string.Format("{0} {1}", episode.ShowTitle, episode.Label), processedFiles);
            }
            else if (!this.HasResults)
            {
                Utilities.Message("{0} has no results to sync", kodiApi.Name);
            }
            else if (!otherServerMedia.HasResults)
            {
                Utilities.Message("{0} has no results to sync with", kodiApi.Name);
            }

            // save list of changed values
            if (processedFiles.Count > 0) this.SaveLogToFile(processedFiles);
            Console.WriteLine("");
        }

        /// <summary>
        /// Display a message on the console of the mismatched media found
        /// </summary>
        /// <typeparam name="T">The type of media being sent</typeparam>
        /// <param name="missingMedia">A list of the media found not to match up</param>
        /// <param name="mediaLabel">The display name of the media being processed</param>
        /// <param name="spacePrefix">Flag indicating if there should be a open line before messages</param>
        private void MismatchMessage<T>(List<T> missingMedia, string mediaLabel, bool spacePrefix)
        {
            if (spacePrefix) Console.WriteLine();

            List<Media> missing = missingMedia.OfType<Media>().ToList();
            Utilities.Message(1, "Found {0} out of sync {1}(s)", missing.Count(x => x.IsOutDated == true), mediaLabel);
            Utilities.Message(2, "{0} watched mismatch(es)", missing.Count(x => x.IsOutDatedWatched == true));
            Utilities.Message(2, "{0} resume mismatch(es) ", missing.Count(x => x.IsOutDatedResumePosition == true));
            
            Console.WriteLine();
        }

        /// <summary>
        /// Send the media to the API to update
        /// </summary>
        /// <typeparam name="T">The type of media being sent</typeparam>
        /// <param name="kodiApi">The API object that is going the updating</param>
        /// <param name="mediaId">The id of the media being updated</param>
        /// <param name="resumePositionChanged">Flag indicating if the resume position changed</param>
        /// <param name="watchChanged">Flag indicating if the play count changed</param>
        /// <param name="resumeValue">The new resume value</param>
        /// <param name="watchedValue">The new watched value</param>
        /// <param name="mediaLabel">The display name of the media being updated</param>
        /// <param name="processedFiles">A list keeping track of updated media</param>
        private void UpdateMedia<T>(KodiApi kodiApi, int mediaId, bool resumePositionChanged, bool watchChanged, int resumeValue, int watchedValue, string mediaLabel, List<string> processedFiles)
        {
            string message = "";
            string changes = "";

            switch (kodiApi.UpdateMedia<T>(mediaId, resumePositionChanged, watchChanged, resumeValue, watchedValue))
            {
                case true:
                    message = string.Format("{0} updated", mediaLabel);
                    Utilities.Message(2, message);
                    break;
                default:
                    message = string.Format("Fail to update {0}, attempted to", mediaLabel);
                    Utilities.Message(2, message);
                    break;
            }

            if (watchChanged) changes += "Set to watched";
            if (watchChanged && resumePositionChanged) changes += ", ";
            if (resumePositionChanged) changes += string.Format("Set resume pos to {0}", resumeValue);
            Utilities.Message(3, changes);

            processedFiles.Add(string.Format("[{0}]{1}{2}{3}", DateTime.Now.ToString("yyyyMMdd HH:mm:ss"), message, Environment.NewLine, changes));
        }

        /// <summary>
        /// Save the watched json to the hdd
        /// </summary>
        /// <param name="serverIp">The server IP address used for the filename</param>
        private void SaveToFile(string serverIp)
        {
            string fileListJson = SimpleJson.SerializeObject(this);
            lock (lockObject)
            {
                using (StreamWriter sw = new StreamWriter(this.FilePath(serverIp), false))
                {
                    sw.Write(fileListJson);
                }
            }
        }

        /// <summary>
        /// Save the log to file
        /// </summary>
        /// <param name="logList">The log list</param>
        private void SaveLogToFile(List<string> logList)
        {
            string nameFormat = "yyyyMMdd";
            string logListJson = SimpleJson.SerializeObject(logList);
            lock (logLockObject)
            {
                using (StreamWriter sw = new StreamWriter(this.FilePath(string.Format("Log_{0}", DateTime.Now.ToString(nameFormat))), true))
                {
                    sw.Write(logListJson);
                }

                // housekeeping (running every day so shouldnt miss files)
                string oldFileName = string.Format("Log_{0}", DateTime.Now.AddMonths(-1).ToString(nameFormat));
                if (File.Exists(this.FilePath(oldFileName))) {
                    File.Delete(this.FilePath(oldFileName));
                }
            }
        }

        /// <summary>
        /// Read the watched json from the hdd
        /// </summary>
        /// <param name="serverIp">The server IP address used for he filename</param>
        private void ReadFromFile(string serverIp)
        {
            string listJson = string.Empty;
            if (File.Exists(this.FilePath(serverIp)))
            {
                using (StreamReader sr = new StreamReader(this.FilePath(serverIp)))
                {
                    listJson = sr.ReadToEnd();
                }

                ServerMedia watched = SimpleJson.DeserializeObject<ServerMedia>(listJson);
                if (watched != null)
                {
                    this.MovieResult = watched.MovieResult;
                    this.EpisodeResult = watched.EpisodeResult;
                }
            }
        }

        /// <summary>
        /// The file path to the media file on the HDD
        /// </summary>
        /// <param name="serverIp">The server IP address used for he filename</param>
        public string FilePath(string serverIp)
        {
            string fileName = string.Format("{0}.json", serverIp.Replace('.', '_'));
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rocco Smit", "Kodi", fileName);
        }

        #endregion
    }

    /// <summary>
    /// The general structure of the JSON that is returned from the Kodi
    /// API for watched media requests
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MediaResult<T>
    {
        #region Properties

        public string Id { get; set; }
        public string JsonRPC { get; set; }
        public T Result { get; set; }
        /// <summary>
        /// Flag to indicate if a result was returned from the API call
        /// </summary>
        public bool HasResult
        {
            get
            {
                if (this.Result != null)
                {
                    return true;
                }

                return false;
            }
        }
                
        #endregion
        #region Constructor

        /// <summary>
        /// The constructor for the class
        /// </summary>
        public MediaResult()
        {
        }

        #endregion
        #region Methods
                
        #endregion
    }
    /// <summary>
    /// The movie specific structure of the JSON that is returned from 
    /// the Kodi API for movie requests
    /// </summary>
    public class MovieResult
    {
        #region Properties

        public object Limits { get; set; }
        public List<Movie> Movies { get; set; }
        public int Count
        {
            get
            {
                if (this.Movies != null)
                {
                    return this.Movies.Count;
                }

                return 0;
            }
        }
        public int CountWatched
        {
            get
            {
                if (this.Movies != null)
                {
                    return this.Movies.Where(x => x.Playcount > 0).Count();
                }

                return 0;
            }
        }
        public int CountResumable
        {
            get
            {
                if (this.Movies != null)
                {
                    return this.Movies.Where(x => x.Resume.Position > 0).Count();
                }

                return 0;
            }
        }

        #endregion
        #region Constructor

        /// <summary>
        /// The constructor for the class
        /// </summary>
        public MovieResult()
        {
            this.Movies = new List<Movie>();
        }

        #endregion
    }
    /// <summary>
    /// The episode specific structure of the JSON that is returned from 
    /// the Kodi API for episode requests
    /// </summary>
    public class EpisodesResult
    {
        #region Properties

        public object Limits { get; set; }
        public List<Episode> Episodes { get; set; }
        public int Count
        {
            get
            {
                if (this.Episodes != null)
                {
                    return this.Episodes.Count;
                }

                return 0;
            }
        }
        public int CountWatched
        {
            get
            {
                if (this.Episodes != null)
                {
                    return this.Episodes.Where(x => x.Playcount > 0).Count();
                }

                return 0;
            }
        }
        public int CountResumable
        {
            get
            {
                if (this.Episodes != null)
                {
                    return this.Episodes.Where(x => x.Resume.Position > 0).Count();
                }

                return 0;
            }
        }

        #endregion
        #region Constructor

        /// <summary>
        /// The constructor for the class
        /// </summary>
        public EpisodesResult()
        {
            this.Episodes = new List<Episode>();
        }

        #endregion
    }
    /// <summary>
    /// The result that gets returned when updating the server with the API
    /// </summary>
    public class UpdateResult
    {
        public string Id { get; set; }
        public string JsonRPC { get; set; }
        public string Result { get; set; }
    }

    /// <summary>
    /// The movie object retuend from the API
    /// </summary>
    public class Movie : Media
    {
        #region Properties

        public int MovieId { get; set; } 
        public string IMDBNumber { get; set; }
        public string Year { get; set; }

        #endregion
        #region Constructor

        /// <summary>
        /// The constructor for the class
        /// </summary>
        public Movie()
            : base()
        {
        }

        #endregion
        #region Methods

        #endregion
    }
    /// <summary>
    /// The episodes object retuend from the API
    /// </summary>
    public class Episode : Media
    {
        #region Properties

        public int EpisodeId { get; set; }
        public string ShowTitle { get; set; }

        #endregion
        #region Constructor

        /// <summary>
        /// The constructor for the class
        /// </summary>
        public Episode()
            : base()
        {
        }

        #endregion
        #region Methods
        
        #endregion
    }
    /// <summary>
    /// A base class for the media
    /// </summary>
    public class Media
    {
        #region Properties

        public string Label { set; get; }
        public Resume Resume { get; set; }
        public int Playcount { get; set; }
        public bool IsOutDated 
        { 
            get 
            {
                return this.IsOutDatedWatched == true || this.IsOutDatedResumePosition == true;
            } 
        }
        public bool IsOutDatedWatched { get; set; }
        public bool IsOutDatedResumePosition { get; set; }

        #endregion
        #region Constructor

        /// <summary>
        /// The constructor for the class
        /// </summary>
        public Media()
        {
            this.Resume = new Resume();
        }

        #endregion
        #region Methods

        /// <summary>
        /// Compare the movie to its equivalent in another list and set its values if 
        /// "older" and remove from list if "newer"
        /// </summary>
        /// <param name="compareToList">The list to look in to find the movie to compare to</param>
        public void Update<T>(List<T> compareToList, Func<T, bool> identifyFunc)
        {
            // find other server's matching media
            Media match = (Media)(object)compareToList.FirstOrDefault(identifyFunc);
            if (match != null)
            {
                // compare values and set if needed
                int maxPlayCount = Math.Max(this.Playcount, match.Playcount);
                int maxResumePosition = Math.Max(this.Resume.Position, match.Resume.Position);

                if (this.Playcount != maxPlayCount)
                {
                    this.Playcount = maxPlayCount;
                    this.IsOutDatedWatched = true;
                }
                if (this.Resume.Position != maxResumePosition)
                {
                    this.Resume.Position = maxResumePosition;
                    this.IsOutDatedResumePosition = true;
                }
            }
        }

        #endregion
    }
    /// <summary>
    /// The resume object used in movies and episodes
    /// </summary>
    public class Resume
    {
        #region Properties

        public int Position { get; set; }
        public int Total { get; set; }

        #endregion
        #region Constructor

        /// <summary>
        /// The constructor for the class
        /// </summary>
        public Resume()
        {
        }

        #endregion
    }
}