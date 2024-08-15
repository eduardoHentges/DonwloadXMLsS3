using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DownloadXMLsS3
{
    public partial class Form1 : Form
    {
        private const int BatchSize = 50; // Tamanho do lote
        private int totalFiles = 0;
        private int filesDownloaded = 0;
        private CancellationTokenSource cancellationTokenSource;

        public Form1()
        {
            InitializeComponent();
        }

        private async void btnDownload_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string folderPath = folderBrowserDialog.SelectedPath;
                string[] urls = txtUrls.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                totalFiles = urls.Length;
                filesDownloaded = 0;
                lblProgress.Text = $"Progress: 0/{totalFiles}";

                // Initialize CancellationTokenSource
                cancellationTokenSource = new CancellationTokenSource();
                CancellationToken token = cancellationTokenSource.Token;

                try
                {
                    for (int i = 0; i < urls.Length; i += BatchSize)
                    {
                        if (token.IsCancellationRequested)
                        {
                            listBoxStatus.Items.Add("Download canceled.");
                            break;
                        }

                        var batch = urls.Skip(i).Take(BatchSize);
                        var downloadTasks = batch.Select(url => DownloadAndSaveXML(url, folderPath, token)).ToArray();

                        await Task.WhenAll(downloadTasks);
                        filesDownloaded += downloadTasks.Length;
                        lblProgress.Text = $"Progress: {filesDownloaded}/{totalFiles}";
                    }
                }
                catch (OperationCanceledException)
                {
                    listBoxStatus.Items.Add("Download canceled.");
                }
                catch (Exception ex)
                {
                    listBoxStatus.Items.Add($"Error: {ex.Message}");
                }
            }
        }

        private async Task DownloadAndSaveXML(string url, string folderPath, CancellationToken token)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Set timeout or other configurations if necessary
                    client.Timeout = TimeSpan.FromMinutes(5);

                    // Use GetAsync and read the response content
                    using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token))
                    {
                        response.EnsureSuccessStatusCode();
                        var xmlContent = await response.Content.ReadAsStringAsync();

                        // Extrair o nome do arquivo da URL
                        string fileName = Path.GetFileName(url);
                        string filePath = Path.Combine(folderPath, fileName);

                        // Salvar o arquivo XML
                        File.WriteAllText(filePath, xmlContent);
                        listBoxStatus.Items.Add($"Downloaded: {filePath}");
                    }
                }
                catch (Exception ex)
                {
                    listBoxStatus.Items.Add($"Error downloading {url}: {ex.Message}");
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            // Cancelar o download
            cancellationTokenSource?.Cancel();
        }
        private void btnClear_Click(object sender, EventArgs e)
        {
            txtUrls.Clear();
           
        }

        private void btnClearListBox_Click(object sender, EventArgs e)
        {
            
            listBoxStatus.Items.Clear();
        }
    }
}
