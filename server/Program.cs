using server.Core.Network;
using server.Database;
using server.Forms;
using System.Diagnostics;

namespace server
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();        
            Application.Run(new MainMenu());
        }
    }
}