﻿using QSP.AircraftProfiles;
using QSP.AircraftProfiles.Configs;
using QSP.Common.Options;
using QSP.LibraryExtension;
using QSP.NavData.AAX;
using QSP.RouteFinding;
using QSP.RouteFinding.Airports;
using QSP.RouteFinding.AirwayStructure;
using QSP.RouteFinding.Containers.CountryCode;
using QSP.RouteFinding.Routes.TrackInUse;
using QSP.RouteFinding.TerminalProcedures;
using QSP.UI.Controllers.ButtonGroup;
using QSP.UI.Forms.Options;
using QSP.UI.ToLdgModule.AboutPage;
using QSP.UI.ToLdgModule.AircraftMenu;
using QSP.UI.ToLdgModule.LandingPerf;
using QSP.UI.ToLdgModule.TOPerf;
using QSP.UI.UserControls;
using QSP.UI.Utilities;
using QSP.Updates;
using QSP.Utilities;
using QSP.WindAloft;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static QSP.UI.Controllers.ButtonGroup.BtnGroupController;
using static QSP.UI.Controllers.ButtonGroup.ControlSwitcher;
using static QSP.UI.Factories.ToolTipFactory;
using static QSP.Utilities.LoggerInstance;

namespace QSP.UI.Forms
{
    public partial class QspForm : Form
    {
        private AircraftMenuControl acMenu;
        private FuelPlanningControl fuelMenu;
        private TOPerfControl toMenu;
        private LandingPerfControl ldgMenu;
        private MiscInfoControl miscInfoMenu;
        private AboutPageControl aboutMenu;

        private ProfileManager profiles;
        private AirwayNetwork airwayNetwork;
        private Locator<AppOptions> appOptionsLocator;
        private Locator<CountryCodeManager> countryCodesLocator;
        private ProcedureFilter procFilter;
        private Locator<IWindTableCollection> windTableLocator;
        private Locator<Updater> updaterLocator;

        private BtnGroupController btnControl;
        private ControlSwitcher viewControl;
        private TracksForm trackFrm;
        private WindDataForm windFrm;
        private bool failedToLoadNavDataAtStartUp = false;

        private AppOptions appSettings
        { get { return appOptionsLocator.Instance; } }

        private AirportManager airportList
        { get { return airwayNetwork.AirportList; } }

        private WaypointList wptList
        { get { return airwayNetwork.WptList; } }

        private TrackInUseCollection tracksInUse
        { get { return airwayNetwork.TracksInUse; } }

        private CountryCodeManager countryCodes
        { get { return countryCodesLocator.Instance; } }

        private IEnumerable<UserControl> Pages
        {
            get
            {
                return new UserControl[]
                {
                    acMenu,
                    fuelMenu,
                    toMenu,
                    ldgMenu,
                    miscInfoMenu,
                    aboutMenu
                };
            }
        }

        public QspForm()
        {
            InitializeComponent();
        }

        public void Init()
        {
            ShowSplashWhile(() =>
            {
                AddControls();
                InitData();
                InitControls();
                InitTrackForm();
                InitWindForm();
                DownloadWindIfNeeded();
                DownloadTracksIfNeeded();
            });

            if (failedToLoadNavDataAtStartUp)
            {
                string message = "Please set the correct Nav Data location " +
                    "before using the application.";

                if (appSettings.NavDataLocation ==
                    AppOptions.Default.NavDataLocation)
                {
                    // User did not set the path. 
                    // Maybe its the first time the app starts.
                    MsgBoxHelper.ShowWarning(message);
                }
                else
                {
                    MsgBoxHelper.ShowWarning(
                        "Failed to load Nav Data. " + message);
                }

                ShowOptionsForm(FormStartPosition.CenterScreen, true);
            }
        }

        private void InitWindForm()
        {
            windFrm = new WindDataForm();
            windFrm.Init(windDataStatusLabel,
                windTableLocator, WindDownloadStatus.WaitingManualDownload);
        }

        private void InitTrackForm()
        {
            trackFrm = new TracksForm();
            trackFrm.Init(airwayNetwork, trackStatusLabel);
        }

        private static void ShowSplashWhile(Action action)
        {
            var splash = new Splash();
            splash.Show();
            splash.Refresh();

            action();

            splash.Close();
        }

        private void InitData()
        {
            try
            {
                // Aircraft data
                profiles = new ProfileManager();
                profiles.Initialize();
            }
            catch (PerfFileNotFoundException ex)
            {
                WriteToLog(ex);
                MsgBoxHelper.ShowWarning(ex.Message);
            }

            try
            {
                // Load options.
                appOptionsLocator = new Locator<AppOptions>();
                appOptionsLocator.Instance = OptionManager.ReadOrCreateFile();
            }
            catch (Exception ex)
            {
                WriteToLog(ex);
                MsgBoxHelper.ShowError(
                    "Cannot load options. The application will quit now.");
                Environment.Exit(1);
            }

            try
            {
                InitAirportAndWaypoints();
            }
            catch (Exception ex)
            {
                WriteToLog(ex);
                failedToLoadNavDataAtStartUp = true;

                countryCodesLocator = new Locator<CountryCodeManager>(null);
                airwayNetwork = new AirwayNetwork(
                    new DefaultWaypointList(), new DefaultAirportManager());
            }

            procFilter = new ProcedureFilter();
            windTableLocator = new Locator<IWindTableCollection>();
            windTableLocator.Instance = new DefaultWindTableCollection();
            updaterLocator = new Locator<Updater>();
        }

        /// <exception cref="RwyDataFormatException"></exception>
        /// <exception cref="ReadAirportFileException"></exception>
        /// <exception cref="WaypointFileReadException"></exception>
        /// <exception cref="LoadCountryNamesException"></exception>
        private void InitAirportAndWaypoints()
        {
            string navDataPath = appSettings.NavDataLocation;
            var airportTxtPath = Path.Combine(navDataPath, "Airports.txt");

            var airportList = new AirportManager(
                new AirportDataLoader(airportTxtPath).LoadFromFile());

            var result = new WptListLoader(navDataPath).LoadFromFile();
            countryCodesLocator = result.CountryCodes.ToLocator();
            airwayNetwork = new AirwayNetwork(result.WptList, airportList);
        }

        private void InitControls()
        {
            CheckRegistry();
            SubscribeEvents();

            acMenu.Initialize(profiles);
            acMenu.AircraftsChanged += fuelMenu.RefreshAircrafts;
            acMenu.AircraftsChanged += toMenu.RefreshAircrafts;
            acMenu.AircraftsChanged += ldgMenu.RefreshAircrafts;

            fuelMenu.Init(
                appOptionsLocator,
                airwayNetwork,
                procFilter,
                countryCodesLocator,
                windTableLocator,
                profiles.AcConfigs,
                profiles.FuelData);

            toMenu.Init(
                profiles.AcConfigs,
                profiles.TOTables.ToList(),
                airportList,
                () => fuelMenu.AircraftRequest);

            toMenu.TryLoadState();

            ldgMenu.Init(
                profiles.AcConfigs,
                profiles.LdgTables.ToList(),
                airportList,
                () => fuelMenu.AircraftRequest);

            ldgMenu.TryLoadState();

            InitMiscInfoMenu();
            RefreshAirportInfoSelection();

            fuelMenu.altnControl.AlternatesChanged += (s, e) =>
            RefreshAirportInfoSelection();

            fuelMenu.AircraftRequestChanged += (s, e) =>
            {
                var showReqBtn = fuelMenu.AircraftRequest != null;
                toMenu.requestBtn.Visible = showReqBtn;
                ldgMenu.requestBtn.Visible = showReqBtn;
            };

            airwayNetwork.AirportListChanged += (s, e) =>
            {
                fuelMenu.RefreshForAirportListChange();
                toMenu.Airports = airwayNetwork.AirportList;
                ldgMenu.Airports = airwayNetwork.AirportList;
                miscInfoMenu.AirportList = airwayNetwork.AirportList;
            };

            airwayNetwork.WptListChanged += (s, e) =>
            {
                fuelMenu.OnWptListChanged();
            };

            aboutMenu.Init("QSimPlanner");
            EnableBtnColorControls();
            EnableViewControl();
            AddToolTip();

            FormClosing += CloseMain;

            panel1.HorizontalScroll.Enabled = false;
            panel1.HorizontalScroll.Visible = false;
            panel1.AutoScroll = true;
        }

        private void RefreshAirportInfoSelection()
        {
            miscInfoMenu.SetAltn(fuelMenu.altnControl.Alternates);
        }

        private void InitMiscInfoMenu()
        {
            Func<IEnumerable<string>> altnGetter = () =>
            fuelMenu.altnControl.Controls
            .Select(c => c.IcaoTxtBox.Text.Trim().ToUpper());

            miscInfoMenu.Init(
                airportList,
                windTableLocator,
                true,
                () => fuelMenu.origTxtBox.Text.Trim().ToUpper(),
                () => fuelMenu.destTxtBox.Text.Trim().ToUpper(),
                altnGetter);
        }

        private void AddToolTip()
        {
            var tp = GetToolTip();
            tp.SetToolTip(optionsBtn, "Options");
            tp.SetToolTip(aboutBtn, "About");
        }

        private void SubscribeEvents()
        {
            var origTxtBox = fuelMenu.origTxtBox;

            origTxtBox.TextChanged += (sender, e) =>
            {
                miscInfoMenu.SetOrig(origTxtBox.Text.Trim().ToUpper());
            };

            var destTxtBox = fuelMenu.destTxtBox;

            destTxtBox.TextChanged += (sender, e) =>
            {
                miscInfoMenu.SetDest(destTxtBox.Text.Trim().ToUpper());
            };

            EnableAirportRequests();
            navDataStatusLabel.Click += ViewOptions;
            navDataStatusLabel.MouseEnter += SetHandCursor;
            navDataStatusLabel.MouseLeave += SetDefaultCursor;
            windDataStatusLabel.Click += windDataStatusLabel_Click;
            windDataStatusLabel.MouseEnter += SetHandCursor;
            windDataStatusLabel.MouseLeave += SetDefaultCursor;
            trackStatusLabel.Click += (s, e) => trackFrm.ShowDialog();
            trackStatusLabel.MouseEnter += SetHandCursor;
            trackStatusLabel.MouseLeave += SetDefaultCursor;
            optionsBtn.Click += (s, e) => ShowOptionsForm();
        }

        private void EnableAirportRequests()
        {
            var toControl = toMenu.airportInfoControl;
            toControl.reqAirportBtn.Visible = true;
            toControl.reqAirportBtn.Click += (s, e) =>
            {
                toControl.airportTxtBox.Text = fuelMenu.origTxtBox.Text;
                toControl.rwyComboBox.Text = fuelMenu.origRwyComboBox.Text;
            };

            var ldgControl = ldgMenu.airportInfoControl;
            ldgControl.reqAirportBtn.Visible = true;
            ldgControl.reqAirportBtn.Click += (s, e) =>
            {
                ldgControl.airportTxtBox.Text = fuelMenu.destTxtBox.Text;
                ldgControl.rwyComboBox.Text = fuelMenu.destRwyComboBox.Text;
            };
        }

        private void EnableViewControl()
        {
            viewControl = new ControlSwitcher(
                new BtnControlPair(acConfigBtn, acMenu),
                new BtnControlPair(fuelBtn, fuelMenu),
                new BtnControlPair(toBtn, toMenu),
                new BtnControlPair(ldgBtn, ldgMenu),
                new BtnControlPair(airportBtn, miscInfoMenu),
                new BtnControlPair(aboutBtn, aboutMenu));

            viewControl.Subscribed = true;
        }

        private void EnableBtnColorControls()
        {
            var acConfigPair = new BtnColorPair(acConfigBtn, Color.Black,
                Color.WhiteSmoke, Color.White, Color.FromArgb(192, 0, 0));

            var fuelPair = new BtnColorPair(fuelBtn, Color.Black,
               Color.WhiteSmoke, Color.White, Color.DarkOrange);

            var toPair = new BtnColorPair(toBtn, Color.Black,
                Color.WhiteSmoke, Color.White, Color.ForestGreen);

            var ldgPair = new BtnColorPair(ldgBtn, Color.Black,
            Color.WhiteSmoke, Color.White, Color.FromArgb(0, 170, 170));

            var airportPair = new BtnColorPair(airportBtn, Color.Black,
            Color.WhiteSmoke, Color.White, Color.DodgerBlue);

            var aboutPair = new BtnColorPair(aboutBtn, Color.White,
            Color.Black, Color.White, Color.Turquoise);

            btnControl = new BtnGroupController(
                acConfigPair,
                fuelPair,
                toPair,
                ldgPair,
                airportPair,
                aboutPair);

            btnControl.Initialize();
            btnControl.SetSelected(acConfigBtn);
        }

        private void AddControls()
        {
            acMenu = new AircraftMenuControl();
            fuelMenu = new FuelPlanningControl();
            toMenu = new TOPerfControl();
            ldgMenu = new LandingPerfControl();
            miscInfoMenu = new MiscInfoControl();
            aboutMenu = new AboutPageControl();

            foreach (var i in Pages)
            {
                i.Location = Point.Empty;
                i.Visible = i == acMenu;
                panel2.Controls.Add(i);
            }
        }

        private static void CheckRegistry()
        {
            // Try to check/add registry so that google map works properly. 
            var regChecker = new IeEmulationChecker();

            try
            {
#if DEBUG
                regChecker.DebugRun();
#endif
                regChecker.Run();
            }
            catch (Exception ex)
            {
                WriteToLog(ex);
            }
        }

        private void ViewOptions(object sender, EventArgs e)
        {
            optionsBtn.PerformClick();
        }

        private void SetHandCursor(object sender, EventArgs e)
        {
            Cursor = Cursors.Hand;
        }

        private void SetDefaultCursor(object sender, EventArgs e)
        {
            Cursor = Cursors.Default;
        }

        private void DownloadTracksIfNeeded()
        {
            trackStatusLabel.Image = Properties.Resources.YellowLight;
            trackStatusLabel.Text = "Tracks: Not downloaded";
            if (appSettings.AutoDLTracks) trackFrm.DownloadAllTracks();
        }

        private async void DownloadWindIfNeeded()
        {
            if (appSettings.AutoDLWind)
            {
                await windFrm.DownloadWind();
            }
        }

        private void CloseMain(object sender, CancelEventArgs e)
        {
            if (appSettings.PromptBeforeExit)
            {
                var Result = MessageBox.Show(
                    "Exit the application?",
                    "",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (Result != DialogResult.Yes)
                {
                    // Do not exit the app.
                    e.Cancel = true;
                    return;
                }
            }

            toMenu.TrySaveState();
            ldgMenu.TrySaveState();
            fuelMenu.SaveStateToFile();
        }

        private void windDataStatusLabel_Click(object sender, EventArgs e)
        {
            windFrm.ShowDialog();
        }

        private void ShowOptionsForm(
            FormStartPosition position = FormStartPosition.CenterParent,
            bool showInTaskbar = false)
        {
            using (var frm = new OptionsForm())
            {
                frm.Init(
                   airwayNetwork,
                   countryCodesLocator,
                   appOptionsLocator,
                   updaterLocator);

                frm.NavDataLocationChanged += (s, e) =>
                    fuelMenu.RefreshForNavDataLocationChange();

                frm.ShowInTaskbar = showInTaskbar;
                frm.StartPosition = position;
                frm.ShowDialog();
            }
        }

        private void optionsBtn_MouseEnter(object sender, EventArgs e)
        {
            optionsBtn.ForeColor = Color.White;
            optionsBtn.BackColor = Color.Purple;
        }

        private void optionsBtn_MouseLeave(object sender, EventArgs e)
        {
            optionsBtn.ForeColor = Color.White;
            optionsBtn.BackColor = Color.Black;
        }

        // TODO: Some ideas for future:
        // (1) Flightaware flight plans
        // (2) NOAA temp./wind/sigWx charts
        //     http://aviationweather.gov/webiffdp/page/public?name=iffdp_main
        //     http://aviationweather.gov/iffdp/sgwx
        // (3) Charts (FAA, Eurocontrol)
    }
}
