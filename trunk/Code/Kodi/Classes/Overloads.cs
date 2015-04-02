using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kodi.Classes
{
    public class KodiMovieEqualityComparer : IEqualityComparer<Movie>
    {
        #region IEqualityComparer<Movie> Members

        /// <summary>
        /// Override the equals opperator to find items that are equal or greater than item
        /// </summary>
        /// <param name="x">Movie to compare</param>
        /// <param name="y">Movie to compare</param>
        /// <returns></returns>
        public bool Equals(Movie x, Movie y)
        {
            return x.IMDBNumber == y.IMDBNumber &&
                   x.Playcount == y.Playcount &&
                   x.Resume.Position == y.Resume.Position;
                   
        }
        /// <summary>
        /// Override the hash comparison
        /// </summary>
        /// <param name="obj">The movie to get hash from</param>
        /// <returns></returns>
        public int GetHashCode(Movie obj)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + obj.Label.GetHashCode();
                hash = hash * 23 + obj.IMDBNumber.GetHashCode();
                hash = hash * 23 + obj.Playcount.GetHashCode();
                hash = hash * 23 + obj.Resume.Position.GetHashCode();

                return hash;
            }
        }

        #endregion
    }

    public class KodiEpisodeEqualityComparer : IEqualityComparer<Episode>
    {
        #region IEqualityComparer<Episode> Members

        /// <summary>
        /// Override the equals opperator
        /// </summary>
        /// <param name="x">Episode to compare</param>
        /// <param name="y">Episode to compare</param>
        /// <returns></returns>
        public bool Equals(Episode x, Episode y)
        {
            // not using unique id, found differences between servers
            return x.Label == y.Label && 
                   x.ShowTitle == y.ShowTitle &&
                   x.Playcount == y.Playcount &&
                   x.Resume.Position == y.Resume.Position;
        }
        /// <summary>
        /// Override the hash comparison
        /// </summary>
        /// <param name="obj">The episode to get hash from</param>
        /// <returns></returns>
        public int GetHashCode(Episode obj)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + obj.Label.GetHashCode();
                hash = hash * 23 + obj.ShowTitle.GetHashCode();
                hash = hash * 23 + obj.Playcount.GetHashCode();
                hash = hash * 23 + obj.Resume.Position.GetHashCode();

                return hash;
            }
        }

        #endregion
    }

}
