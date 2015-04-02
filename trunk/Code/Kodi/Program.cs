using Kodi.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kodi
{
    class Program
    {
        static void Main(string[] args)
        {
            Sync sync = new Sync();
            sync.UpdateLibraries();
        }
    }
}
