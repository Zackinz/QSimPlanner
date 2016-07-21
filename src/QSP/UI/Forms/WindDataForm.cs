﻿using QSP.LibraryExtension;
using QSP.UI.Utilities;
using QSP.WindAloft;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using static QSP.Utilities.LoggerInstance;

namespace QSP.UI.Forms
{
    public partial class WindDataForm : Form
    {
        private Locator<WindTableCollection> windTableLocator;
        private ToolStripStatusLabel toolStripLbl;
        private bool windAvailable;

        public WindDataForm()
        {
            InitializeComponent();
        }

        public void Init(
            ToolStripStatusLabel toolStripLbl,
            Locator<WindTableCollection> windTableLocator,
            WindDownloadStatus status)
        {
            this.toolStripLbl = toolStripLbl;
            this.windTableLocator = windTableLocator;
            ShowWindStatus(status);
            windAvailable = false;

            downlaodBtn.Click += async (s, e) => await DownloadWind();
            saveFileBtn.Click += SaveFile;
            loadFileBtn.Click += LoadFile;
        }

        private async void downlaodBtn_Click(object sender, EventArgs e)
        {
            await DownloadWind();
        }

        public async Task DownloadWind()
        {
            downlaodBtn.Enabled = false;
            ShowWindStatus(WindDownloadStatus.Downloading);

            try
            {
                windTableLocator.Instance = await WindManager.LoadWindAsync();
                ShowWindStatus(WindDownloadStatus.FinishedDownload);
                windAvailable = true;
            }
            catch (Exception ex) when (
                ex is ReadWindFileException ||
                ex is DownloadGribFileException)
            {
                WriteToLog(ex);
                ShowWindStatus(WindDownloadStatus.FailedToDownload);
            }

            downlaodBtn.Enabled = true;
        }

        private void ShowWindStatus(WindDownloadStatus item)
        {
            toolStripLbl.Text = item.Text;
            toolStripLbl.Image = item.Image;

            statusLbl.Text = "Status : " + item.Text;
            statusPicBox.BackgroundImage = item.Image;
        }

        private void SaveFile(object sender, EventArgs e)
        {
            var sourceFile = WindManager.DownloadFilePath;

            if (windAvailable == false)
            {
                MsgBoxHelper.ShowWarning(
                  "No wind data has been downloaded or loaded from file.");
                return;
            }

            if (File.Exists(sourceFile) == false)
            {
                MsgBoxHelper.ShowWarning(
                    "The temporary wind data file was deleted. " +
                    "Unable to proceed.");
                return;
            }

            var saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter =
                "grib2 files (*.grib2)|*.grib2|All files (*.*)|*.*";
            saveFileDialog.InitialDirectory = Constants.WxFileDirectory;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                var file = saveFileDialog.FileName;

                try
                {
                    File.Delete(file);
                    File.Copy(sourceFile, file);
                }
                catch (Exception ex)
                {
                    WriteToLog(ex);
                    MsgBoxHelper.ShowWarning("Failed to save file.");
                }
            }
        }

        private async void LoadFile(object sender, EventArgs e)
        {
            loadFileBtn.Enabled = false;
            var openFileDialog = new OpenFileDialog();

            openFileDialog.Filter =
                "grib2 files (*.grib2)|*.grib2|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Constants.WxFileDirectory;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var file = openFileDialog.FileName;

                try
                {
                    ShowWindStatus(WindDownloadStatus.LoadingFromFile);

                    await Task.Factory.StartNew(() => LoadFromFile(file));

                    var fileNameShort = Path.GetFileName(file);
                    var fileNameMsg = fileNameShort.Length > 10 ?
                        "" : $"({fileNameShort})";

                    ShowWindStatus(new WindDownloadStatus(
                        $"Loaded from file {fileNameMsg}",
                        Properties.Resources.GreenLight));
                    windAvailable = true;
                }
                catch (Exception ex)
                {
                    WriteToLog(ex);
                    MsgBoxHelper.ShowWarning(
                        $"Failed to load file {file}");
                }
            }

            loadFileBtn.Enabled = true;
        }

        private void LoadFromFile(string file)
        {
            File.Delete(WindManager.DownloadFilePath);
            File.Copy(file, WindManager.DownloadFilePath);

            GribConverter.ConvertGrib();
            var handler = new WindFileHandler();
            windTableLocator.Instance = handler.ImportAllTables();
            handler.TryDeleteCsvFiles();
        }
    }
}