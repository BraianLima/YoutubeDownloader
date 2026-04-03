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
            string caminhoTemp = ""; // Para limpar depois
            try
            {
                BloquearControles(true);
                progressBar1.Value = 0;

                string url = txt_url.Text;
                VideoId? videoId = VideoId.TryParse(url);
                if (videoId == null)
                {
                    MessageBox.Show("Informe um link válido");
                    return;
                }

                var youtube = new YoutubeClient();
                var video = await youtube.Videos.GetAsync(videoId.Value);
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId.Value);
                var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                if (audioStreamInfo == null) throw new Exception("Áudio não encontrado.");

                string pastaDestino = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

                // Remove caracteres inválidos do título
                string nomeBase = string.Concat(video.Title.Split(Path.GetInvalidFileNameChars()));

                // Definimos o caminho temporário (WebM/M4A) e o final (MP3)
                caminhoTemp = Path.Combine(pastaDestino, $"{nomeBase}.tmp");
                string caminhoMp3 = Path.Combine(pastaDestino, $"{nomeBase}.mp3");

                // 1. Download do Stream original
                var progress = new Progress<double>(p => {
                    this.Invoke(() => progressBar1.Value = (int)(p * 100));
                });

                await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, caminhoTemp, progress);

                // 2. Conversão para MP3
                // Avisar o usuário ou mudar label se tiver (opcional)
                await ConverterParaMp3Async(caminhoTemp, caminhoMp3);

                MessageBox.Show(
                    $"Download e conversão concluídos!\n\nArquivo: {nomeBase}.mp3",
                    "Sucesso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message);
            }
            finally
            {
                // 3. Limpeza: Apaga o arquivo temporário se ele existir
                if (!string.IsNullOrEmpty(caminhoTemp) && File.Exists(caminhoTemp))
                {
                    try { File.Delete(caminhoTemp); } catch { }
                }

                BloquearControles(false);
                progressBar1.Value = 0;
            }
        }

        private async Task ConverterParaMp3Async(string caminhoEntrada, string caminhoSaida)
        {
            string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffmpeg.exe");

            if (!File.Exists(ffmpegPath))
                throw new FileNotFoundException("FFmpeg não encontrado para conversão.");

            // Argumentos: -i (entrada) -q:a 0 (melhor qualidade VBR) -y (sobrescrever se existir)
            string argumentos = $"-i \"{caminhoEntrada}\" -q:a 0 -y \"{caminhoSaida}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = argumentos,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true // FFmpeg envia logs pelo Error
            };

            using (var process = Process.Start(startInfo))
            {
                await process.WaitForExitAsync();
                if (process.ExitCode != 0)
                {
                    var erro = await process.StandardError.ReadToEndAsync();
                    throw new Exception($"Erro na conversão: {erro}");
                }
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
