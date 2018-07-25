using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Renci.SshNet;

namespace MonoBrick
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class UploadToEv3
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("05192f84-a787-4e1c-b18e-869a79265954");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadToEv3"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private UploadToEv3(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }


            m_emptyFolder = GetTemporaryDirectory();

            m_outputWindow = new Lazy<OutputWindowPane>(() =>
            {
                OutputWindow win = Dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput).Object as OutputWindow;
                return win.OutputWindowPanes.Add("MonoBrick Ev3");
            });
        }

        private string m_emptyFolder;

        public static string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }


        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static UploadToEv3 Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        private DTE2 Dte => (DTE2)ServiceProvider.GetService(typeof(DTE));

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new UploadToEv3(package);
        }

        private void WriteLine(string text) => Write(text + Environment.NewLine);
        private void Write(string text)
        {
            var w = m_outputWindow.Value;
            w.OutputString(text);
        }

        private void BuildProject()
        {
            WriteLine("Building project");
            Dte.Solution.SolutionBuild.Build(true);
        }

        private Project GetStartupProject()
        {
            var info = (Array)Dte.Solution.SolutionBuild.StartupProjects;

            if (info == null)
            {
                return null;
            }

            Project p = Dte.Solution.Item(info.GetValue(0));
            return p;
        }

        private string GetEv3ProgramFolderName(Project p)
        {
            return Path.GetFileNameWithoutExtension(p.Properties.Item("OutputFileName").Value.ToString());
        }

        private string GetOutputFolder(Project p)
        {
            var output = p.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
            var projectPath = p.Properties.Item("FullPath").Value.ToString();
            return Path.Combine(projectPath, output);
        }

        private List<string> GetFilesToUpload(Project p)
        {
            string outputFolder = GetOutputFolder(p);

            List<string> filesToUpload = new List<string>();
            if (Directory.Exists(outputFolder))
            {
                foreach (var item in Directory.EnumerateFiles(outputFolder))
                {
                    var ex = Path.GetExtension(item);
                    if (ex == ".exe" || ex == ".dll")
                    {
                        filesToUpload.Add(item);
                    }
                }
            }
            return filesToUpload;
        }



        private void ShowErrorMessage(string title, string message)
        {
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

        }

        private Lazy<OutputWindowPane> m_outputWindow;

 
        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            BuildProject();

            var proj = GetStartupProject();
            if (proj == null)
            {
                ShowErrorMessage("Ev3 Extension", "Could not find startup project");
                return;
            }

            var files = GetFilesToUpload(proj);

            var dest = $"/home/root/apps/{GetEv3ProgramFolderName(proj)}";

            WriteLine($"will upload {files.Count} files to: '{dest}'");

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using (ScpClient client = new ScpClient(new ConnectionInfo("10.0.1.1", "root", new PasswordAuthenticationMethod("root", ""))))
                    {
                        client.Connect();
                        //make sure folder exists
                        client.Upload(new DirectoryInfo(m_emptyFolder), dest);
                        foreach (var item in files)
                        {
                            var fi = new FileInfo(item);
                            WriteLine($"uploading {fi.Name} ...");
                            client.Upload(fi, $"{dest}/{fi.Name}");
                        }

                    }
                    WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Upload finished");
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Error Uploading", ex.ToString());
                }
            });
        }
    }
}
