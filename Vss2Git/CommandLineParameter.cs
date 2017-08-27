using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hpdi.Vss2Git
{

    class CommandLineParameter
    {

        /// <summary>Default constructor</summary>
        public CommandLineParameter ()
        {
            this.Clear ();
        }

        /// <summary>Overloaded to provide initialization using text from a command line</summary>
        /// <param name="CmdText">text of an entry on a command line</param>
        public CommandLineParameter ( string CmdText )
        {
            this.Parse ( CmdText );
        }

        /// <summary>Get/Set the flag used (i.e. "-", "/", "--")</summary>
        public string Flag { get; set; }

        /// <summary>Get/Set the parameter name</summary>
        public string Name { get; set; }

        /// <summary>Get/Set the parameter value used by itself or in association with the name</summary>
        public string Value { get; set; }

        /// <summary>Get the status of a flag being assigned</summary>
        public bool IsFlagged
        {
            get { return !string.IsNullOrEmpty ( Flag ); }
        }

        /// <summary>Overriden to provide the text of the command line parameter</summary>
        /// <returns>Value | Flag + Name | Flag + Name + ':' + Value (double quoted if a space exists)</returns>
        public override string ToString ()
        {
            string ret = "";

            // determine the return value
            if ( string.IsNullOrEmpty ( Flag ) )
            {
                ret = Value;
            }
            else if ( string.IsNullOrEmpty ( Value ) )
            {
                ret = Flag + Name;
            }
            else
            {
                ret = Flag + Name + ":" + Value;
            }

            // double-quote entire parameter if a space exists within it
            if ( ret.Contains ( " " ) )
            {
                ret = "\"" + ret + "\"";
            }

            return ret;

        }

        /// <summary>Rest all the properties to their defaults</summary>
        public void Clear ()
        {
            Flag = "";
            Name = "";
            Value = "";
        }

        /// <summary>Parse a single command line item to pick out the pieces of the parameter</summary>
        /// <param name="CmdText">a single parameter on the command line</param>
        public void Parse ( string CmdText )
        {
            string v;
            int pos;

            this.Clear ();
            if ( string.IsNullOrEmpty ( CmdText ) )
                return;

            // determine if it's just a value, or if it's a flagged parameter
            if ( CmdText.StartsWith ( "--" ) )
            {
                Flag = "--";
                v = CmdText.Substring ( 2 );
            }
            else if ( CmdText.StartsWith ( "-" ) || CmdText.StartsWith ( "/" ) )
            {
                Flag = CmdText.Substring ( 0, 1 );
                v = CmdText.Substring ( 1 );
            }
            else
            {
                Value = CmdText;
                return;
            }

            // determine the name and if there is a value associated to the parameter
            if ( string.IsNullOrEmpty ( v ) )
                return;
            pos = v.IndexOf ( ':' );
            if ( pos > 0 )
            {
                Name = v.Substring ( 0, pos );
                Value = v.Substring ( pos + 1 );
            }
            else
            {
                Name = v;
            }

        }

    }

}
