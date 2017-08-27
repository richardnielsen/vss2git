using System;
using System.Reflection;
using Hpdi.VssLogicalLib;
using System.IO;

namespace Hpdi.Vss2Git
{

    /// <summary>The main processor for performing a project conversion from VSS to Git</summary>
	class Processor
	{

        private readonly WorkQueue workQueue = new WorkQueue(1);
        private Logger logger = Logger.Null;

        /// <summary>Default constructor</summary>
        public Processor ()
		{
            UsingUI = false;
            Parameters = new ProcessParameters ();
            workQueue.Idle += WorkQueue_Idle;
        }

        /// <summary>Get the parameters used to perform an export process</summary>
        public ProcessParameters Parameters { get; private set; }

        /// <summary>Get the underlying WorkQueue used to perform the process</summary>
        public WorkQueue Queue { get { return workQueue; } }

        /// <summary>Get the RevisionAnalyzer being used during the current process</summary>
        public RevisionAnalyzer Analyzer { get; private set; }

        /// <summary>Get the ChangesetBuilder being used during the current process</summary>
        public ChangesetBuilder Builder { get; private set; }

        /// <summary>Setup logging given a possible filename to use</summary>
        /// <param name="filename">the path/file name to use for logging</param>
        private void OpenLog ( string filename )
        {
            if ( string.IsNullOrEmpty ( filename ) )
            {
                logger = Logger.Null;
            }
            FileStream fs = File.Open ( filename, FileMode.Append, FileAccess.Write, FileShare.Read );
            logger = new Logger ( fs );
        }

        /// <summary>Get/Set the environment info of whether we are using a U/I or not</summary>
        public bool UsingUI { get; set; }

        /// <summary>Main routine to process a single export of a VSS project to a Git repository</summary>
        public void Process ()
		{

            VssDatabaseFactory df;
            VssDatabase db;
            VssProject project;
            VssItem item;

            OpenLog ( Parameters.LogFile );

            try
            {
                // log the parameters
                logger.WriteLine ( "VSS2Git version {0}", Assembly.GetExecutingAssembly ().GetName ().Version );
                logger.WriteLine ( "VSS Directory: " + Parameters.VssDirectory );
                logger.WriteLine ( "VSS Project: " + Parameters.VssProject );
                logger.WriteLine ( "VSS Exclusions: " + Parameters.VssExcludePaths );
                logger.WriteLine ( "Git Directory: " + Parameters.GitDirectory );
                logger.WriteLine ( "Email Domain: " + Parameters.EmailDomain );
                logger.WriteLine ( "Default Comment: " + Parameters.DefaultComment );
                logger.WriteLine ( "VSS encoding: {0} (CP: {1}, IANA: {2})",
                    Parameters.DataEncoding.EncodingName,
                    Parameters.DataEncoding.CodePage,
                    Parameters.DataEncoding.WebName );
                logger.WriteLine ( "Comment transcoding: " + Parameters.TranscodeCommentsUtf8.ToString () );
                logger.WriteLine ( "Ignore errors: " + Parameters.IgnoreGitErrors.ToString () );
                logger.WriteLine ( "Any Comment Seconds: " + Parameters.AnyCommentSeconds.ToString () );
                logger.WriteLine ( "Same Comment Seconds: " + Parameters.SameCommentSeconds.ToString () );

                // open the sourcesafe database
                df = new VssDatabaseFactory ( Parameters.VssDirectory );
                df.Encoding = Parameters.DataEncoding;
                db = df.Open ();

                // get the item from sourcesafe
                try
                {
                    item = db.GetItem ( Parameters.VssProject );
                }
                catch ( VssPathException ex )
                {
                    throw new Exception ( "Invalid project path! [" + Parameters.VssProject + "]", ex );
                }

                // ensure the item is a project
                project = item as VssProject;
                if ( project == null )
                {
                    throw new Exception ( Parameters.VssProject + " is not a valid project!" );
                }
            }
            catch ( Exception ex )
            {
                logger.Write ( ExceptionFormatter.Format ( ex ) );
                logger.Dispose ();
                logger = Logger.Null;
                throw ex;
            }

            // create and setup a revision analyzer
            Analyzer = new RevisionAnalyzer ( workQueue, logger, db );
            if ( !string.IsNullOrEmpty ( Parameters.VssExcludePaths ) )
            {
                Analyzer.ExcludeFiles = Parameters.VssExcludePaths;
            }
            Analyzer.AddItem ( project );

            // create and setup a change set builder
            Builder = new ChangesetBuilder ( workQueue, logger, Analyzer );
            Builder.AnyCommentThreshold = TimeSpan.FromSeconds ( Parameters.AnyCommentSeconds );
            Builder.SameCommentThreshold = TimeSpan.FromSeconds ( Parameters.SameCommentSeconds );
            Builder.BuildChangesets ();

            if ( !string.IsNullOrEmpty ( Parameters.GitDirectory ) )
            {
                var gitExporter = new GitExporter ( workQueue, logger, Analyzer, Builder );
                if ( !string.IsNullOrEmpty ( Parameters.EmailDomain ) )
                {
                    gitExporter.EmailDomain = Parameters.EmailDomain;
                }
                if ( !string.IsNullOrEmpty ( Parameters.DefaultComment ) )
                {
                    gitExporter.DefaultComment = Parameters.DefaultComment;
                }
                if ( !Parameters.TranscodeCommentsUtf8 )
                {
                    gitExporter.CommitEncoding = Parameters.DataEncoding;
                }
                gitExporter.IgnoreErrors = Parameters.IgnoreGitErrors;
                gitExporter.ExportToGit ( Parameters.GitDirectory );
            }

            // if run from command line, wait until the work is done before exiting
            if ( !UsingUI )
            {
                workQueue.WaitIdle ();
            }

        }

        /// <summary>When the work queue has finished, do some final
        /// processing</summary>
        /// <param name="S">source object which called the routine</param>
        /// <param name="EA">basic EventArgs parameter</param>
        private void WorkQueue_Idle ( object S, EventArgs EA )
        {
            if ( logger != null )
            {
                // log any exceptions that occurred during processing
                var exceptions = workQueue.FetchExceptions ();
                if ( exceptions != null )
                {
                    foreach ( var exception in exceptions )
                    {
                        logger.WriteLine ( ExceptionFormatter.Format ( exception ) );
                    }
                }

                // dispose/close the log file
                logger.Dispose ();
                logger = Logger.Null;
            }
        }

    }

}
