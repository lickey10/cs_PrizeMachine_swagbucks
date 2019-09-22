using Microsoft.Win32;
using RefreshUtilities;
using SCTVObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SCTV
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1301:AvoidDuplicateAccelerators")]
    public partial class MainForm : Form
    {
        private bool loggedIn = false;
        public static string[] blockedTerms;
        public static string[] foundBlockedTerms;
        public static string[] foundBlockedSites;
        public static string blockedTermsPath = "config\\BlockedTerms.txt";
        public static string foundBlockedTermsPath = "config\\FoundBlockedTerms.txt";
        public static string[] blockedSites;
        public static string blockedSitesPath = "config\\BlockedSites.txt";
        public static string foundBlockedSitesPath = "config\\foundBlockedSites.txt";
        public static string loginInfoPath = "config\\LoginInfo.txt";
        public static string statusLogPath = ConfigurationManager.AppSettings["StatusLogPath"];

        public bool adminLock = false;//locks down browser until unlocked by a parent
        public int loggedInTime = 0;
        public bool checkForms = true;
        public bool MonitorActivity = false; //determines whether safesurf monitors page contents, forms, sites, etc...
        int loginMaxTime = 20;//20 minutes
        TabCtlEx tabControlEx = new TabCtlEx();

        bool showVolumeControl = false;
        bool showAddressBar = true;

        private DateTime startTime;
        private string userName;
        string documentString = "";
        bool enterTheContest = false;
        int counterCashstravaganza = 0;
        int counterUnclaimedPrizes = 0;
        string[] videos = null;
        int currentVideoIndex = -1;
        ArrayList videosList = new ArrayList();
        string currentVideoNumberString = "";
        System.Windows.Forms.Timer goToURLTimer = new System.Windows.Forms.Timer();
        ExtendedWebBrowser hideMeBrowser;
        ExtendedWebBrowser swagBucksBrowser;
        bool foundCategory = false;
        bool foundVideo = false;
        bool watchingVideo = false;
        int errorCount = 0;
        int errorCountMax = 5;
        string prevVideoNumberString = "";
        string[] categories = null;
        int currentCategoryIndex = 0;
        int keepRunningTimerTicks = 0;
        string goToUrlString = "";
        RefreshUtilities.RefreshUtilities refreshUtilities;
        FirstRun firstRun = new FirstRun();
        int numberOfPrizesEntered = 0;
        int refreshRate = 50;//the default refresh rate for the videos
        bool foundNextVideo = false;
        int playListCompleteCount = 0; //the number of playlists that have been watched
        int watchingVideosCount = 0;
        string startURL = "http://www.swagbucks.com/watch/";
        string refererURL = "http://www.swagbucks.com/refer/lickey";
        List<string> users = new List<string>();
        string userLoggingOut = "";
        bool logBackIn = false;
        bool loggingIn = false;
        string currentUser = "";

        public Uri URL
        {
            set { _windowManager.ActiveBrowser.Url = value; }
            get { return _windowManager.ActiveBrowser.Url; }
        }

        public bool ShowMenuStrip
        {
            set { this.menuStrip.Visible = value; }
        }

        public FormBorderStyle FormBorder
        {
            set { this.FormBorderStyle = value; }
        }

        public HtmlDocument SetDocument
        {
            set
            {
                if (value.Url.ToString().ToLower().Contains("://www.swagbucks.com/watch/video/"))
                {
                    lblStatus.Text = "Watching video";

                    if (isCurrentVideoWatched(value))
                    {
                        if (!getNextVideo(value))
                        {
                            findNextCategory(value.Body.InnerHtml, false);

                            if (foundCategory)
                            {
                                foundNextVideo = true;

                                playListCompleteCount++;
                                lblPlaylistCount.Text = playListCompleteCount.ToString();

                                if (playListCompleteCount > 25)
                                    RestartApp();
                            }
                            else
                                refreshUtilities.GoToURL("javascript: window.external.CallServerSideCode();", 2, lblRefreshTimer, swagBucksBrowser);
                        }
                        else
                            refreshUtilities.GoToURL(value.Url.ToString(), 10, lblRefreshTimer, swagBucksBrowser);
                    }
                    else
                    {
                        watchingVideosCount++;

                        if (watchingVideosCount < 35)
                            refreshUtilities.GoToURL("javascript: window.external.CallServerSideCode();", 5, lblRefreshTimer, swagBucksBrowser);
                        else
                        {
                            watchingVideosCount = 0;

                            if (!getNextVideo(value))
                            {
                                findNextCategory(value.Body.InnerHtml, false);

                                if (foundCategory)
                                    foundNextVideo = true;
                                else
                                    refreshUtilities.GoToURL("javascript: window.external.CallServerSideCode();", 2, lblRefreshTimer, swagBucksBrowser);
                            }
                        }
                    }
                }
                else
                {
                    watchingVideo = false;

                    _windowManager_DocumentCompleted(value, null);
                }
            }
        }

        //[DllImport("user32.dll")]
        //public static extern IntPtr FindWindow(string strClassName, string strWindowName);

        //[DllImport("user32.dll", SetLastError = true)]
        //public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        //public static void Run(IntPtr windowHandle)
        //{
        //    uint appID = Application. .GetAppIDByWindow(windowHandle);
        //    BFS.Audio.SetApplicationMute(appID, !BFS.Audio.GetApplicationMute(appID));
        //}

        //[DllImport("winmm.dll")]
        //public static extern int waveOutGetVolume(IntPtr h, out uint dwVolume);

        //[DllImport("winmm.dll")]
        //public static extern int waveOutSetVolume(IntPtr h, uint dwVolume);

        //// Constants
        //private const int FEATURE_DISABLE_NAVIGATION_SOUNDS = 21;
        //private const int SET_FEATURE_ON_THREAD = 0x00000001;
        //private const int SET_FEATURE_ON_PROCESS = 0x00000002;
        //private const int SET_FEATURE_IN_REGISTRY = 0x00000004;
        //private const int SET_FEATURE_ON_THREAD_LOCALMACHINE = 0x00000008;
        //private const int SET_FEATURE_ON_THREAD_INTRANET = 0x00000010;
        //private const int SET_FEATURE_ON_THREAD_TRUSTED = 0x00000020;
        //private const int SET_FEATURE_ON_THREAD_INTERNET = 0x00000040;
        //private const int SET_FEATURE_ON_THREAD_RESTRICTED = 0x00000080;

        //// Necessary dll import
        //[DllImport("urlmon.dll")]
        //[PreserveSig]
        //[return: MarshalAs(UnmanagedType.Error)]
        //static extern int CoInternetSetFeatureEnabled(
        //int FeatureEntry,
        //[MarshalAs(UnmanagedType.U4)] int dwFlags,
        //bool fEnable);

        //private const int APPCOMMAND_VOLUME_MUTE = 0x80000;
        //private const int APPCOMMAND_VOLUME_UP = 0xA0000;
        //private const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        //private const int WM_APPCOMMAND = 0x319;

        //[DllImport("user32.dll")]
        //public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg,
        //    IntPtr wParam, IntPtr lParam);

        public MainForm()
        {
            InitializeComponent();

            try
            {
                // save the current volume
                //uint _savedVolume;
                //waveOutGetVolume(swagBucksBrowser.Handle, out _savedVolume);

                //this.FormClosing += delegate
                //{
                //    // restore the volume upon exit
                //    waveOutSetVolume(swagBucksBrowser.Handle, _savedVolume);
                //};

                //// mute
                //waveOutSetVolume(swagBucksBrowser.Handle, 0);

                //CoInternetSetFeatureEnabled(FEATURE_DISABLE_NAVIGATION_SOUNDS, SET_FEATURE_ON_PROCESS, true);

                //SendMessageW(this.Handle, WM_APPCOMMAND, this.Handle,
                //(IntPtr)APPCOMMAND_VOLUME_MUTE);

                firstRun.FirstTimeAppHasRun = firstRun.IsThisFirstRunOnThisPC();

                if (firstRun.FirstTimeAppHasRun)
                {
                    startURL = refererURL;

                    StartInstructions startInstructions = new StartInstructions();
                    startInstructions.Show(this);
                }

                if (!statusLogPath.Contains("."))
                    statusLogPath += "Status_" + DateTime.Now.ToShortDateString().Replace("/", "") + "_" + DateTime.Now.ToShortTimeString().Replace(":", "") + ".txt";

                statusLogPath = statusLogPath.Replace(" ", "");

                useLatestIE();

                tabControlEx.Name = "tabControlEx";
                tabControlEx.SelectedIndex = 0;
                tabControlEx.Visible = false;
                tabControlEx.OnClose += new TabCtlEx.OnHeaderCloseDelegate(tabEx_OnClose);
                tabControlEx.VisibleChanged += new System.EventHandler(this.tabControlEx_VisibleChanged);

                this.panel1.Controls.Add(tabControlEx);
                tabControlEx.Dock = DockStyle.Fill;

                _windowManager = new WindowManager(tabControlEx);
                _windowManager.CommandStateChanged += new EventHandler<CommandStateEventArgs>(_windowManager_CommandStateChanged);
                _windowManager.StatusTextChanged += new EventHandler<TextChangedEventArgs>(_windowManager_StatusTextChanged);
                _windowManager.DocumentCompleted += _windowManager_DocumentCompleted;
                //_windowManager.ActiveBrowser.Navigating += ActiveBrowser_Navigating;
                //_windowManager.ActiveBrowser.ScriptErrorsSuppressed = true;
                _windowManager.ShowAddressBar = showAddressBar;

                startTime = DateTime.Now;
                userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

                initFormsConfigs();

                //string tempString = getRefreshRate().ToString();
                //lblRefreshTimer.Text = tempString;

                ////load blocked terms
                //loadBlockedTerms(blockedTermsPath);

                ////load blocked sites
                //loadBlockedSites(blockedSitesPath);

                ////load found blocked terms
                //loadFoundBlockedTerms(foundBlockedTermsPath);

                ////load found blocked sites
                //loadFoundBlockedSites(foundBlockedSitesPath);


                //getDefaultBrowser();

            }
            catch (Exception ex)
            {
                //Tools.WriteToFile(ex);
                //Application.Restart();
                throw;
            }
        }

        // Starting the app here...
        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                // Open a new browser window

                //hideMeBrowser = _windowManager.New(false);
                //hideMeBrowser.Url = new Uri("https://us.hideproxy.me/index.php");



                swagBucksBrowser = this._windowManager.New();
                swagBucksBrowser.Url = new Uri(startURL);
                swagBucksBrowser.ScriptErrorsSuppressed = true;
                swagBucksBrowser.ObjectForScripting = new MyScript();
                //swagBucksBrowser.Url = new Uri("http://www.swagbucks.com");

                //var hWnd = Application.OpenForms[0].Handle;
                //if (hWnd != IntPtr.Zero)
                //{
                //    //return;

                //    uint pID;
                //    GetWindowThreadProcessId(hWnd, out pID);

                //    if (pID != 0)
                //        VolumeMixer.SetApplicationVolume((int)pID, 0f);
                //}

                refreshUtilities = new RefreshUtilities.RefreshUtilities();
                refreshUtilities.ClickComplete += RefreshUtilities_ClickComplete;
                refreshUtilities.GoToUrlComplete += RefreshUtilities_GoToUrlComplete;
                refreshUtilities.CallMethodComplete += RefreshUtilities_CallMethodComplete;
                refreshUtilities.Error += RefreshUtilities_Error;
            }
            catch (Exception ex)
            {
                //Tools.WriteToFile(ex);
                //Application.Restart();
                throw;
            }
        }

        private void RefreshUtilities_Error(object sender, EventArgs e)
        {
            RestartApp();
        }

        private void RefreshUtilities_CallMethodComplete(object sender, EventArgs e)
        {
            //playListCompleteCount = 0;

            //RestartApp();

            if (sender != null && sender is TimerInfo && ((TimerInfo)sender).MethodToCall.Trim().Length > 0)
            {
                if (((TimerInfo)sender).MethodToCall == "clickWatchVideos")
                {
                    if (!getNextVideo(swagBucksBrowser.Document))
                        findNextCategory(documentString, false);

                    if (!foundCategory)
                        btnWatchVideos_Click(sender, null);
                }
            }

        }

        private void Window_Error(object sender, HtmlElementErrorEventArgs e)
        {
            //Application.Restart();
        }

        private void RefreshUtilities_GoToUrlComplete(object sender, EventArgs e)
        {
            if (sender != null && sender is TimerInfo && ((TimerInfo)sender).Browser is ExtendedWebBrowser)
            {
                ExtendedWebBrowser tempBrowser = (ExtendedWebBrowser)((TimerInfo)sender).Browser;

                if (tempBrowser.IsBusy)
                    tempBrowser.Stop();

                tempBrowser.Url = new Uri(((TimerInfo)sender).UrlToGoTo);
            }
        }

        private void RefreshUtilities_ClickComplete(object sender, EventArgs e)
        {
            numberOfPrizesEntered++;

            lblRefreshTimer.Text = "0 seconds";
            lblStatus.Text = "Looking for next video";

            watchingVideo = false;


        }

        private void ActiveBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            documentString = "";
        }

        private void _windowManager_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            try
            {
                documentString = swagBucksBrowser.DocumentText;

                if (swagBucksBrowser.Document.Url != null && chbAutoRefresh.Checked)
                {
                    swagBucksBrowser.Document.Window.Error += Window_Error;

                    if (swagBucksBrowser.Document.Url.ToString().ToLower().Contains("login"))
                    {
                        refreshUtilities.Cancel();

                        foundCategory = false;
                        foundVideo = false;
                        watchingVideo = false;
                        foundNextVideo = false;
                    }
                    else if (!foundCategory && (swagBucksBrowser.Document.Url.ToString().ToLower() == "https://www.swagbucks.com/watch/" || swagBucksBrowser.Url.ToString().ToLower() == "https://www.swagbucks.com/watch"))//watch home page - find a playlist to start on
                    {
                        foundNextVideo = false;
                        lblStatus.Text = "Looking for next category";

                        findNextCategory(documentString, false);
                    }
                    else if (!foundVideo && !watchingVideo && swagBucksBrowser.Url.ToString().ToLower().Contains("://www.swagbucks.com/watch/playlists/"))//swagbucks video playlist
                    {
                        foundCategory = false;
                        lblStatus.Text = "Looking for next playlist";
                        iterateVideoCards(documentString);
                    }
                    else if (swagBucksBrowser != null && swagBucksBrowser.Document.Url.ToString().ToLower().Contains("://www.swagbucks.com/watch/video/"))//we are watching a video
                    {
                        currentVideoNumberString = swagBucksBrowser.Document.Url.ToString().Replace("https://www.swagbucks.com", "");

                        //if (!foundNextVideo || prevVideoNumberString.Length == 0 || currentVideoNumberString != prevVideoNumberString)//this is a new video
                        if (prevVideoNumberString.Length == 0 || currentVideoNumberString != prevVideoNumberString)//this is a new video
                        {
                            foundVideo = false;
                            prevVideoNumberString = currentVideoNumberString;
                            watchingVideo = true;
                            foundCategory = false;
                            lblWatched.Text = "Not Watched";
                            lblStatus.Text = "Waiting for page and video to load";

                            if (firstRun.FirstTimeAppHasRun)
                            {
                                firstRun.FirstTimeAppHasRun = false;
                                firstRun.IsThisFirstRunOnThisPC();
                            }

                            refreshUtilities.GoToURL("javascript: window.external.CallServerSideCode();", 10, true, lblRefreshTimer, swagBucksBrowser);
                        }
                        else
                            refreshUtilities.GoToURL("javascript: window.external.CallServerSideCode();", 5, lblRefreshTimer, swagBucksBrowser);
                    }
                    else if (swagBucksBrowser.Document.Url.ToString().ToLower() == "https://www.swagbucks.com/")
                        findNextCategory(documentString, false);
                    else
                    {
                        if (!refreshUtilities.IsActive)
                        {
                            lblStatus.Text = "Clicking Watch Videos";

                            refreshUtilities.CallMethod("clickWatchVideos", 20, lblRefreshTimer);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Tools.WriteToFile(ex);

                //this.Close();
                //Application.Restart();
                throw;

                //try
                //{
                //    // Open a new browser window
                //    this._windowManager.Close();
                //    //swagBucksBrowser = this._windowManager.New(false);
                //    //swagBucksBrowser.Url = new Uri("http://www.swagbucks.com/watch/");

                //    _windowManager = new WindowManager(tabControlEx);
                //    _windowManager.CommandStateChanged += new EventHandler<CommandStateEventArgs>(_windowManager_CommandStateChanged);
                //    _windowManager.StatusTextChanged += new EventHandler<TextChangedEventArgs>(_windowManager_StatusTextChanged);
                //    _windowManager.DocumentCompleted += _windowManager_DocumentCompleted;
                //    //_windowManager.ActiveBrowser.Navigating += ActiveBrowser_Navigating;
                //    //_windowManager.ActiveBrowser.ScriptErrorsSuppressed = true;
                //    _windowManager.ShowAddressBar = showAddressBar;

                //    swagBucksBrowser = null;
                //    swagBucksBrowser = this._windowManager.New(false);
                //    swagBucksBrowser.Url = new Uri("http://www.swagbucks.com/watch/");

                //    refreshTimer.Stop();
                //}
                //catch (Exception exc)
                //{
                //    string message = exc.Message;

                //    throw;
                //}
            }
        }

        private void logIn(string username, string password)
        {
            userLoggingOut = "";
            logBackIn = false;
            bool foundEmail = false;
            bool foundPassword = false;
            HtmlElement txtPassword = null;

            log(statusLogPath, "Logging In " + username);

            HtmlElementCollection elc = swagBucksBrowser.Document.GetElementsByTagName("input");

            //find user
            foreach (HtmlElement el in elc)
            {
                //<input placeholder="Email" type="email" class="form-control form-group-sm" name="email" tabindex="1">
                if (el.OuterHtml.ToLower().Contains("type=\"email\""))//this is the email field
                {
                    //el.Focus();
                    //el.InnerText = username;
                    el.SetAttribute("value", username);

                    foundEmail = true;
                }

                //<input placeholder="Password" type="password" class="form-control form-group-sm" name="password" tabindex="2">
                if (el.OuterHtml.ToLower().Contains("tabindex=\"2\""))//this is the password field
                {
                    //el.SetAttribute("text", password);
                    el.SetAttribute("value", password);
                    txtPassword = el;
                    foundPassword = true;

                    break;
                }
            }

            if (foundEmail && foundPassword)
            {
                elc = swagBucksBrowser.Document.Forms;

                //elc = bitVideoBrowser.Document.GetElementsByTagName("form");

                foreach (HtmlElement el in elc)
                {
                    //<form id="simpleLogin-form" action="/Home/Login" method="POST" class="hidden-xs">
                    if (el.OuterHtml.Contains("id=\"simpleLogin-form\"") && el.OuterHtml.Contains("class=\"hidden-xs\""))
                    {
                        //submit the form
                        //el.InvokeMember("submit");
                        //loggingIn = true;
                        //break;

                        HtmlElementCollection elc2 = el.GetElementsByTagName("input");

                        foreach (HtmlElement el2 in elc2)
                        {
                            //find login

                            //<input type="submit" class="WLButton ucase btn btn-success form-control loginbtn" value="Login" tabindex="3">
                            if (el2.OuterHtml.ToLower().Contains("type=\"submit\"") && el2.OuterHtml.ToLower().Contains("value=\"login\""))//this is the login button
                            {
                                loggingIn = true;
                                refreshUtilities.ClickElement(el2, 3, true, lblRefreshTimer);

                                currentUser = username + "|" + password;

                                break;
                            }
                        }
                    }
                }
            }
        }

        private bool isCurrentVideoWatched(HtmlDocument pageDocument)
        {
            string currentVideoIDString = pageDocument.Url.ToString();
            string splitString = "class=\"sbPlaylistVideo\"";

            currentVideoIDString = findValue(currentVideoIDString, "video/", "/");

            videos = pageDocument.Body.InnerHtml.Split(new string[] { splitString }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string video in videos)
            {
                if (video.Contains("iconCheckmark") && video.Contains(currentVideoIDString))
                {
                    lblWatched.Text = "Watched";
                    watchingVideo = false;
                    return true;
                }
            }

            lblWatched.Text = "Not Watched";
            return false;
        }

        private bool getNextVideo(HtmlDocument pageDocument)
        {
            string currentVideoIDString = pageDocument.Url.ToString();
            string splitString = "class=\"sbPlaylistVideo\"";
            bool foundCurrentVideo = false;

            currentVideoIDString = findValue(currentVideoIDString, "video/", "/");

            videos = pageDocument.Body.InnerHtml.Split(new string[] { splitString }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string video in videos)
            {
                if (foundCurrentVideo)//go to next video
                {
                    string nextVideoURL = findValue(video, "href=\"", "\"");
                    nextVideoURL = pageDocument.Url.Scheme + "://" + pageDocument.Url.Host + nextVideoURL;

                    if (nextVideoURL.Trim().Length > 0)
                    {
                        refreshUtilities.GoToURL(nextVideoURL, 4, true, lblRefreshTimer, swagBucksBrowser);
                        foundNextVideo = true;
                        foundCategory = false;
                        lblStatus.Text = "Going to next video";

                        return true;
                    }
                }

                if (video.Contains("iconCheckmark") && video.Contains(currentVideoIDString))
                //if (video.Contains(currentVideoIDString))
                {
                    foundCurrentVideo = true;
                }
            }

            return false;
        }

        private int getRefreshRate()
        {
            int tempRefreshRate = -1;
            string tempString = "";

            if (File.Exists("config.cel"))
            {
                foreach (string configLine in File.ReadAllLines("config.cel"))
                {
                    if (configLine.Contains("RefreshRate="))
                    {
                        tempString = configLine.Replace("RefreshRate=", "");

                        if (!int.TryParse(tempString, out tempRefreshRate))
                            tempRefreshRate = refreshRate;
                        else
                            refreshRate = tempRefreshRate;

                        break;
                    }
                }
            }

            if (tempRefreshRate == -1)
                tempRefreshRate = refreshRate;

            return tempRefreshRate;
        }

        private void saveRefreshRate()
        {
            string[] tempRefreshRate = new string[1];
            tempRefreshRate[0] = "RefreshRate=" + refreshRate;

            File.WriteAllLines("config.cel", tempRefreshRate);
        }

        private void findNextCategory(string pageContent, bool goInstantly)
        {
            try
            {
                string splitString = "<li";
                videosList.Clear();
                foundVideo = false;
                watchingVideo = false;

                //get playlist string
                pageContent = findValue(pageContent, "<ul id=\"mainNavCategoriesList\" class=\"navList\">", "</ul>", false).Trim();

                if (categories == null || categories.Length == 0)
                    categories = pageContent.Split(new string[] { splitString }, StringSplitOptions.RemoveEmptyEntries);

                if (categories.Length > 1)
                {
                    //randomize category selection
                    Random rnd = new Random();
                    int rndCategoryIndex = rnd.Next(1, categories.Length - 1);

                    string category = categories[rndCategoryIndex];

                    //get url and go
                    string newURL = findValue(category, "<a href=\"", "\"");

                    if (newURL.Trim().Length > 0)
                    {
                        //currentCategoryIndex = newCategoryIndex;
                        foundCategory = true;
                        watchingVideo = false;

                        lblStatus.Text = "Going to next Category";

                        newURL = "http://www.swagbucks.com" + newURL;

                        if (goInstantly)
                            refreshUtilities.GoToURL(newURL, 1, 0, true, lblRefreshTimer, swagBucksBrowser);
                        else
                            refreshUtilities.GoToURL(newURL, 10, true, lblRefreshTimer, swagBucksBrowser);
                    }
                }
            }
            catch (Exception ex)
            {
                //Tools.WriteToFile(ex);

                //Application.Restart();
                throw;
            }
        }

        private void iterateVideoCards(string pageContent)
        {
            try
            {
                foundNextVideo = false;
                string splitString = "{cardId:";
                string videoURL = "";
                //currentVideoIndex++;
                bool watched = true;
                string watchedString = "";
                string numVideosString = "";
                double numVideos = 0;
                string numPointsString = "";
                double numPoints = 0;
                string bestChoiceUrl = "";
                double bestChoiceScore = 0;

                lblStatus.Text = "Looking for Playlist";

                videosList.Clear();
                foundCategory = false;
                string[] cards = pageContent.Split(new string[] { splitString }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string card in cards)
                {
                    watchedString = findValue(card, "watched: ", ",");
                    bool.TryParse(watchedString, out watched);

                    if (!watched && watchedString.Trim().Length > 0)
                    {
                        videoURL = findValue(card, "link: '", "'");
                        videoURL = videoURL.Replace("\\", "");

                        //find best choice
                        if (videoURL.Trim().Length > 0)
                        {
                            numVideosString = findValue(card, "size: ", ",");
                            double.TryParse(numVideosString, out numVideos);

                            numPointsString = findValue(card, "earnLoc: ", ",");
                            double.TryParse(numPointsString, out numPoints);

                            if (numPoints > 0 && numVideos > 0)
                            {
                                double tempScore = numPoints / numVideos;

                                if (tempScore > bestChoiceScore)
                                {
                                    bestChoiceScore = tempScore;
                                    bestChoiceUrl = videoURL;
                                }
                            }
                        }
                    }
                }

                if (bestChoiceUrl.Trim().Length > 0 && !foundVideo)
                {
                    foundVideo = true;

                    bestChoiceUrl = "http://www.swagbucks.com" + bestChoiceUrl;

                    refreshUtilities.GoToURL(bestChoiceUrl, 5, true, lblRefreshTimer, swagBucksBrowser);

                    lblStatus.Text = "Going to new Playlist";
                }
                else//we have watched all the videos in this playlist find a new one
                {
                    findNextCategory(pageContent, false);
                }
            }
            catch (Exception ex)
            {
                //Tools.WriteToFile(ex);

                //Application.Restart();
                throw;
            }
        }

        private void cleanVideoList()
        {
            try
            {
                ArrayList videosToRemove = new ArrayList();
                string currentVideoString = swagBucksBrowser.Url.ToString().Replace("http://www.swagbucks.com", "");

                if (videosList.Contains(currentVideoString))
                {
                    foreach (string video in videosList)
                    {
                        videosToRemove.Add(video);

                        if (video == currentVideoString)//this is the current video in the playlist
                            break;
                    }

                    foreach (string video in videosToRemove)
                        videosList.Remove(video);
                }
            }
            catch (Exception ex)
            {
                //Tools.WriteToFile(ex);

                //Application.Restart();
                throw;
            }
        }

        protected void RestartApp()
        {
            if ((components != null))
            {
                components.Dispose();
            }
            base.Dispose(true);

            Application.Restart();
        }

        private void initFormsConfigs()
        {
            SettingsHelper helper = SettingsHelper.Current;

            checkForms = helper.CheckForms;
        }

        private void useLatestIE()
        {
            try
            {
                string AppName = Application.ProductName;// My.Application.Info.AssemblyName
                int VersionCode = 0;
                string Version = "";
                object ieVersion = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Internet Explorer").GetValue("svcUpdateVersion");

                if (ieVersion == null)
                    ieVersion = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Internet Explorer").GetValue("Version");

                if (ieVersion != null)
                {
                    Version = ieVersion.ToString().Substring(0, ieVersion.ToString().IndexOf("."));
                    switch (Version)
                    {
                        case "7":
                            VersionCode = 7000;
                            break;
                        case "8":
                            VersionCode = 8888;
                            break;
                        case "9":
                            VersionCode = 9999;
                            break;
                        case "10":
                            VersionCode = 10001;
                            break;
                        default:
                            if (int.Parse(Version) >= 11)
                                VersionCode = 11001;
                            else
                                Tools.WriteToFile(Tools.errorFile, "useLatestIE error: IE Version not supported");
                            break;
                    }
                }
                else
                {
                    Tools.WriteToFile(Tools.errorFile, "useLatestIE error: Registry error");
                }

                //'Check if the right emulation is set
                //'if not, Set Emulation to highest level possible on the user machine
                string Root = "HKEY_CURRENT_USER\\";
                string Key = "Software\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION";

                object CurrentSetting = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(Key).GetValue(AppName + ".exe");

                if (CurrentSetting == null || int.Parse(CurrentSetting.ToString()) != VersionCode)
                {
                    Microsoft.Win32.Registry.SetValue(Root + Key, AppName + ".exe", VersionCode);
                    Microsoft.Win32.Registry.SetValue(Root + Key, AppName + ".vshost.exe", VersionCode);
                }
            }
            catch (Exception ex)
            {
                Tools.WriteToFile(Tools.errorFile, "useLatestIE error: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        // Update the status text
        void _windowManager_StatusTextChanged(object sender, TextChangedEventArgs e)
        {
            this.toolStripStatusLabel.Text = e.Text;
        }

        // Enable / disable buttons
        void _windowManager_CommandStateChanged(object sender, CommandStateEventArgs e)
        {
            this.forwardToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Forward) == BrowserCommands.Forward);
            this.backToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Back) == BrowserCommands.Back);
            this.printPreviewToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.PrintPreview) == BrowserCommands.PrintPreview);
            this.printPreviewToolStripMenuItem.Enabled = ((e.BrowserCommands & BrowserCommands.PrintPreview) == BrowserCommands.PrintPreview);
            this.printToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Print) == BrowserCommands.Print);
            this.printToolStripMenuItem.Enabled = ((e.BrowserCommands & BrowserCommands.Print) == BrowserCommands.Print);
            this.homeToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Home) == BrowserCommands.Home);
            this.searchToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Search) == BrowserCommands.Search);
            this.refreshToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Reload) == BrowserCommands.Reload);
            this.stopToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Stop) == BrowserCommands.Stop);
        }

        #region Tools menu
        // Executed when the user clicks on Tools -> Options
        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OptionsForm of = new OptionsForm())
            {
                of.ShowDialog(this);
            }
        }

        // Tools -> Show script errors
        private void scriptErrorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ScriptErrorManager.Instance.ShowWindow();
        }

        private void UpdateLoginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Login login = new Login())
            {
                login.Update = true;
                login.ShowDialog(this);
            }
        }

        private void modifyBlockedTermsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //display terms
            tcAdmin.Visible = true;
            tcAdmin.BringToFront();

            tcAdmin.SelectedTab = tcAdmin.TabPages["tpChangeLoginInfo"];
        }

        private void modifyBlockedSitesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tcAdmin.Visible = true;
            tcAdmin.BringToFront();
            tcAdmin.SelectedTab = tcAdmin.TabPages["tpBlockedSites"];
        }

        private void foundBlockedTermsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tcAdmin.Visible = true;
            tcAdmin.BringToFront();
            tcAdmin.SelectedTab = tcAdmin.TabPages["tpFoundBlockedTerms"];
        }

        private void foundBlockedSitesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tcAdmin.Visible = true;
            tcAdmin.BringToFront();
            tcAdmin.SelectedTab = tcAdmin.TabPages["tpFoundBlockedSites"];
        }
        #endregion

        #region File Menu

        // File -> Print
        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Print();
        }

        // File -> Print Preview
        private void printPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrintPreview();
        }

        // File -> Exit
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // File -> Open URL
        private void openUrlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenUrlForm ouf = new OpenUrlForm())
            {
                if (ouf.ShowDialog() == DialogResult.OK)
                {
                    ExtendedWebBrowser brw = _windowManager.New(false);
                    brw.Navigate(ouf.Url);
                }
            }
        }

        // File -> Open File
        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = Properties.Resources.OpenFileDialogFilter;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Uri url = new Uri(ofd.FileName);
                    WindowManager.Open(url);
                }
            }
        }
        #endregion

        #region Help Menu

        // Executed when the user clicks on Help -> About
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About();
        }

        /// <summary>
        /// Shows the AboutForm
        /// </summary>
        private void About()
        {
            using (AboutForm af = new AboutForm())
            {
                af.ShowDialog(this);
            }
        }

        #endregion

        /// <summary>
        /// The WindowManager class
        /// </summary>
        public WindowManager _windowManager;

        // This is handy when all the tabs are closed.
        private void tabControlEx_VisibleChanged(object sender, EventArgs e)
        {
            if (tabControlEx.Visible)
            {
                this.panel1.BackColor = SystemColors.Control;
            }
            else
                this.panel1.BackColor = SystemColors.AppWorkspace;
        }

        #region Printing & Print Preview
        private void Print()
        {
            ExtendedWebBrowser brw = _windowManager.ActiveBrowser;
            if (brw != null)
                brw.ShowPrintDialog();
        }

        private void PrintPreview()
        {
            ExtendedWebBrowser brw = _windowManager.ActiveBrowser;
            if (brw != null)
                brw.ShowPrintPreviewDialog();
        }
        #endregion

        #region Toolstrip buttons
        private void closeWindowToolStripButton_Click(object sender, EventArgs e)
        {
            this._windowManager.New();
        }

        private void closeToolStripButton_Click(object sender, EventArgs e)
        {
            //closes browser window
            //this._windowManager.Close();

            //closes admin tabPages
            tcAdmin.Visible = false;
        }

        private void tabEx_OnClose(object sender, CloseEventArgs e)
        {
            //this.userControl11.Controls.Remove(this.userControl11.TabPages[e.TabIndex]);

            //closes browser window
            this._windowManager.Close();
        }

        private void printToolStripButton_Click(object sender, EventArgs e)
        {
            Print();
        }

        private void printPreviewToolStripButton_Click(object sender, EventArgs e)
        {
            PrintPreview();
        }

        private void backToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null && _windowManager.ActiveBrowser.CanGoBack)
                _windowManager.ActiveBrowser.GoBack();
        }

        private void forwardToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null && _windowManager.ActiveBrowser.CanGoForward)
                _windowManager.ActiveBrowser.GoForward();
        }

        private void stopToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null)
            {
                _windowManager.ActiveBrowser.Stop();
            }
            stopToolStripButton.Enabled = false;
        }

        private void refreshToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null)
            {
                _windowManager.ActiveBrowser.Refresh(WebBrowserRefreshOption.Normal);
            }
        }

        private void homeToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null)
                _windowManager.ActiveBrowser.GoHome();
        }

        private void searchToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null)
                _windowManager.ActiveBrowser.GoSearch();
        }

        #endregion

        public WindowManager WindowManager
        {
            get { return _windowManager; }
        }

        /// <summary>
        /// load blocked terms from file
        /// </summary>
        /// <param name="path"></param>
        public void loadBlockedTerms(string path)
        {
            blockedTerms = File.ReadAllLines(path);

            if (!validateBlockedTerms())
            {
                //decrypt terms
                blockedTerms = Encryption.Decrypt(blockedTerms);
            }

            if (!validateBlockedTerms())
            {
                //log that terms have been tampered with
                log(blockedTermsPath, "Blocked Terms file has been tampered with.  Reinstall SafeSurf");
                //block all pages
                adminLock = true;
            }

            dgBlockedTerms.Dock = DockStyle.Fill;
            dgBlockedTerms.Anchor = AnchorStyles.Right;
            dgBlockedTerms.Anchor = AnchorStyles.Bottom;
            dgBlockedTerms.Anchor = AnchorStyles.Left;
            dgBlockedTerms.Anchor = AnchorStyles.Top;
            dgBlockedTerms.Columns.Add("Terms", "Terms");
            dgBlockedTerms.Refresh();

            foreach (string term in blockedTerms)
            {
                dgBlockedTerms.Rows.Add(new string[] { term });
            }
        }

        private void loadBlockedSites(string path)
        {
            blockedSites = File.ReadAllLines(path);

            if (!validateBlockedSites())
            {
                //decrypt terms
                blockedSites = Encryption.Decrypt(blockedSites);
            }

            if (!validateBlockedSites())
            {
                //log that terms have been tampered with
                log(blockedSitesPath, "Blocked Sites file has been tampered with.  Reinstall SafeSurf");
                //block all pages
                adminLock = true;
            }

            dgBlockedSites.Dock = DockStyle.Fill;
            dgBlockedSites.Anchor = AnchorStyles.Right;
            dgBlockedSites.Anchor = AnchorStyles.Bottom;
            dgBlockedSites.Anchor = AnchorStyles.Left;
            dgBlockedSites.Anchor = AnchorStyles.Top;
            dgBlockedSites.Columns.Add("Sites", "Sites");

            foreach (string site in blockedSites)
            {
                dgBlockedSites.Rows.Add(new string[] { site });
            }
        }

        public void loadFoundBlockedTerms(string path)
        {
            string fBlockedTerms = "";

            if (File.Exists(path))
                foundBlockedTerms = File.ReadAllLines(path);

            if (foundBlockedTerms != null && foundBlockedTerms.Length > 0)
            {
                //if (!validateFoundBlockedTerms())
                //{
                //decrypt terms
                foundBlockedTerms = Encryption.Decrypt(foundBlockedTerms);
                //}

                if (!validateBlockedTerms())
                {
                    //log that terms have been tampered with
                    log(foundBlockedTermsPath, "Found Blocked Terms file has been tampered with.");
                    //block all pages
                    adminLock = true;
                }

                lbFoundBlockedTerms.DataSource = foundBlockedTerms;
            }
        }

        public void loadFoundBlockedSites(string path)
        {
            if (File.Exists(path))
                foundBlockedSites = File.ReadAllLines(path);

            if (foundBlockedSites != null && foundBlockedSites.Length > 0)
            {

                //if (!validateBlockedTerms())
                //{
                //decrypt terms
                foundBlockedSites = Encryption.Decrypt(foundBlockedSites);
                //}

                //if (!validateBlockedTerms())
                //{
                //    //log that terms have been tampered with
                //    log(blockedTermsPath, "Blocked Terms file has been tampered with.  Reinstall SafeSurf");
                //    //block all pages
                //    adminLock = true;
                //}

                lbFoundBlockedSites.DataSource = foundBlockedSites;
            }
        }

        private bool validateBlockedTerms()
        {
            bool isValid = false;

            foreach (string term in blockedTerms)
            {
                if (term.ToLower() == "fuck")
                {
                    isValid = true;
                    break;
                }
            }

            return isValid;
        }

        private bool validateBlockedSites()
        {
            bool isValid = false;

            foreach (string site in blockedSites)
            {
                if (site.ToLower() == "pussy.org")
                {
                    isValid = true;
                    break;
                }
            }

            return isValid;
        }

        private bool validateFoundBlockedTerms()
        {
            bool isValid = true;

            //foreach (string term in foundBlockedTerms)
            //{
            //    if (term.ToLower().Contains("fuck"))
            //    {
            //        isValid = true;
            //        break;
            //    }
            //}

            return isValid;
        }

        #region datagridview events
        private void dgBlockedTerms_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            //make sure values are valid
            //DataGridView dg = (DataGridView)sender;

        }

        private void dgBlockedTerms_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                //update blocked terms file
                ArrayList terms = new ArrayList();
                string value = "";
                DataGridView dg = (DataGridView)sender;
                foreach (DataGridViewRow row in dg.Rows)
                {
                    value = Convert.ToString(row.Cells["Terms"].Value);
                    if (value != null && value.Trim().Length > 0)
                        terms.Add(value);
                }

                blockedTerms = (string[])terms.ToArray(typeof(string));

                //encrypt
                blockedTerms = Encryption.Encrypt(blockedTerms);

                //save blockedTerms
                File.WriteAllLines(blockedTermsPath, blockedTerms);
            }
            catch (Exception ex)
            {

            }
        }
        #endregion

        private void logHeader(string path)
        {
            if (startTime.CompareTo(File.GetLastWriteTime(path)) == 1)
            {
                StringBuilder content = new StringBuilder();

                content.AppendLine();
                content.AppendLine("User: " + userName + "  Start Time: " + startTime);

                File.AppendAllText(path, Encryption.Encrypt(content.ToString()));
            }
        }

        public void log(string path, string content)
        {
            //make sure the path exists
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            logHeader(path);

            File.AppendAllText(path, content);
        }

        public void log(string path, string[] content)
        {
            //make sure the path exists
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            logHeader(path);

            File.WriteAllLines(path, content);
            //File.WriteAllText(path, content);
        }

        private void tcAdmin_VisibleChanged(object sender, EventArgs e)
        {
            closeToolStripButton.Visible = true;
        }

        private void btnChangePassword_Click(object sender, EventArgs e)
        {
            string[] loginInfo = { "username:" + txtNewUserName.Text.Trim(), "password:" + txtNewPassword.Text.Trim() };
            loginInfo = Encryption.Encrypt(loginInfo);
            File.WriteAllLines(MainForm.loginInfoPath, loginInfo);
            lblLoginInfoUpdated.Visible = true;
        }

        private void tpChangeLoginInfo_Leave(object sender, EventArgs e)
        {
            lblLoginInfoUpdated.Visible = false;
        }

        private string getDefaultBrowser()
        {
            //original value on classesroot
            //"C:\Program Files\Internet Explorer\IEXPLORE.EXE" -nohome

            string browser = string.Empty;
            RegistryKey key = null;
            try
            {
                key = Registry.ClassesRoot.OpenSubKey(@"HTTP\shell\open\command", true);

                //trim off quotes
                //browser = key.GetValue(null).ToString().Replace("\"", "");
                //if (!browser.EndsWith(".exe"))
                //{
                //    //get rid of everything after the ".exe"
                //    browser = browser.Substring(0, browser.ToLower().LastIndexOf(".exe") + 4);
                //}

                browser = key.GetValue(null).ToString();

                //key.SetValue(null, (string)@browser);

                string safeSurfBrowser = "\"" + Application.ExecutablePath + "\"";

                key.SetValue(null, (string)@safeSurfBrowser);
            }
            finally
            {
                if (key != null) key.Close();
            }
            return browser;
        }

        private void JustinRecordtoolStripButton_Click(object sender, EventArgs e)
        {
            //need to get channel name from url
            string[] urlSegments = _windowManager.ActiveBrowser.Url.Segments;

            if (urlSegments[1].ToLower() != "directory")//this is a channel
            {
                string channelName = urlSegments[1];
                DialogResult result = MessageBox.Show("Are you sure you want to download from " + channelName, "Download " + channelName, MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    //pop up justin downloader and start downloading
                    //SCTVJustinTV.Downloader downloader = new SCTVJustinTV.Downloader(channelName, "12", Application.StartupPath + "\\JustinDownloads\\");
                    //SCTVJustinTV.Downloader downloader = new SCTVJustinTV.Downloader();
                    //downloader.Channel = channelName;
                    //downloader.Show();
                }
            }
            else
                MessageBox.Show("You must be watching the channel you want to record");
        }

        private void toolStripButtonFavorites_Click(object sender, EventArgs e)
        {
            string url = "";

            //check for url
            if (_windowManager.ActiveBrowser != null && _windowManager.ActiveBrowser.Url.PathAndQuery.Length > 0)
            {
                url = _windowManager.ActiveBrowser.Url.PathAndQuery;

                //add to onlineMedia.xml
                //SCTVObjects.MediaHandler.AddOnlineMedia(_windowManager.ActiveBrowser.Url.Host, _windowManager.ActiveBrowser.Url.PathAndQuery, "Online", "Favorites", "", "");
            }
            else
                MessageBox.Show("You must browse to a website to add it to your favorites");
        }

        private string findValue(string stringToParse, string startPattern, string endPattern)
        {
            return findValue(stringToParse, startPattern, endPattern, false);
        }

        private string findValue(string stringToParse, string startPattern, string endPattern, bool returnSearchPatterns)
        {
            int start = 0;
            int end = 0;
            string foundValue = "";

            try
            {
                start = stringToParse.IndexOf(startPattern);

                if (start > -1)
                {
                    if (!returnSearchPatterns)
                        stringToParse = stringToParse.Substring(start + startPattern.Length);
                    else
                        stringToParse = stringToParse.Substring(start);

                    end = stringToParse.IndexOf(endPattern);

                    if (end > 0)
                    {
                        if (returnSearchPatterns)
                            foundValue = stringToParse.Substring(0, end + endPattern.Length);
                        else
                            foundValue = stringToParse.Substring(0, end);
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
                //Tools.WriteToFile(ex);
            }

            return foundValue;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            swagBucksBrowser.Url = new Uri("http://www.swagbucks.com/watch/");
        }

        private void chbAutoRefresh_CheckedChanged(object sender, EventArgs e)
        {
            if (!chbAutoRefresh.Checked)
            {
                refreshUtilities.Cancel();

                lblRefreshTimer.Text = "0 seconds";
            }
            else
                refreshUtilities.GoToURL("javascript: window.external.CallServerSideCode();", 1, lblRefreshTimer, swagBucksBrowser);
        }

        private void btnFindCategory_Click(object sender, EventArgs e)
        {
            findNextCategory(swagBucksBrowser.DocumentText, true);
        }

        private void btnGetBestVideo_Click(object sender, EventArgs e)
        {
            iterateVideoCards(swagBucksBrowser.DocumentText);
        }

        private void btnGetNextVideo_Click(object sender, EventArgs e)
        {
            getNextVideo(swagBucksBrowser.Document);
        }

        private void btnIsWatched_Click(object sender, EventArgs e)
        {
            swagBucksBrowser.Url = new Uri("javascript: window.external.CallServerSideCode();");
        }

        private void btnMoreVideos_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.instagc.com/1790146");
        }

        private void btnEarnBitcoin_Click(object sender, EventArgs e)
        {
            Process.Start("https://bitvideo.club/index?ref=lickey");
        }

        private void btnSurveys_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.swagbucks.com/surveys");
        }

        private void btnWatchVideos_Click(object sender, EventArgs e)
        {
            foundCategory = false;
            chbAutoRefresh.Checked = true;
            swagBucksBrowser.Url = new Uri("http://www.swagbucks.com/watch/");
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }
    }

    [ComVisible(true)]
    public class MyScript
    {
        public void CallServerSideCode()
        {
            try
            {
                MainForm currentForm = ((MainForm)Application.OpenForms[0]);

                var doc = currentForm._windowManager.ActiveBrowser.Document;

                //var renderedHtml = doc.GetElementsByTagName("HTML")[0].OuterHtml;

                //currentForm.SetDocumentString = renderedHtml;
                currentForm.SetDocument = doc;
            }
            catch (Exception ex)
            {
                //Application.Restart();
            }
        }
    }
}