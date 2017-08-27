using System;
using System.Collections.Generic;
using System.Text;

namespace Hpdi.Vss2Git
{

    /// <summary>A parser for picking out the pieces of each argument from the command line
    /// and storing them in proper structures/lists</summary>
    class CommandLineParser
    {

        /// <summary>Default constructor</summary>
        public CommandLineParser ()
        {
            AllParameters = new List<CommandLineParameter> ();
            Values = new List<string> ();
            Parameters = new Dictionary<string, CommandLineParameter> ();
            HelpRequested = false;
        }

        /// <summary>Get the list of </summary>
        public List<CommandLineParameter> AllParameters { get; private set; }

        /// <summary>Get the list of individual values supplied, that didn't have
        /// a parameter flag</summary>
        public List<string> Values { get; private set; }

        /// <summary>Get the dictionary of all the command line parameters parsed
        /// (actually had a flag)</summary>
        public Dictionary<string, CommandLineParameter> Parameters { get; private set; }

        /// <summary>Get/Set the flag that help was requested in the form of a parsed
        /// command line such as --help, -?, or /?</summary>
        public bool HelpRequested { get; set; }

        /// <summary>Reset all the properties to their defaults</summary>
        public void Clear ()
        {
            AllParameters.Clear ();
            Values.Clear ();
            Parameters.Clear ();
            HelpRequested = false;
        }

        /// <summary>Generate the parameters from an array of command line arguments</summary>
        /// <param name="Args">a string[] of command line arguments</param>
        public void Parse ( string[] Args )
        {
            Clear ();
            foreach ( string arg in Args )
            {
                CommandLineParameter cp = new CommandLineParameter ( arg );
                if ( cp.IsFlagged )
                {
                    if ( !Parameters.ContainsKey ( cp.Name ) )
                    {
                        Parameters.Add ( cp.Name, cp );
                    }
                    if ( cp.Name.Equals ( "?") || cp.Name.Equals ( "help" ) )
                    {
                        HelpRequested = true;
                    }
                }
                else
                {
                    Values.Add ( cp.Value );
                }
                AllParameters.Add ( cp );
            }

        }

    }

}
