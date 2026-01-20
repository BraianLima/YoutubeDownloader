using System.Diagnostics;
using System.Text;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

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

                string ffmpegFolder = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "ffmpeg"
                );

                string argumentos =
                    "--ffmpeg-location \"" + ffmpegFolder + "\" " +
                    "-f \"bv*[ext=mp4]+ba[ext=m4a]/b[ext=mp4]\" " +
                    "--merge-output-format mp4 " +
                    "--restrict-filenames " +
                    "-o \"" + pastaDestino + "\\%(title)s.%(ext)s\" " +
                    "\"" + urlVideo + "\"";


                string output = await ExecutarYtDlpAsync(argumentos);
                string nomeArquivoMp4 = ExtrairNomeArquivo(output);
                string caminhoMp4 = Path.Combine(pastaDestino, nomeArquivoMp4);
                ApagarArquivosTemporarios(pastaDestino);

                MessageBox.Show(
                    $"Download finalizado com sucesso!\n\nArquivo:\n{nomeArquivoMp4}",
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

                var youtube = new YoutubeClient();

                var video = await youtube.Videos.GetAsync(videoId.Value);
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId.Value);

                // Melhor stream de áudio disponível
                var audioStreamInfo = streamManifest
                    .GetAudioOnlyStreams()
                    .GetWithHighestBitrate();

                if (audioStreamInfo == null)
                    throw new Exception("Nenhum stream de áudio encontrado.");

                string pastaDestino = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads"
                );

                string nomeBase = string.Concat(
                    video.Title.Split(Path.GetInvalidFileNameChars())
                );

                string caminhoAudio = Path.Combine(pastaDestino, $"{nomeBase}.webm");
                string caminhoMp3 = Path.Combine(pastaDestino, $"{nomeBase}.mp3");

                var progress = new Progress<double>(p =>
                {
                    int valor = (int)(p * 100);
                    valor = Math.Max(0, Math.Min(100, valor));

                    progressBar1.Invoke(() =>
                    {
                        progressBar1.Value = valor;
                    });
                });

                await youtube.Videos.Streams.DownloadAsync(
                    audioStreamInfo,
                    caminhoAudio,
                    progress
                );

                MessageBox.Show(
                    $"Download finalizado com sucesso!\n\nArquivo:\n{Path.GetFileName(caminhoAudio)}",
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

        private void ApagarArquivosTemporarios(string pastaDestino)
        {
            try
            {
                var arquivosM4a = Directory.GetFiles(
                    pastaDestino,
                    "*.m4a",
                    SearchOption.TopDirectoryOnly
                );

                foreach (var arquivo in arquivosM4a)
                {
                    File.Delete(arquivo);
                }
            }
            catch { }
        }

    }
}
