using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kodi.Classes
{
    public class Utilities
    {
        /// <summary>
        /// A helper class to write text to the console screen
        /// </summary>
        /// <param name="message">The message to write</param>
        /// <param name="args">The arguments to replace in the message</param>
        public static void Message(string message, params object[] args)
        {
            Console.WriteLine(string.Format(message, args));
        }
        /// <summary>
        /// A helper class to write text to the console screen
        /// </summary>
        /// <param name="tabs">The amount of indents to add to the front of the string</param>
        /// <param name="message">The message to write</param>
        /// <param name="args">The arguments to replace in the message</param>
        public static void Message(int tabs, string message, params object[] args)
        {
            string indents = string.Empty;
            for (int i = 0; i < tabs - 1; i++)
            {
                indents += "    ";
            }
            indents += "  - ";
            
            Utilities.Message(string.Format("{0}{1}", indents, message), args);
        }
    }
}
