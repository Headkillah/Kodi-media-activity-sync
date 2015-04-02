using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Kodi.Classes
{
    public class KodiApi
    {
        #region Properties

        public string Name { get; private set; }
        public string ServerIP { get; private set; }
        public string ServerAPIURL { get; private set; }
        public string ServerUsername { get; private set; }
        public bool IsOnline { get; set; }

        private MediaType _MediaType = MediaType.Movie;
        private string _ServerPassword;        

        #endregion

        #region Constructors

        /// <summary>
        /// The constructor for the class
        /// </summary>
        /// <param name="name">The name of the server used for identification</param>
        /// <param name="serverIP">The server IP address of the server the API is run on</param>
        /// <param name="serverAPIURL">The URL for the server's API</param>
        /// <param name="serverUsername">The username for the server's API</param>
        /// <param name="serverPassword">The password for the server's API</param>
        public KodiApi(string name, string serverIP, string serverAPIURL, string serverUsername, string serverPassword)
        {
            this.Name = name;
            this.ServerIP = serverIP;
            this.ServerAPIURL = serverAPIURL;
            this.ServerUsername = serverUsername;
            this._ServerPassword = serverPassword;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Retuns the API search query based on the media type set
        /// </summary>
        /// <returns>The API query</returns>
        public string GetAPISearchQuery()
        {
            if (this._MediaType == MediaType.Movie)
            {
                return "{\"jsonrpc\": \"2.0\", \"method\": \"VideoLibrary.GetMovies\", \"params\": { \"properties\" : [\"year\", \"resume\", \"imdbnumber\", \"playcount\"], \"sort\": { \"order\": \"ascending\", \"method\": \"label\", \"ignorearticle\": true } }, \"id\": \"libMovies\"}";
            }

            return "{\"jsonrpc\": \"2.0\", \"method\": \"VideoLibrary.GetEpisodes\", \"params\": { \"properties\": [\"title\", \"showtitle\", \"resume\", \"uniqueid\", \"playcount\"], \"sort\": { \"order\": \"ascending\", \"method\": \"label\" } }, \"id\": \"libEpisodes\"}";
        }
       
        /// <summary>
        /// Retuns the API update query based on the media type set
        /// </summary>
        /// <param name="mediaId">The media identifier in the library</param>
        /// <param name="resumePositionChanged">Flag indicating if the resume position changed</param>
        /// <param name="watchChanged">Flag indicating if the play count changed</param>
        /// <param name="resumeValue">The new resume value</param>
        /// <param name="watchedValue">The new watched value</param>
        /// <returns>The API query</returns>
        public string APIUpdateQuery(int mediaId, bool resumePositionChanged, bool watchChanged, int resumeValue, int watchedValue)
        {
            string details = "SetEpisodeDetails";
            string identifier = "episodeid";
            string library = "libEpisodes";
            string resume = string.Empty;
            string watched = string.Empty;

            if (resumePositionChanged) resume = string.Format("\"resume\": {{ \"position\": {0} }}", resumeValue);
            if (watchChanged) watched = string.Format("\"playcount\": {0}", watchedValue);
            if (watchChanged && resumePositionChanged) watched = string.Format("{0}, ", watched);

            if (this._MediaType == MediaType.Movie)
            {
                details = "SetMovieDetails";
                identifier = "movieid";
                library = "libMovies";
            }

            return string.Format("{{\"jsonrpc\": \"2.0\", \"method\": \"VideoLibrary.{0}\", \"params\": {{ \"{1}\": {2}, {3}{4} }}, \"id\": \"{5}\"}}", details, identifier, mediaId, watched, resume, library);
        }

        /// <summary>
        /// Checks the API is ative or not
        /// </summary>
        public void SetIsOnline()
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(this.ServerAPIURL) as HttpWebRequest;
                request.Method = "HEAD";
                request.Timeout = 5000;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                this.IsOnline = (response.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                this.IsOnline = false;
            }

            this.IsOnline = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">The object that corresponds to the media type being collected</typeparam>
        /// <returns>The result of the API call</returns>
        public MediaResult<T> GetResults<T>()
        {
            // set the media type the query is about
            this._MediaType = (typeof(T) == typeof(MovieResult)) ? MediaType.Movie : MediaType.Episode;

            // set up the rest client to query
            RestClient client = new RestClient(this.ServerAPIURL);
            client.Authenticator = new HttpBasicAuthenticator(this.ServerUsername, this._ServerPassword);
            client.Timeout = 5000;            

            // set up the request to send to client
            RestRequest request = new RestRequest(Method.POST);
            request.AddJsonBody(SimpleJson.DeserializeObject(this.GetAPISearchQuery()));

            // query the client
            RestResponse<MediaResult<T>> response = (RestResponse<MediaResult<T>>)client.Execute<MediaResult<T>>(request);
            if (response.Data != null)
            {
                this.IsOnline = true;
            }

            return response.Data;
        }

        /// <summary>
        /// Update the server media library
        /// </summary>
        /// <typeparam name="T">The object that corresponds to the media type being updated</typeparam>
        /// <param name="mediaId">The library id of the item being updated</param>
        /// <param name="resumePositionChanged">Flag indicating if the resume position changed</param>
        /// <param name="watchChanged">Flag indicating if the play count changed</param>
        /// <param name="resumeValue">The new resume value</param>
        /// <param name="watchedValue">The new watched value</param>
        /// <returns>The result of the API call</returns>
        public bool UpdateMedia<T>(int mediaId, bool resumePositionChanged, bool watchChanged, int resumeValue, int watchedValue)
        {
            // set the media type the query is about
            this._MediaType = (typeof(T) == typeof(MovieResult)) ? MediaType.Movie : MediaType.Episode;

            // set up the rest client to query
            RestClient client = new RestClient(this.ServerAPIURL);
            client.Authenticator = new HttpBasicAuthenticator(this.ServerUsername, this._ServerPassword);

            // set up the request to send to client
            RestRequest request = new RestRequest(Method.POST);
            request.AddJsonBody(SimpleJson.DeserializeObject(this.APIUpdateQuery(mediaId, resumePositionChanged, watchChanged, resumeValue, watchedValue)));

            // query the client
            RestResponse<UpdateResult> response = (RestResponse<UpdateResult>)client.Execute<UpdateResult>(request);
            if (response != null && response.Data != null && response.Data.Result == "OK")
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}