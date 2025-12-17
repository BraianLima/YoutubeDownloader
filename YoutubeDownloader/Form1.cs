using System.Diagnostics;
using System.Text;
using YoutubeExplode.Videos;

namespace YoutubeDownloader
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void BloquearControles(bool bloquear)
        {
            txt_url.Enabled = !bloquear;
            btn_audio.Enabled = !bloquear;
            btn_video.Enabled = !bloquear;
        }

        private async Task<string> ExecutarYtDlpAsync(string argumentos)
        {
            var tcs = new TaskCompletionSource<string>();
            var outputCompleto = new StringBuilder();
            string ytDlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"yt-dlp.exe");
            if (!File.Exists(ytDlpPath))
            {
                throw new FileNotFoundException(
                    "yt-dlp.exe não encontrado em: " + ytDlpPath
                );
            }

            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = argumentos,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = basePath
                },
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (s, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;

                outputCompleto.AppendLine(e.Data);

                // Captura progresso (ex: 23.4%)
                if (e.Data.Contains("[download]") && e.Data.Contains("%"))
                {
                    int percentIndex = e.Data.IndexOf('%');
                    string numero = "";

                    for (int i = percentIndex - 1; i >= 0; i--)
                    {
                        if (char.IsDigit(e.Data[i]) || e.Data[i] == '.')
                            numero = e.Data[i] + numero;
                        else
                            break;
                    }

                    if (double.TryParse(
                        numero,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double progresso))
                    {
                        this.Invoke(() =>
                        {
                            int valor = (int)Math.Round(progresso);
                            valor = Math.Max(0, Math.Min(100, valor));
                            progressBar1.Value = valor;
                        });
                    }
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    outputCompleto.AppendLine("ERRO: " + e.Data);
            };

            process.Exited += (s, e) =>
            {
                this.Invoke(() => progressBar1.Value = 100);

                if (process.ExitCode == 0)
                    tcs.SetResult(outputCompleto.ToString());
                else
                    tcs.SetException(new Exception(outputCompleto.ToString()));

                process.Dispose();
            };

            progressBar1.Invoke(() => progressBar1.Value = 0);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return await tcs.Task;
        }


        private string ExtrairNomeArquivo(string output)
        {
            // yt-dlp geralmente escreve: Destination: nome.ext
            foreach (var line in output.Split(Environment.NewLine))
            {
                if (line.Contains("Destination:"))
                {
                    return line.Split("Destination:")[1].Trim();
                }
            }

            return "arquivo gerado";
        }

        private async void btn_video_Click(object sender, EventArgs e)
        {
            try
            {
                BloquearControles(true);
                progressBar1.Value = 0;

                string url = txt_url.Text;
                if (string.IsNullOrEmpty(url))
                {
                    MessageBox.Show(
                        $"Informe o link do vídeo",
                        "Concluído",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                VideoId? videoId = VideoId.TryParse(url);
                if (videoId == null)
                {
                    MessageBox.Show(
                        $"Informe um link válido",
                        "Concluído",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                string urlVideo = "https://youtube.com/watch?v=" + videoId.Value;

                string pastaDestino = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

                string argumentos =
                    $"-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]\" " +
                    $"--merge-output-format mp4 " +
                    $"--audio-format aac " +
                    $"--restrict-filenames " +
                    $"-o \"{pastaDestino}\\%(title)s.%(ext)s\" " +
                    $"\"{urlVideo}\"";

                string output = await ExecutarYtDlpAsync(argumentos);
                string nomeArquivo = ExtrairNomeArquivo(output);

                MessageBox.Show(
                    $"Download finalizado com sucesso!\n\nArquivo:\n{nomeArquivo}",
                    "Concluído",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao baixar o vídeo:\n\n" + ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                BloquearControles(false);
                progressBar1.Value = 0;
            }
        }

        private async void btn_audio_Click(object sender, EventArgs e)
        {
            try
            {
                BloquearControles(true);
                progressBar1.Value = 0;

                string url = txt_url.Text;
                if (string.IsNullOrEmpty(url))
                {
                    MessageBox.Show(
                        $"Informe o link do vídeo",
                        "Concluído",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                VideoId? videoId = VideoId.TryParse(url);
                if (videoId == null)
                {
                    MessageBox.Show(
                        $"Informe um link válido",
                        "Concluído",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                string urlVideo = "https://youtube.com/watch?v=" + videoId.Value;

                string pastaDestino = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

                string argumentos =
                    $"-x --audio-format mp3 " +
                    $"--restrict-filenames " +
                    $"-o \"{pastaDestino}\\%(title)s.%(ext)s\" " +
                    $"\"{urlVideo}\"";

                string output = await ExecutarYtDlpAsync(argumentos);
                string nomeArquivo = ExtrairNomeArquivo(output);

                MessageBox.Show(
                    $"Download finalizado com sucesso!\n\nArquivo:\n{nomeArquivo}",
                    "Concluído",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao baixar o áudio:\n\n" + ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                BloquearControles(false);
                progressBar1.Value = 0;
            }
        }

    }
}
