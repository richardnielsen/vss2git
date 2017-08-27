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
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Hpdi.VssLogicalLib;

namespace Hpdi.Vss2Git
{
    /// <summary>
    /// Main form for the application.
    /// </summary>
    /// <author>Trevor Robinson</author>
    public partial class MainForm : Form
    {
        private readonly Dictionary<int, EncodingInfo> codePages = new Dictionary<int, EncodingInfo>();
        Processor Proc = Program.MainProc;

        public MainForm ()
        {
            InitializeComponent();
        }

        private void goButton_Click ( object sender, EventArgs e )
        {
            try
            {
                WriteSettings ();
                UnloadUI ();
                Proc.UsingUI = true;
                Proc.Process ();
                goButton.Enabled = false;
                cancelButton.Enabled = true;
                statusTimer.Enabled = true;
            }
            catch ( Exception Ex )
            {
                MessageBox.Show ( ExceptionFormatter.Format ( Ex ), "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
        }

        private void cancelButton_Click ( object sender, EventArgs e )
        {
            if ( Proc.Queue != null )
            {
                Proc.Queue.Abort ();
            }
        }

        private void statusTimer_Tick(object sender, EventArgs e)
        {
            statusLabel.Text = Proc.Queue.LastStatus ?? "Idle";
            timeLabel.Text = string.Format("Elapsed: {0:g}", Proc.Queue.ActiveTime);

            if ( Proc.Analyzer != null)
            {
                fileLabel.Text = "Files: " + Proc.Analyzer.FileCount;
                revisionLabel.Text = "Revisions: " + Proc.Analyzer.RevisionCount;
            }

            if ( Proc.Builder != null)
            {
                changeLabel.Text = "Changesets: " + Proc.Builder.Changesets.Count;
            }

            if ( Proc.Queue.IsIdle)
            {
                statusTimer.Enabled = false;
                goButton.Enabled = true;
                cancelButton.Enabled = false;
            }

        }

        private void ShowException(Exception exception)
        {
            MessageBox.Show(ExceptionFormatter.Format(exception), "Unhandled Exception",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Text += " " + Assembly.GetExecutingAssembly().GetName().Version;

            var defaultCodePage = Encoding.Default.CodePage;
            var description = string.Format("System default - {0}", Encoding.Default.EncodingName);
            var defaultIndex = encodingComboBox.Items.Add(description);
            encodingComboBox.SelectedIndex = defaultIndex;

            var encodings = Encoding.GetEncodings();
            foreach (var encoding in encodings)
            {
                var codePage = encoding.CodePage;
                description = string.Format("CP{0} - {1}", codePage, encoding.DisplayName);
                var index = encodingComboBox.Items.Add(description);
                codePages[index] = encoding;
                if (codePage == defaultCodePage)
                {
                    codePages[defaultIndex] = encoding;
                }
            }

            // setup u/i based on command line parameters or last execution values
            if ( Program.CmdLine.Parameters.Count > 0 )
            {
                LoadUI ();
            }
            else
            {
                ReadSettings ();
            }

        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            WriteSettings();
            if ( Proc.Queue != null )
            {
                Proc.Queue.Abort ();
                Proc.Queue.WaitIdle ();
            }
        }

        private void ReadSettings()
        {
            var settings = Properties.Settings.Default;
            vssDirTextBox.Text = settings.VssDirectory;
            vssProjectTextBox.Text = settings.VssProject;
            excludeTextBox.Text = settings.VssExcludePaths;
            outDirTextBox.Text = settings.GitDirectory;
            domainTextBox.Text = settings.DefaultEmailDomain;
            commentTextBox.Text = settings.DefaultComment;
            logTextBox.Text = settings.LogFile;
            transcodeCheckBox.Checked = settings.TranscodeComments;
            forceAnnotatedCheckBox.Checked = settings.ForceAnnotatedTags;
            anyCommentUpDown.Value = settings.AnyCommentSeconds;
            sameCommentUpDown.Value = settings.SameCommentSeconds;
        }

        private void WriteSettings()
        {
            var settings = Properties.Settings.Default;
            settings.VssDirectory = vssDirTextBox.Text;
            settings.VssProject = vssProjectTextBox.Text;
            settings.VssExcludePaths = excludeTextBox.Text;
            settings.GitDirectory = outDirTextBox.Text;
            settings.DefaultEmailDomain = domainTextBox.Text;
            settings.LogFile = logTextBox.Text;
            settings.TranscodeComments = transcodeCheckBox.Checked;
            settings.ForceAnnotatedTags = forceAnnotatedCheckBox.Checked;
            settings.AnyCommentSeconds = (int)anyCommentUpDown.Value;
            settings.SameCommentSeconds = (int)sameCommentUpDown.Value;
            settings.Save();
        }

        /// <summary>Save the u/i parameters to our underlying processing parameters</summary>
        private void UnloadUI ()
        {
            Proc.Parameters.VssDirectory = vssDirTextBox.Text;
            Proc.Parameters.VssProject = vssProjectTextBox.Text;
            Proc.Parameters.VssExcludePaths = excludeTextBox.Text;
            Proc.Parameters.GitDirectory = outDirTextBox.Text;
            Proc.Parameters.EmailDomain = domainTextBox.Text;
            Proc.Parameters.LogFile = logTextBox.Text;
            Proc.Parameters.DefaultComment = commentTextBox.Text;
            Proc.Parameters.TranscodeCommentsUtf8 = transcodeCheckBox.Checked;
            Proc.Parameters.ForceAnnotatedTags = forceAnnotatedCheckBox.Checked;
            Proc.Parameters.IgnoreGitErrors = ignoreErrorsCheckBox.Checked;
            Proc.Parameters.AnyCommentSeconds = (double) anyCommentUpDown.Value;
            Proc.Parameters.SameCommentSeconds = (double) sameCommentUpDown.Value;

            Proc.Parameters.DataEncoding = Encoding.Default;
            EncodingInfo encodingInfo;
            if ( codePages.TryGetValue ( encodingComboBox.SelectedIndex, out encodingInfo ) )
            {
                Proc.Parameters.DataEncoding = encodingInfo.GetEncoding ();
            }
        }

        /// <summary>Load the u/i parameters from our underlying processing parameters</summary>
        private void LoadUI ()
        {
            vssDirTextBox.Text = Proc.Parameters.VssDirectory;
            vssProjectTextBox.Text = Proc.Parameters.VssProject;
            excludeTextBox.Text = Proc.Parameters.VssExcludePaths;
            outDirTextBox.Text = Proc.Parameters.GitDirectory;
            domainTextBox.Text = Proc.Parameters.EmailDomain;
            logTextBox.Text = Proc.Parameters.LogFile;
            commentTextBox.Text = Proc.Parameters.DefaultComment;
            transcodeCheckBox.Checked = Proc.Parameters.TranscodeCommentsUtf8;
            forceAnnotatedCheckBox.Checked = Proc.Parameters.ForceAnnotatedTags;
            ignoreErrorsCheckBox.Checked = Proc.Parameters.IgnoreGitErrors;
            anyCommentUpDown.Value = (decimal) Proc.Parameters.AnyCommentSeconds;
            sameCommentUpDown.Value = (decimal) Proc.Parameters.SameCommentSeconds;
        }

    }

}
