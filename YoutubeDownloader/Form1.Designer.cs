namespace YoutubeDownloader
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            txt_url = new TextBox();
            btn_audio = new Button();
            btn_video = new Button();
            progressBar1 = new ProgressBar();
            SuspendLayout();
            // 
            // txt_url
            // 
            txt_url.Location = new Point(46, 49);
            txt_url.Name = "txt_url";
            txt_url.Size = new Size(399, 27);
            txt_url.TabIndex = 0;
            // 
            // btn_audio
            // 
            btn_audio.Location = new Point(46, 99);
            btn_audio.Name = "btn_audio";
            btn_audio.Size = new Size(155, 29);
            btn_audio.TabIndex = 1;
            btn_audio.Text = "Baixar como Áudio";
            btn_audio.UseVisualStyleBackColor = true;
            btn_audio.Click += btn_audio_Click;
            // 
            // btn_video
            // 
            btn_video.Location = new Point(274, 99);
            btn_video.Name = "btn_video";
            btn_video.Size = new Size(171, 29);
            btn_video.TabIndex = 2;
            btn_video.Text = "Baixar como Vídeo";
            btn_video.UseVisualStyleBackColor = true;
            btn_video.Click += btn_video_Click;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(46, 170);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(399, 29);
            progressBar1.TabIndex = 3;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(486, 249);
            Controls.Add(progressBar1);
            Controls.Add(btn_video);
            Controls.Add(btn_audio);
            Controls.Add(txt_url);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txt_url;
        private Button btn_audio;
        private Button btn_video;
        private ProgressBar progressBar1;
    }
}
