/* Copyright 2009 HPDI, LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Windows.Forms;

namespace Hpdi.Vss2Git
{
    /// <summary>
    /// Entrypoint to the application.
    /// </summary>
    /// <author>Trevor Robinson</author>
    static class Program
    {

        /// <summary>Get the parsed parameters passed via the command line</summary>
        public static CommandLineParser CmdLine = new CommandLineParser ();

        /// <summary>Main processor which has its parameters loaded from the command line</summary>
        public static Processor MainProc = new Processor ();

        [STAThread]
        static void Main ( string[] Args )
        {

            // parse command line parameters and setup the main processor
            try
            {
                CmdLine.Parse ( Args );
                MainProc.Parameters.Load ( CmdLine );
            }
            catch ( Exception Ex )
            {
                MessageBox.Show ( Ex.Message, "Command Line Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
                return;
            }

            // show command line usage if requested
            if ( CmdLine.HelpRequested )
            {
                MessageBox.Show ( MainProc.Parameters.GenerateHelpMsg (), "Command Line Usage", MessageBoxButtons.OK, MessageBoxIcon.Information );
                return;
            }

            // determine if we should auto-execute or show u/i
            if ( MainProc.Parameters.AutoExecute )
            {
                MainProc.Process ();
            }
            else
            {
                Application.EnableVisualStyles ();
                Application.SetCompatibleTextRenderingDefault ( false );
                MainForm ui = new MainForm ();
                Application.Run ( ui );
            }

        }

    }
}
