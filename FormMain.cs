using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;

namespace RebootDeleter {
    public partial class FormMain : Form {
        public FormMain() {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e) {
            try {
                string[] args = Environment.GetCommandLineArgs();
                string file;
                if (args.Length < 2) {
                    var dialog = new OpenFileDialog() {
                        AddExtension = true,
                        AutoUpgradeEnabled = true,
                        CheckFileExists = true,
                        CheckPathExists = true,
                        FileName = "",
                        InitialDirectory = Environment.CurrentDirectory,
                        Multiselect = false,
                        Title = "Choose a file to delete"
                    };
                    dialog.ShowDialog();
                    file = dialog.FileName;
                    if (string.IsNullOrEmpty(file)) Environment.Exit(1223);
                } else {
                    file = args[1];
                }
                // detect admin rights
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent()) {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    if (!principal.IsInRole(WindowsBuiltInRole.Administrator)) {
                        MessageBox.Show("The application must be run under administrative privileges.", default, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(5);
                    }
                }
                // detect if user want to add registery key
                if (file == "/reg") {
                    try {
                        using (var rootKey = Registry.ClassesRoot.CreateSubKey(@"*\shell\DeleteOnReboot")) {
                            string name;
                            if (args.Length >= 3) {
                                name = args[2];
                            } else {
                                // check translation
                                var culture = CultureInfo.CurrentCulture.Parent.Parent;
                                if (!TranslationsContextMenu.TryGetValue(culture.Name, out name)) {
                                    // not found, fallback to default
                                    TranslationsContextMenu.TryGetValue("en", out name);
                                }
                            }
                            rootKey.SetValue("", name);
                            rootKey.SetValue("HasLUAShield", "");
                            using (var commandKey = rootKey.CreateSubKey("command")) {
                                commandKey.SetValue("", $"\"{Assembly.GetExecutingAssembly().Location}\" \"%1\"");
                            }
                        }
                    } catch (Exception ex) {
                        MessageBox.Show($"Unable to add to context menu (0x{ex.HResult.ToString("X")}): {ex.Message}", default, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(ex.HResult);
                    }
                    MessageBox.Show($"Successfully added to context menu.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Environment.Exit(0);
                } else if (file == "/unreg") {
                    try {
                        Registry.ClassesRoot.DeleteSubKeyTree(@"*\shell\DeleteOnReboot");
                    } catch (ArgumentException) {
                        MessageBox.Show("The context menu item has already been removed.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Environment.Exit(0);
                    } catch (Exception ex) {
                        MessageBox.Show($"Unable to remove from context menu (0x{ex.HResult.ToString("X")}): {ex.Message}", default, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(ex.HResult);
                    }
                    MessageBox.Show($"Successfully removed from context menu.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Environment.Exit(0);
                }
                // detect file existence
                if (!File.Exists(file)) {
                    MessageBox.Show("Specified file does not exist.", default, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(3);
                }
                // delete file
                var result = MoveFileExW(file, null, MOVEFILE_DELAY_UNTIL_REBOOT);
                if (!result) {
                    var exc = new Win32Exception(Marshal.GetLastWin32Error());
                    MessageBox.Show($"Failed to delete \"{file}\" (0x{exc.HResult.ToString("X")}): {exc.Message}", default, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(exc.ErrorCode);
                } else {
                    MessageBox.Show($"Successfully scheduled deletion of \"{file}\" on next reboot.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Environment.Exit(0);
                }
            } catch (Exception ex) {
                MessageBox.Show($"Error (0x{ex.HResult.ToString("X")}): {ex.ToString()}", default, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Dictionary<string, string> TranslationsContextMenu = new Dictionary<string, string>() {
            {"en", "Delete on &Reboot"},
            {"zh", "在重启时删除(&R)"},
            {"zh-Hans", "在重启时删除(&R)"},
            {"zh-Hant", "在重啓時刪除(&R)"},
            {"ja", "再起動時に削除(&R)"}
        };

        const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x4;
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool MoveFileExW(string lpExistingFileName, string lpNewFileName, uint dwFlags);
    }
}
