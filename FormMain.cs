using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace RebootDeleter
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                var files = new List<string>();
                if (args.Length < 2)
                {
                    var dialog = new OpenFileDialog()
                    {
                        AddExtension = true,
                        AutoUpgradeEnabled = true,
                        CheckFileExists = true,
                        CheckPathExists = true,
                        FileName = string.Empty,
                        InitialDirectory = Environment.CurrentDirectory,
                        Multiselect = true,
                        Title = Properties.Strings.FileChooserTitle
                    };
                    dialog.ShowDialog();
                    files.AddRange(dialog.FileNames);
                    if (files.Count == 0) Environment.Exit(1223);
                }
                else
                {
                    files.AddRange(Environment.CommandLine.Split(' ').Skip(1));
                }

                // detect admin rights
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        MessageBox.Show(Properties.Strings.AdminPermissionRequired, default, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(5);
                    }
                }
                // detect if user want to add registery key
                if (files[0] == "/reg")
                {
                    try
                    {
                        using (var rootKey = Registry.ClassesRoot.CreateSubKey(@"*\shell\DeleteOnReboot"))
                        {
                            string name;
                            if (args.Length >= 3)
                            {
                                name = args[2];
                            }
                            else
                            {
                                name = Properties.Strings.ContextMenuItem;
                            }
                            rootKey.SetValue("", name);
                            rootKey.SetValue("HasLUAShield", "");
                            using (var commandKey = rootKey.CreateSubKey("command"))
                            {
                                commandKey.SetValue("", $"\"{Assembly.GetExecutingAssembly().Location}\" \"%1\"");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format(Properties.Strings.ErrorContextMenuItemAddition, $"0x{ex.HResult:x}", ex.Message), default, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(ex.HResult);
                    }
                    MessageBox.Show(Properties.Strings.ContextMenuItemAdditionSuccess, Properties.Strings.Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Environment.Exit(0);
                }
                else if (files[0] == "/unreg")
                {
                    try
                    {
                        Registry.ClassesRoot.DeleteSubKeyTree(@"*\shell\DeleteOnReboot");
                    }
                    catch (ArgumentException)
                    {
                        MessageBox.Show(Properties.Strings.ContextMenuItemAlreadyRemoved, Properties.Strings.Warning, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Environment.Exit(0);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format(Properties.Strings.ErrorContextMenuItemRemoval, $"0x{ex.HResult:x}", ex.Message), Properties.Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(ex.HResult);
                    }
                    MessageBox.Show(Properties.Strings.ContextMenuItemRemoved, Properties.Strings.Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Environment.Exit(0);
                }

                var deletionSucceeded = new List<string>();
                var deletionFailed = new Dictionary<string, Exception>();
                foreach (var file in files)
                {
                    if (string.IsNullOrWhiteSpace(file)) continue;
                    // delete file, will give us win32 exception if anything happens
                    var result = MoveFileExW(file, null, MOVEFILE_DELAY_UNTIL_REBOOT);
                    if (!result)
                    {
                        var exc = new Win32Exception(Marshal.GetLastWin32Error());
                        deletionFailed.Add(file, exc);
                    }
                    else
                    {
                        deletionSucceeded.Add(file);
                    }
                }

                // compose result
                var messageBuilder = new StringBuilder();

                if (deletionSucceeded.Count > 0)
                {
                    messageBuilder.Append(string.Format(Properties.Strings.DeletionScheduledHeader, deletionSucceeded.Count));
                    foreach (var file in deletionSucceeded)
                    {
                        messageBuilder.Append(Environment.NewLine);
                        messageBuilder.Append(file);
                    }
                    if (deletionFailed.Count > 0)
                        for (int i = 0; i < 2; i++)
                            messageBuilder.Append(Environment.NewLine);
                }

                if (deletionFailed.Count > 0)
                {
                    messageBuilder.Append(string.Format(Properties.Strings.DeletionFailedHeader, deletionFailed.Count));
                    foreach (var file in deletionFailed)
                    {
                        messageBuilder.Append(Environment.NewLine);
                        messageBuilder.Append($"0x{file.Value.HResult:x} {file.Value.Message}: {file.Key}");
                    }
                }

                var message = messageBuilder.ToString();
                if (!string.IsNullOrEmpty(message)) MessageBox.Show(message, Properties.Strings.Info, MessageBoxButtons.OK, MessageBoxIcon.Information);
                Environment.Exit(deletionFailed.Count > 0 ? 3 : 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Properties.Strings.ErrorUnexpected, $"0x{ex.HResult:x}", ex), Properties.Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x4;
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool MoveFileExW(string lpExistingFileName, string lpNewFileName, uint dwFlags);
    }
}
