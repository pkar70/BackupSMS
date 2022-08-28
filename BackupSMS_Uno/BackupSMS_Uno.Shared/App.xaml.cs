using System;
//using System.Collections.Generic;
//using System.IO;
using System.Linq;
//using System.Runtime.InteropServices.WindowsRuntime;
//using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
//using Windows.Foundation;
//using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
//using Windows.UI.Xaml.Controls.Primitives;
//using Windows.UI.Xaml.Data;
//using Windows.UI.Xaml.Input;
//using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace BackupSMS
{
    sealed partial class App : Application
    {

        #region "automat"


        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            // this.Suspending += OnSuspending;
        }


        private Frame OnLaunchFragment(ApplicationExecutionState aes)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                // BackupSMS nie ma cofania, więc tego nie potrzebuję
                // ' PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
                //    rootFrame.Navigated += OnNavigatedAddBackButton;
                //Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += OnBackButtonPressed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            return rootFrame;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = OnLaunchFragment(e.PreviousExecutionState);

            if (e != null && e.PrelaunchActivated == true) return;

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            //' to jest m.in. dla Toast i tak dalej?

            //' próba czy to commandline
            if (args.Kind == ActivationKind.CommandLineLaunch)
            {
                CommandLineActivatedEventArgs commandLine = args as CommandLineActivatedEventArgs;
                CommandLineActivationOperation operation = commandLine?.Operation;
                string strArgs = operation?.Arguments;

                if (!string.IsNullOrEmpty(strArgs))
                {
                    await ObsluzCommandLine(strArgs);
                    Window.Current.Close();
                    return;
                }
            }

            // ' jesli nie cmdline (a np. toast), albo cmdline bez parametrow, to pokazujemy okno
            Frame rootFrame = OnLaunchFragment(args.PreviousExecutionState);

            //if (args.Kind == ActivationKind.ToastNotification)
            //    rootFrame.Navigate(GetType(PknPublic));
            //else
            rootFrame.Navigate(typeof(MainPage));

            Window.Current.Activate();

        }
        #endregion


        #region "remsys/cmd line, w VB to jest w Common"

        private Windows.ApplicationModel.AppService.AppServiceConnection moAppConn;
        private string msLocalCmdsHelp = "";

#if NETFX_CORE
        private void RemSysOnServiceClosed(Windows.ApplicationModel.AppService.AppServiceConnection appCon, Windows.ApplicationModel.AppService.AppServiceClosedEventArgs args)
        {
            if (appCon != null) appCon.Dispose();
            if (moTaskDeferal != null)
            {
                moTaskDeferal.Complete();
                moTaskDeferal = null;
            }
        }

        private void RemSysOnTaskCanceled(Windows.ApplicationModel.Background.IBackgroundTaskInstance sender, Windows.ApplicationModel.Background.BackgroundTaskCancellationReason reason)
        {
            if (moTaskDeferal != null)
            {
                moTaskDeferal.Complete();
                moTaskDeferal = null;
            }
        }

#endif 

        /// <summary>
        /// do sprawdzania w OnBackgroundActivated
        /// jak zwróci True, to znaczy że nie wolno zwalniać moTaskDeferal !
        /// sLocalCmdsHelp: tekst do odesłania na HELP
        /// </summary>
        public bool RemSysInit(BackgroundActivatedEventArgs args, string sLocalCmdsHelp)
        {
#if NETFX_CORE
            Windows.ApplicationModel.AppService.AppServiceTriggerDetails oDetails =
                args.TaskInstance.TriggerDetails as Windows.ApplicationModel.AppService.AppServiceTriggerDetails;
            if (oDetails is null) return false;

            msLocalCmdsHelp = sLocalCmdsHelp;

            args.TaskInstance.Canceled += RemSysOnTaskCanceled;
            moAppConn = oDetails.AppServiceConnection;
            moAppConn.RequestReceived += RemSysOnRequestReceived;
            moAppConn.ServiceClosed += RemSysOnServiceClosed;

            return true;
#else
            return false;
#endif 
        }

        public async System.Threading.Tasks.Task<string> CmdLineOrRemSys(string sCommand)
        {
            string sResult = "";

            sResult = p.k.AppServiceStdCmd(sCommand, msLocalCmdsHelp);
            if (string.IsNullOrEmpty(sResult))
                sResult = await AppServiceLocalCommand(sCommand);

            return sResult;
        }
        public async System.Threading.Tasks.Task ObsluzCommandLine(string sCommand)
        {
            Windows.Storage.StorageFolder oFold = Windows.Storage.ApplicationData.Current.TemporaryFolder;
            if (oFold is null) return;

            Windows.Storage.StorageFile oLock = await oFold.CreateFileAsync("cmdline.lock", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            if (oLock is null) return;

            string sResult = await CmdLineOrRemSys(sCommand);
            if (string.IsNullOrEmpty(sResult)) sResult = "(empty - probably unrecognized command)";

            Windows.Storage.StorageFile oResFile = await oFold.CreateFileAsync("stdout.txt", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            if (oResFile is null) return;

            await oResFile.WriteAllTextAsync(sResult);

            await oLock.DeleteAsync();
        }
#if NETFX_CORE
        private async void RemSysOnRequestReceived(Windows.ApplicationModel.AppService.AppServiceConnection sender, Windows.ApplicationModel.AppService.AppServiceRequestReceivedEventArgs args)
        {
            // 'Get a deferral so we can use an awaitable API to respond to the message

            string sStatus;
            string sResult = "";
            Windows.ApplicationModel.AppService.AppServiceDeferral messageDeferral = args.GetDeferral();

            if (p.k.GetSettingsBool("remoteSystemDisabled"))
                sStatus = "No permission";
            else
            {

                Windows.Foundation.Collections.ValueSet oInputMsg = args.Request.Message;

                sStatus = "ERROR while processing command";

                if (oInputMsg.ContainsKey("command"))
                {

                    string sCommand = oInputMsg["command"].ToString();
                    sResult = await CmdLineOrRemSys(sCommand);
                }

                if (sResult != "") sStatus = "OK";

            }

            Windows.Foundation.Collections.ValueSet oResultMsg = new Windows.Foundation.Collections.ValueSet();
            oResultMsg.Add("status", sStatus);
            oResultMsg.Add("result", sResult);

            await args.Request.SendResponseAsync(oResultMsg);

            messageDeferral.Complete();
            moTaskDeferal.Complete();

        }
#endif

#endregion


        private Windows.ApplicationModel.Background.BackgroundTaskDeferral moTaskDeferal = null;

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        { // timer, RemoteSystem
            moTaskDeferal = args.TaskInstance.GetDeferral();

            //p.k.CrashMessageAdd("OnBackgroundActivated");

            bool bNoComplete = false;
            bool bObsluzone = false;

            if (p.k.IsThisTriggerPolnocny(args))
            {
                //p.k.CrashMessageAdd("OnBackgroundActivated - IsThisTriggerPolnocny");

                if (!p.k.GetSettingsBool("noSDcard", true))
                {
                    //p.k.CrashMessageAdd("OnBackgroundActivated - have SD");
                    DateTime oDate = DateTime.Now.AddDays(-1); // .AddHours((double)-DateTime.Now.Hour - 1);
                    await WyciagnijSMS(oDate, true, false, null, "");
                    //p.k.CrashMessageAdd("OnBackgroundActivated - po wyciagnij");
                }
                bObsluzone = true;
            }

            // lista komend danej aplikacji
            string sLocalCmds = "save day \t save data from this day\n" +
                "save since \t save data since last save\n" +
            "save all \t save all data\n";

            if (!bObsluzone) bNoComplete = RemSysInit(args, sLocalCmds);

            if(!bNoComplete) moTaskDeferal.Complete();

        }

        private async System.Threading.Tasks.Task<string> AppServiceLocalCommand(string sCommand)
        {
            switch(sCommand.ToLower())
            {
                case "save day":
                    DateTime oDate = DateTime.Now.AddHours((double)-DateTime.Now.Hour - 1);
                    await WyciagnijSMS(oDate, true, false, null, "");
                    return "DONE";
                case "save since":
                    return sCommand.ToLower() + " unimplemented yet";
                case "save all":
                    return sCommand.ToLower() + " unimplemented yet";
            }
            return "ERROR Unrecognized command"; 
        }

        // własny cache, 2020.01.29
        static System.Collections.Generic.Dictionary<string, string> oNazwyNumery = new System.Collections.Generic.Dictionary<string, string>();


        public static async System.Threading.Tasks.Task<string> PhoneNo2ContactName(string sPhoneNumber)
        {
            // https://stackoverflow.com/questions/34953283/how-to-get-contact-by-phone-number
            if (string.IsNullOrEmpty(sPhoneNumber))
                return "";

            // jesli juz znamy, to zwróć z cache
            string sName = "";
            if (oNazwyNumery.TryGetValue(sPhoneNumber, out sName))
                return sName;

            try
            {

                Windows.ApplicationModel.Contacts.ContactStore oStore = await Windows.ApplicationModel.Contacts.ContactManager.RequestStoreAsync(Windows.ApplicationModel.Contacts.ContactStoreAccessType.AllContactsReadOnly);
                var oCQO = new Windows.ApplicationModel.Contacts.ContactQueryOptions(sPhoneNumber, Windows.ApplicationModel.Contacts.ContactQuerySearchFields.Phone);
                oCQO.DesiredFields = Windows.ApplicationModel.Contacts.ContactQueryDesiredFields.PhoneNumber;
                Windows.ApplicationModel.Contacts.ContactReader oContactRdr = oStore.GetContactReader(oCQO);
                Windows.ApplicationModel.Contacts.ContactBatch oBatch = await oContactRdr.ReadBatchAsync();

                if (oBatch.Contacts.Count < 1)
                    return "";

                sName = oBatch.Contacts[0].DisplayName;
            }
            catch 
            {
                return "????";
            }

            try
            {
                oNazwyNumery.Add(sPhoneNumber, sName);
            }
            catch { }

            return sName;
        }

#if false
        // poprzednia wersja, teraz to jest w pkmodule
        public static async System.Threading.Tasks.Task<Windows.Storage.StorageFolder> GetSDcardFolder()
        {
            try
            {
#if NETFX_CORE
                Windows.Storage.StorageFolder externalDevices = Windows.Storage.KnownFolders.RemovableDevices;
                System.Collections.Generic.IReadOnlyList<Windows.Storage.StorageFolder> oCards = await externalDevices.GetFoldersAsync();
                return oCards.FirstOrDefault();
#else
                Windows.Storage.StorageFolder docFolder = Windows.Storage.KnownFolders.DocumentsLibrary;
                return docFolder;
#endif
            }
            catch 
            {
            }

            return null;
        }


        private static async System.Threading.Tasks.Task<Windows.Storage.StorageFolder> GetFolderForBackup(TextBlock uiMsgCnt)
        {
            Windows.Storage.StorageFolder sdCard = await GetSDcardFolder();

            if (sdCard == null)
            {
                if (uiMsgCnt != null)
                    uiMsgCnt.Text = "Cannot save - no SD card?";
                return null;    // error - nie ma karty
            }

            Windows.Storage.StorageFolder oFold = await sdCard.CreateFolderAsync("DataLogs", Windows.Storage.CreationCollisionOption.OpenIfExists);
            oFold = await oFold?.CreateFolderAsync("BackupSMS", Windows.Storage.CreationCollisionOption.OpenIfExists);
            oFold = await oFold?.CreateFolderAsync(DateTime.Now.ToString("yyyy"), Windows.Storage.CreationCollisionOption.OpenIfExists);
            oFold = await oFold?.CreateFolderAsync(DateTime.Now.ToString("MM"), Windows.Storage.CreationCollisionOption.OpenIfExists);

            return oFold;
        }
#endif 

        private static Windows.Storage.StorageFile oLastExportFile = null;
        private static string sLastExportFileContent = "";

        public static async System.Threading.Tasks.Task<int> WyciagnijSMS(DateTime oDate, bool bInTimer, bool bShowSince, TextBlock uiMsgCnt, string sSufix)
        {
            //p.k.CrashMessageAdd("WyciagnijSMS - START");
            Windows.Storage.StorageFolder oFold = await p.k.GetLogFolderMonthAsync(true);            // GetFolderForBackup(uiMsgCnt);
            if (oFold is null)
            {
                if(uiMsgCnt != null) uiMsgCnt.Text = "Cannot save - no folder?";
                return -1; //error
            }


            int iRet = -1;
            int iMMSattNum = 1; // numer attachmentu MMS
            string sMMSattFolder = "MMS-" + DateTime.Now.ToString("yyyy.MM.dd-HH.mm"); // folder dla MMS attachments

            bool bError = false;
            Windows.ApplicationModel.Chat.ChatMessageReader oRdr = null;

            try
            {
                Windows.ApplicationModel.Chat.ChatMessageStore oStore = await Windows.ApplicationModel.Chat.ChatMessageManager.RequestStoreAsync();
                if (oStore == null)
                {
                    if (uiMsgCnt != null) p.k.DialogBox("No permission (or this is not a phone)");
                    return -1;
                }

                //p.k.CrashMessageAdd("WSMS - got oStore");
                
                oRdr = oStore.GetMessageReader();
                //p.k.CrashMessageAdd("WSMS - got oRdr");
            }
            catch 
            {
                bError = true;
            }

            if (bError)
            {
                if (uiMsgCnt != null)
                    uiMsgCnt.Text = "ERROR - check permissions?";
                return -1;
            }

            string sTxt = "";
            int iGuard = 0;
            bool bStop = false;

            int iLastRunCnt = p.k.GetSettingsInt("lastRunCnt");
            string sLastRun = "";
            if (bShowSince && iLastRunCnt > 0)
                sLastRun = " (/" + iLastRunCnt.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";

            while (iGuard < 10000 && !bStop)
            {
                iGuard = iGuard + 1;
                if (uiMsgCnt != null)
                {
                    string sTmp = "batch " + iGuard.ToString(System.Globalization.CultureInfo.InvariantCulture) + sLastRun;
                    uiMsgCnt.Text = sTmp;
                }
                //p.k.CrashMessageAdd("WSMS - loop, iGuard=" + iGuard);

                System.Collections.Generic.IReadOnlyList<Windows.ApplicationModel.Chat.ChatMessage> oMsgList; // = default(IReadOnlyList<ChatMessage>);
                try     // Try dodane 20190223
                {
                    oMsgList = await oRdr.ReadBatchAsync();
                }
                catch(Exception ex)
                {
                    p.k.CrashMessageAdd("ReadBatchAsync ERROR: ", ex);
                    if (uiMsgCnt != null)
                        uiMsgCnt.Text = "ERROR - check permissions?";
                    return -1;
                }

                if (oMsgList.Count < 1)
                    break;

                // Folder	From	FromAddress	To	ToAddress	Date	Message

                foreach (Windows.ApplicationModel.Chat.ChatMessage oMsg in oMsgList)
                {

                    try
                    {


                    // 2021.05.09: przeniosłem z końca tej pętli
                    if (oMsg.LocalTimestamp < oDate)
                    {
                        bStop = true;
                        break;
                    }

                    if (oMsg.IsIncoming)
                        sTxt = sTxt + "Inbox|";
                    else
                        sTxt = sTxt + "Outbox|";

                    sTxt = sTxt + await PhoneNo2ContactName(oMsg.From) + "|";   // from
                    sTxt = sTxt + oMsg.From + "|";   // fromAddress

                    string sRcptNum = "";
                    string sRcptName = "";
                    foreach (string sRcpt in oMsg.Recipients)
                    {
                        sRcptNum = sRcptNum + sRcpt;
                        sRcptName = sRcptName + await PhoneNo2ContactName(sRcpt);
                    }

                    sTxt = sTxt + sRcptName + "|";   // from
                    sTxt = sTxt + sRcptNum + "|";   // fromAddress

                    try // 20180117: jakby LocalTimeStamp miał być null (np.)...
                    {
                        sTxt = sTxt + oMsg.LocalTimestamp.ToString("dd/MM/yyyy HH:mm:ss") + "|";
                    }
                    catch 
                    {
                        sTxt = sTxt + "|";
                    }// empty date

                    try // 20180117: jakby Body miał być null (np.)...
                    {
                        sTxt = sTxt + oMsg.Body.Replace("\n", "\\n");
                    }
                    catch 
                    {
                    }

#if NETFX_CORE
                    if (oMsg.MessageOperatorKind == Windows.ApplicationModel.Chat.ChatMessageOperatorKind.Mms)
                    {// 2020.10.28 - ale w Store nie zadziała!, zas na moim telefonie - moze sie uda
                     // This API is not available to all Windows/Windows Phone apps. Unless your developer account is specially provisioned by Microsoft, calls to these APIs will fail at runtime

                            try
                            {
                            if (oMsg.Attachments != null)   // to jeszze byc moze zawsze by zadziałało...
                            {
                                foreach (Windows.ApplicationModel.Chat.ChatMessageAttachment oAtt in oMsg.Attachments)
                                {
                                    string sTresc="";

                                    switch (oAtt.MimeType)
                                    {
                                        case "application/smil":
                                            // ignorujemy, choc mozna byloby czytac jak text/plain
                                            break;
                                        case "text/plain":
                                            { // to pozwala zdefiniowac oStream, oBuffer niezaleznie w kazdym CASE
                                                var oStream = (await oAtt.DataStreamReference.OpenReadAsync());
                                                var oBuffer = new Windows.Storage.Streams.Buffer((uint)oStream.Size + 10); // niby powinno wystarczyc...
                                                var oBuffOut = await oStream.ReadAsync(oBuffer, oBuffer.Capacity, Windows.Storage.Streams.InputStreamOptions.None).AsTask();
                                                var oMmsRdr = Windows.Storage.Streams.DataReader.FromBuffer(oBuffOut);
                                                oMmsRdr.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf16LE;
                                                sTresc = oMmsRdr.ReadString(oMmsRdr.UnconsumedBufferLength / 2); // z BYTE na UTF16
                                                oMmsRdr.Dispose();
                                                oStream.Dispose();
                                            }
                                            break;
                                        case "image/jpeg":
                                            {
                                                var oStream = (await oAtt.DataStreamReference.OpenReadAsync());
                                                var oBuffer = new Windows.Storage.Streams.Buffer((uint)oStream.Size + 10); // niby powinno wystarczyc...
                                                var oBuffOut = await oStream.ReadAsync(oBuffer, oBuffer.Capacity, Windows.Storage.Streams.InputStreamOptions.None).AsTask();

                                                
                                                string sFilename = iMMSattNum.ToString("000#") + ".jpg";
                                                iMMSattNum = iMMSattNum + 1;

                                                var oMMsFold = await oFold.CreateFolderAsync(sMMSattFolder, Windows.Storage.CreationCollisionOption.OpenIfExists);
                                                var oMmsFile = await oMMsFold?.CreateFileAsync(sFilename);
                                                await Windows.Storage.FileIO.WriteBufferAsync(oMmsFile, oBuffer);

                                                sTresc = " picture: " + sFilename + " ";

                                            }
                                            break;
                                        default:
                                            sTresc = " attachment, MIME: " + oAtt.MimeType + " FILENAME: " + oAtt.OriginalFileName;
                                            break;
                                    }
                                    sTxt = sTxt + "MMS: " + sTresc.Replace("\n", "\\n");

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //p.k.CrashMessageAdd("MMS",ex);
                        }



                    }
#endif

                    // <Message><Recepients /><Body>A jak chcesz spędzić ten czas.</Body><IsIncoming>true</IsIncoming><IsRead>true</IsRead><Attachments /><LocalTimestamp>131606927899116393</LocalTimestamp><Sender>+48531346962</Sender></Message>

                    // 20190825: sTxt += oMsg.Body + "\n"; // Constants.vbCrLf;
                    sTxt += "\n";
                    iRet++;

                    }
                    catch (Exception ex)
                    {

                        p.k.CrashMessageAdd("WyciagnijSMS - iterate batch", ex);
                    }

                } // foreach(msg in batch)

            } // while(read batch)

            if (bShowSince)
                p.k.SetSettingsInt("lastRunCnt", iGuard);

            if (uiMsgCnt != null)
                uiMsgCnt.Text = "Saving...";

            //p.k.CrashMessageAdd("saving...");

            string sFile = "SMS " + DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss") + ".csv";
            oLastExportFile = await oFold.CreateFileAsync(sFile, Windows.Storage.CreationCollisionOption.OpenIfExists);
            await Windows.Storage.FileIO.WriteTextAsync(oLastExportFile, sTxt);

            if (uiMsgCnt != null)
            {
                uiMsgCnt.Text = "Saved " + iRet.ToString(System.Globalization.CultureInfo.InvariantCulture) + " messages.";

                if (p.k.GetSettingsBool("noSDcard", true))
                { // gdy nie ma karty SD, to robimy Share
                    sLastExportFileContent = sTxt;
                    Windows.ApplicationModel.DataTransfer.DataTransferManager.GetForCurrentView().DataRequested += SzarnijDane;
                    Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI();
                }
            }

            return iRet;
            
        }

        private static void SzarnijDane(DataTransferManager sender, DataRequestedEventArgs args)
        {
            args.Request.Data.SetText(sLastExportFileContent);
            args.Request.Data.Properties.Title = Windows.ApplicationModel.Package.Current.DisplayName;
            sLastExportFileContent = "";
        }
    }
}



