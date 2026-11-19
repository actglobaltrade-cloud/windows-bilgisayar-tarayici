using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class GuvenliWindowsAsistaniSetup
{
    private static TextBox pathBox;
    private static CheckBox shortcutBox;
    private static Button installButton;
    private static Label status;

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Form form = new Form();
        form.Text = "Güvenli Windows Asistanı - Kurulum";
        form.ClientSize = new System.Drawing.Size(570, 260);
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.StartPosition = FormStartPosition.CenterScreen;

        Label title = new Label { Text = "Güvenli Windows Asistanı", Font = new System.Drawing.Font("Segoe UI", 15, System.Drawing.FontStyle.Bold), Location = new System.Drawing.Point(22, 18), Size = new System.Drawing.Size(520, 32) };
        form.Controls.Add(title);
        Label description = new Label { Text = "Kullanıcı hesabınıza kurulur; yönetici izni istemez.", Location = new System.Drawing.Point(24, 58), Size = new System.Drawing.Size(520, 24) };
        form.Controls.Add(description);
        Label pathLabel = new Label { Text = "Kurulum konumu:", Location = new System.Drawing.Point(24, 98), Size = new System.Drawing.Size(140, 22) };
        form.Controls.Add(pathLabel);
        pathBox = new TextBox { Location = new System.Drawing.Point(24, 124), Size = new System.Drawing.Size(420, 24), Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs\\GuvenliWindowsAsistani") };
        form.Controls.Add(pathBox);
        Button browse = new Button { Text = "Gözat...", Location = new System.Drawing.Point(452, 122), Size = new System.Drawing.Size(90, 27) };
        browse.Click += delegate
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Kurulum klasörünü seçin";
                dialog.SelectedPath = pathBox.Text;
                if (dialog.ShowDialog(form) == DialogResult.OK)
                    pathBox.Text = Path.Combine(dialog.SelectedPath, "GuvenliWindowsAsistani");
            }
        };
        form.Controls.Add(browse);
        shortcutBox = new CheckBox { Text = "Masaüstüne kısayol oluştur", Checked = true, Location = new System.Drawing.Point(24, 160), Size = new System.Drawing.Size(260, 24) };
        form.Controls.Add(shortcutBox);
        status = new Label { Location = new System.Drawing.Point(24, 205), Size = new System.Drawing.Size(330, 24) };
        form.Controls.Add(status);
        installButton = new Button { Text = "Yükle ve başlat", Location = new System.Drawing.Point(390, 195), Size = new System.Drawing.Size(152, 36) };
        installButton.Click += Install;
        form.Controls.Add(installButton);
        form.AcceptButton = installButton;
        Application.Run(form);
    }

    private static void Install(object sender, EventArgs e)
    {
        string path = pathBox.Text.Trim();
        if (String.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show("Kurulum konumu boş olamaz.", "Kurulum", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        installButton.Enabled = false;
        status.Text = "Kuruluyor...";
        Application.DoEvents();
        try
        {
            Directory.CreateDirectory(path);
            string app = Path.Combine(path, "GuvenliWindowsAsistani.exe");
            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("GuvenliWindowsAsistaniSetup.GuvenliWindowsAsistani.exe"))
            {
                if (resource == null)
                    throw new InvalidOperationException("Uygulama kurulum paketinde bulunamadı.");
                using (FileStream output = new FileStream(app, FileMode.Create, FileAccess.Write, FileShare.None))
                    resource.CopyTo(output);
            }

            string menu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Güvenli Windows Asistanı");
            Directory.CreateDirectory(menu);
            CreateShortcut(Path.Combine(menu, "Güvenli Windows Asistanı.lnk"), app);
            if (shortcutBox.Checked)
                CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Güvenli Windows Asistanı.lnk"), app);

            Process.Start(app);
            Application.Exit();
        }
        catch (Exception exception)
        {
            installButton.Enabled = true;
            status.Text = "Kurulum başarısız.";
            MessageBox.Show("Uygulama kurulamadı:\r\n" + exception.Message, "Kurulum hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void CreateShortcut(string shortcutPath, string target)
    {
        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null)
            throw new InvalidOperationException("Windows kısayol bileşeni kullanılamıyor.");
        object shell = Activator.CreateInstance(shellType);
        object shortcut = null;
        try
        {
            shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
            Type shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { target });
            shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { Path.GetDirectoryName(target) });
            shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { target + ",0" });
            shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
        }
        finally
        {
            if (shortcut != null && Marshal.IsComObject(shortcut))
                Marshal.ReleaseComObject(shortcut);
            if (Marshal.IsComObject(shell))
                Marshal.ReleaseComObject(shell);
        }
    }
}
