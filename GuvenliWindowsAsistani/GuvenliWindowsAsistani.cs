using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal static class GuvenliWindowsAsistani
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        string apiKey = ApiKeyDialog.GetKey();
        if (!String.IsNullOrWhiteSpace(apiKey))
            Application.Run(new MainForm(apiKey));
    }

    private static string Property(ManagementBaseObject item, string name)
    {
        object value = item[name];
        return value == null ? "" : Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static Dictionary<string, string> FirstRow(string query, params string[] names)
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
        using (ManagementObjectCollection results = searcher.Get())
        {
            foreach (ManagementObject item in results)
            {
                using (item)
                {
                    foreach (string name in names)
                        values[name] = Property(item, name);
                }
                break;
            }
        }
        return values;
    }

    private static string ValueOr(Dictionary<string, string> values, string name, string fallback)
    {
        string value;
        return values.TryGetValue(name, out value) && !String.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    private static string CollectSummary()
    {
        StringBuilder text = new StringBuilder();
        text.AppendLine("SINIRLI WINDOWS SISTEM OZETI");
        text.AppendLine("Gizlilik: Bilgisayar/kullanici adi, seri numarasi, IP/DNS adresi, dosya yolu, baslangic komutu, BIOS seri numarasi veya uygulama listesi dahil edilmez.");

        try
        {
            Dictionary<string, string> os = FirstRow(
                "SELECT Caption, Version, BuildNumber, OSArchitecture FROM Win32_OperatingSystem",
                "Caption", "Version", "BuildNumber", "OSArchitecture");
            text.AppendLine();
            text.AppendLine("ISLETIM SISTEMI");
            text.AppendLine("Surum: " + ValueOr(os, "Caption", "Bilinmiyor"));
            text.AppendLine("Versiyon/build: " + ValueOr(os, "Version", "Bilinmiyor") + " / " + ValueOr(os, "BuildNumber", "Bilinmiyor"));
            text.AppendLine("Mimari: " + ValueOr(os, "OSArchitecture", "Bilinmiyor"));
        }
        catch { text.AppendLine("Isletim sistemi ozeti okunamadi."); }

        try
        {
            Dictionary<string, string> machine = FirstRow(
                "SELECT Manufacturer, Model, TotalPhysicalMemory FROM Win32_ComputerSystem",
                "Manufacturer", "Model", "TotalPhysicalMemory");
            text.AppendLine();
            text.AppendLine("DONANIM");
            text.AppendLine("Uretici/model: " + ValueOr(machine, "Manufacturer", "Bilinmiyor") + " / " + ValueOr(machine, "Model", "Bilinmiyor"));
            ulong memory;
            if (UInt64.TryParse(ValueOr(machine, "TotalPhysicalMemory", "0"), NumberStyles.Integer, CultureInfo.InvariantCulture, out memory))
                text.AppendLine("Fiziksel bellek (GB): " + Math.Round(memory / 1073741824.0, 1).ToString(CultureInfo.InvariantCulture));
        }
        catch { text.AppendLine("Donanim ozeti okunamadi."); }

        try
        {
            ulong cores = 0;
            ulong logicalProcessors = 0;
            bool coresKnown = true;
            bool logicalProcessorsKnown = true;
            bool foundProcessor = false;
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                "SELECT NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor"))
            using (ManagementObjectCollection results = searcher.Get())
            {
                foreach (ManagementObject item in results)
                {
                    using (item)
                    {
                        foundProcessor = true;
                        ulong value;
                        if (UInt64.TryParse(Property(item, "NumberOfCores"), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                            cores += value;
                        else
                            coresKnown = false;
                        if (UInt64.TryParse(Property(item, "NumberOfLogicalProcessors"), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                            logicalProcessors += value;
                        else
                            logicalProcessorsKnown = false;
                    }
                }
            }
            text.AppendLine("Islemci cekirdek/mantiksal islemci sayisi: " +
                (foundProcessor && coresKnown ? cores.ToString(CultureInfo.InvariantCulture) : "Bilinmiyor") + " / " +
                (foundProcessor && logicalProcessorsKnown ? logicalProcessors.ToString(CultureInfo.InvariantCulture) : "Bilinmiyor"));
        }
        catch { text.AppendLine("Islemci ozeti okunamadi."); }

        try
        {
            ulong total = 0;
            ulong free = 0;
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                "SELECT Size, FreeSpace FROM Win32_LogicalDisk WHERE DriveType=3"))
            using (ManagementObjectCollection results = searcher.Get())
            {
                foreach (ManagementObject item in results)
                {
                    using (item)
                    {
                        ulong size;
                        ulong available;
                        if (UInt64.TryParse(Property(item, "Size"), NumberStyles.Integer, CultureInfo.InvariantCulture, out size))
                            total += size;
                        if (UInt64.TryParse(Property(item, "FreeSpace"), NumberStyles.Integer, CultureInfo.InvariantCulture, out available))
                            free += available;
                    }
                }
            }
            text.AppendLine("Yerel disklerin toplam/bos alani (GB): " +
                Math.Round(total / 1073741824.0, 1).ToString(CultureInfo.InvariantCulture) + " / " +
                Math.Round(free / 1073741824.0, 1).ToString(CultureInfo.InvariantCulture));
        }
        catch { text.AppendLine("Disk ozeti okunamadi."); }

        try
        {
            List<string> adapters = new List<string>();
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
            using (ManagementObjectCollection results = searcher.Get())
            {
                foreach (ManagementObject item in results)
                {
                    using (item)
                    {
                        string name = Property(item, "Name");
                        if (!String.IsNullOrWhiteSpace(name))
                            adapters.Add(name);
                    }
                }
            }
            text.AppendLine("Grafik bagdastiricilari: " + (adapters.Count == 0 ? "Bilinmiyor" : String.Join(", ", adapters.ToArray())));
        }
        catch { text.AppendLine("Grafik ozeti okunamadi."); }

        text.AppendLine("Birincil ekran cozunurlugu: " + Screen.PrimaryScreen.Bounds.Width + "x" + Screen.PrimaryScreen.Bounds.Height);
        text.AppendLine("Saat dilimi: " + TimeZoneInfo.Local.Id);
        text.AppendLine("Bolge dili: " + CultureInfo.CurrentCulture.Name);
        return text.ToString();
    }

    private static object GetValue(object value, string key)
    {
        IDictionary<string, object> dictionary = value as IDictionary<string, object>;
        object result;
        return dictionary != null && dictionary.TryGetValue(key, out result) ? result : null;
    }

    private sealed class ApiResult
    {
        public string Report;
        public string Error;
    }

    private static string ApiError(WebException exception, out int status)
    {
        status = 0;
        HttpWebResponse response = exception.Response as HttpWebResponse;
        if (response == null)
            return exception.Message;
        status = (int)response.StatusCode;
        using (response)
        using (Stream stream = response.GetResponseStream())
        {
            if (stream == null)
                return exception.Message;
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                string body = reader.ReadToEnd();
                try
                {
                    object parsed = new JavaScriptSerializer().DeserializeObject(body);
                    object error = GetValue(parsed, "error");
                    object message = GetValue(error, "message");
                    if (message != null)
                        return Convert.ToString(message, CultureInfo.InvariantCulture);
                }
                catch { }
                return String.IsNullOrWhiteSpace(body) ? exception.Message : body;
            }
        }
    }

    private static string ExtractReport(object response)
    {
        ArrayList candidates = GetValue(response, "candidates") as ArrayList;
        if (candidates == null || candidates.Count == 0)
        {
            object feedback = GetValue(response, "promptFeedback");
            object reason = GetValue(feedback, "blockReason");
            if (reason != null)
                throw new InvalidOperationException("Gemini istegi engellendi: " + Convert.ToString(reason, CultureInfo.InvariantCulture));
            throw new InvalidOperationException("Gemini yanitinda rapor metni bulunamadi.");
        }

        object content = GetValue(candidates[0], "content");
        ArrayList parts = GetValue(content, "parts") as ArrayList;
        List<string> strings = new List<string>();
        if (parts != null)
        {
            foreach (object part in parts)
            {
                object value = GetValue(part, "text");
                if (value != null)
                    strings.Add(Convert.ToString(value, CultureInfo.InvariantCulture));
            }
        }
        string report = String.Join(Environment.NewLine, strings.ToArray()).Trim();
        if (String.IsNullOrWhiteSpace(report))
            throw new InvalidOperationException("Gemini bos bir rapor dondurdu.");
        return report;
    }

    private static ApiResult RequestReport(string apiKey, string summary, Action<string> updateStatus)
    {
        string prompt =
            "Asagidaki sinirli Windows sistem ozeti icin Turkce, tarafsiz, anlasilir bir rapor hazirla. " +
            "Yalnizca verilen bilgilere dayan; kesin olmayan konularda kontrol oner. " +
            "Genel ozet, performans gozlemleri ve uygulanabilir onerileri yaz. " +
            "Bu ozet tam bir guvenlik taramasi degildir; guvenlik acigi veya sistemin tamamen guvenli oldugunu iddia etme. " +
            "Komut, kod veya sistemde otomatik degisiklik talimati uretme.\r\n\r\n" + summary;

        Dictionary<string, object> part = new Dictionary<string, object>();
        part["text"] = prompt;
        Dictionary<string, object> content = new Dictionary<string, object>();
        content["parts"] = new object[] { part };
        Dictionary<string, object> config = new Dictionary<string, object>();
        config["temperature"] = 0.2;
        config["maxOutputTokens"] = 2048;
        Dictionary<string, object> request = new Dictionary<string, object>();
        request["contents"] = new object[] { content };
        request["generationConfig"] = config;
        byte[] body = Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(request));
        string[] models = { "gemini-3.8-flash", "gemini-3.6-flash", "gemini-3.5-flash-lite" };
        int[] retrySeconds = { 3, 6 };
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        int lastStatus = 0;
        string lastMessage = "Gemini API istegi tamamlanamadi.";

        foreach (string model in models)
        {
            updateStatus("Gemini " + model + " modeli deneniyor...");
            string url = "https://generativelanguage.googleapis.com/v1beta/models/" + model + ":generateContent";
            for (int attempt = 0; attempt < 3; attempt++)
            {
                HttpWebRequest http = (HttpWebRequest)WebRequest.Create(url);
                http.Method = "POST";
                http.ContentType = "application/json; charset=utf-8";
                http.Headers["x-goog-api-key"] = apiKey;
                http.Timeout = 120000;
                http.ReadWriteTimeout = 120000;
                try
                {
                    using (Stream stream = http.GetRequestStream())
                        stream.Write(body, 0, body.Length);
                    using (HttpWebResponse response = (HttpWebResponse)http.GetResponse())
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        return new ApiResult { Report = ExtractReport(new JavaScriptSerializer().DeserializeObject(reader.ReadToEnd())) };
                }
                catch (WebException exception)
                {
                    int currentStatus;
                    lastMessage = ApiError(exception, out currentStatus);
                    lastStatus = currentStatus;
                    if (currentStatus == 404 || currentStatus == 503)
                        break;
                    if ((currentStatus != 429 && currentStatus != 500 && currentStatus != 502 && currentStatus != 504) || attempt == 2)
                    {
                        if (currentStatus == 0)
                            lastMessage = "Gemini servisine baglanilamadi: " + exception.Message;
                        return new ApiResult { Error = currentStatus > 0
                            ? "Gemini API HTTP " + currentStatus + ": " + lastMessage
                            : lastMessage };
                    }
                    int seconds = retrySeconds[attempt];
                    updateStatus("Gemini gecici olarak yanit vermiyor; " + seconds + " saniye sonra yeniden denenecek...");
                    System.Threading.Thread.Sleep(seconds * 1000);
                }
            }
        }

        if (lastStatus == 503)
            lastMessage = "Gemini modelleri internet uzerinden erisilebilir ancak HTTP 503 yogunluk yaniti veriyor. Biraz sonra tekrar deneyin. Ayrinti: " + lastMessage;
        else if (lastStatus == 404)
            lastMessage = "Gemini modelleri kullanilamiyor veya API anahtariniz icin erisim etkin degil. Ayrinti: " + lastMessage;
        else
            lastMessage = "Gemini modellerinden yanit alinamadi. Ayrinti: " + lastMessage;
        return new ApiResult { Error = lastMessage };
    }

    private sealed class ApiKeyDialog : Form
    {
        private readonly TextBox input;
        private ApiKeyDialog()
        {
            Text = "Güvenli Windows Asistanı - Gemini";
            ClientSize = new System.Drawing.Size(520, 195);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Label notice = new Label();
            notice.Text = "Gemini API anahtarınızı girin. Anahtar kaydedilmez.\r\nTarama özeti, onay vermeden Gemini'ye gönderilmez.";
            notice.Location = new System.Drawing.Point(20, 18);
            notice.Size = new System.Drawing.Size(475, 45);
            Controls.Add(notice);
            input = new TextBox();
            input.Location = new System.Drawing.Point(20, 75);
            input.Size = new System.Drawing.Size(475, 25);
            input.UseSystemPasswordChar = true;
            Controls.Add(input);
            Button ok = new Button();
            ok.Text = "Devam";
            ok.Location = new System.Drawing.Point(305, 135);
            ok.Size = new System.Drawing.Size(88, 32);
            ok.DialogResult = DialogResult.OK;
            Controls.Add(ok);
            Button cancel = new Button();
            cancel.Text = "İptal";
            cancel.Location = new System.Drawing.Point(407, 135);
            cancel.Size = new System.Drawing.Size(88, 32);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        public static string GetKey()
        {
            using (ApiKeyDialog dialog = new ApiKeyDialog())
            {
                while (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (!String.IsNullOrWhiteSpace(dialog.input.Text))
                        return dialog.input.Text.Trim();
                    MessageBox.Show("API anahtarını girin veya İptal'e basın.", "API anahtarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            return null;
        }
    }

    private sealed class MainForm : Form
    {
        private readonly string apiKey;
        private readonly RichTextBox preview;
        private readonly RichTextBox report;
        private readonly CheckBox consent;
        private readonly Button scanButton;
        private readonly Button sendButton;
        private readonly Button saveButton;
        private readonly Label status;
        private string summary;
        private string completedReport;
        private System.ComponentModel.BackgroundWorker worker;

        public MainForm(string apiKey)
        {
            this.apiKey = apiKey;
            Text = "Güvenli Windows Asistanı";
            ClientSize = new System.Drawing.Size(1000, 760);
            MinimumSize = new System.Drawing.Size(760, 600);
            StartPosition = FormStartPosition.CenterScreen;

            scanButton = new Button { Text = "Windows'u tara", Location = new System.Drawing.Point(18, 16), Size = new System.Drawing.Size(140, 34) };
            scanButton.Click += Scan;
            Controls.Add(scanButton);
            sendButton = new Button { Text = "Onayla ve Gemini'ye gönder", Location = new System.Drawing.Point(170, 16), Size = new System.Drawing.Size(220, 34), Enabled = false };
            sendButton.Click += Send;
            Controls.Add(sendButton);
            saveButton = new Button { Text = "Raporu kaydet", Location = new System.Drawing.Point(402, 16), Size = new System.Drawing.Size(140, 34), Enabled = false };
            saveButton.Click += Save;
            Controls.Add(saveButton);
            status = new Label { Text = "Hazır. Tarama yapın; hiçbir bilgi onayınız olmadan gönderilmez.", Location = new System.Drawing.Point(18, 58), Size = new System.Drawing.Size(950, 24) };
            Controls.Add(status);
            Label previewLabel = new Label { Text = "Gönderilecek sınırlı tarama özeti - gönderim önizlemesi", Location = new System.Drawing.Point(18, 90), Size = new System.Drawing.Size(950, 22) };
            Controls.Add(previewLabel);
            preview = new RichTextBox { Location = new System.Drawing.Point(18, 115), Size = new System.Drawing.Size(950, 260), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, ReadOnly = true, Font = new System.Drawing.Font("Consolas", 9) };
            Controls.Add(preview);
            consent = new CheckBox { Text = "Yukarıdaki özeti rapor için HTTPS üzerinden Google Gemini'ye göndermeyi onaylıyorum.", Location = new System.Drawing.Point(18, 385), Size = new System.Drawing.Size(950, 26) };
            consent.CheckedChanged += delegate { sendButton.Enabled = consent.Checked && !String.IsNullOrEmpty(summary) && worker == null; };
            Controls.Add(consent);
            Label reportLabel = new Label { Text = "Gemini raporu", Location = new System.Drawing.Point(18, 420), Size = new System.Drawing.Size(950, 22) };
            Controls.Add(reportLabel);
            report = new RichTextBox { Location = new System.Drawing.Point(18, 445), Size = new System.Drawing.Size(950, 290), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, ReadOnly = true, Font = new System.Drawing.Font("Segoe UI", 10), Text = "Tarama yapın, gönderilecek özeti inceleyin ve açıkça onay verin." };
            Controls.Add(report);
        }

        private void Scan(object sender, EventArgs e)
        {
            scanButton.Enabled = false;
            sendButton.Enabled = false;
            saveButton.Enabled = false;
            consent.Checked = false;
            summary = null;
            completedReport = null;
            report.Clear();
            preview.Text = "Sınırlı Windows özeti toplanıyor...";
            status.Text = "Kişisel hesap, ağ adresi, dosya yolu, başlangıç komutu ve uygulama listesi taranmaz.";
            worker = new System.ComponentModel.BackgroundWorker();
            worker.DoWork += delegate(object source, System.ComponentModel.DoWorkEventArgs args) { args.Result = CollectSummary(); };
            worker.RunWorkerCompleted += delegate(object source, System.ComponentModel.RunWorkerCompletedEventArgs args)
            {
                worker.Dispose();
                worker = null;
                scanButton.Enabled = true;
                if (args.Error != null)
                {
                    preview.Text = "Windows özeti okunamadı: " + args.Error.Message;
                    status.Text = "Tarama tamamlanamadı.";
                    return;
                }
                summary = (string)args.Result;
                preview.Text = summary;
                status.Text = "Önizlemeyi inceleyin; Gemini'ye yalnızca açık onayınızla gönderilir.";
                sendButton.Enabled = consent.Checked;
            };
            worker.RunWorkerAsync();
        }

        private void Send(object sender, EventArgs e)
        {
            if (!consent.Checked || String.IsNullOrEmpty(summary))
                return;
            if (MessageBox.Show(
                "Önizlemede görünen sistem özeti HTTPS üzerinden Google Gemini'ye gönderilecek. Devam edilsin mi?",
                "Veri gönderim onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            sendButton.Enabled = false;
            scanButton.Enabled = false;
            saveButton.Enabled = false;
            report.Text = "Gemini raporu oluşturuyor...";
            status.Text = "Önizlemedeki özet Google Gemini'ye gönderiliyor...";
            worker = new System.ComponentModel.BackgroundWorker { WorkerReportsProgress = true };
            worker.ProgressChanged += delegate(object source, System.ComponentModel.ProgressChangedEventArgs args)
            {
                status.Text = Convert.ToString(args.UserState, CultureInfo.CurrentCulture);
            };
            worker.DoWork += delegate(object source, System.ComponentModel.DoWorkEventArgs args)
            {
                System.ComponentModel.BackgroundWorker background = (System.ComponentModel.BackgroundWorker)source;
                args.Result = RequestReport(apiKey, summary, delegate(string message) { background.ReportProgress(0, message); });
            };
            worker.RunWorkerCompleted += delegate(object source, System.ComponentModel.RunWorkerCompletedEventArgs args)
            {
                worker.Dispose();
                worker = null;
                scanButton.Enabled = true;
                saveButton.Enabled = true;
                if (args.Error != null)
                {
                    report.Text = "Rapor oluşturulamadı:\r\n" + args.Error.Message + "\r\n\r\nÖnizlemedeki tarama özeti korunuyor.";
                    status.Text = "Rapor alınamadı.";
                    sendButton.Enabled = consent.Checked;
                    return;
                }
                ApiResult result = (ApiResult)args.Result;
                if (!String.IsNullOrWhiteSpace(result.Error))
                {
                    report.Text = "Rapor oluşturulamadı:\r\n" + result.Error + "\r\n\r\nÖnizlemedeki tarama özeti korunuyor.";
                    status.Text = "Gemini yanıt vermedi.";
                    sendButton.Enabled = consent.Checked;
                    return;
                }
                completedReport = result.Report;
                report.Text = completedReport;
                status.Text = "Gemini raporu hazır.";
                sendButton.Enabled = consent.Checked;
            };
            worker.RunWorkerAsync();
        }

        private void Save(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(completedReport))
                return;
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "Metin dosyası (*.txt)|*.txt";
                dialog.FileName = "GuvenliWindowsAsistani-Rapor-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".txt";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    File.WriteAllText(dialog.FileName, completedReport, new UTF8Encoding(false));
            }
        }
    }
}
