using System;
//using System.Collections.Generic;
//using System.IO;
using System.Linq;
//using System.Runtime.InteropServices.WindowsRuntime;
//using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
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


        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

#if NETFX_CORE
            if (e != null && e.PrelaunchActivated == true) return;
#endif

            //if (e.PrelaunchActivated == false)
            //{
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            //}
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


        #endregion

        private static void IntLogAppend(string sStr)
        {
            System.Diagnostics.Debug.Write("PKAR " + sStr);
        }



        public static async System.Threading.Tasks.Task DodajTriggerPolnocny()
        {

            IntLogAppend("DodajTriggerPolnocny - START");
            Windows.ApplicationModel.Background.BackgroundAccessStatus oBAS;
            oBAS = await Windows.ApplicationModel.Background.BackgroundExecutionManager.RequestAccessAsync();

            if ((oBAS == Windows.ApplicationModel.Background.BackgroundAccessStatus.AlwaysAllowed) | 
                (oBAS == Windows.ApplicationModel.Background.BackgroundAccessStatus.AllowedSubjectToSystemPolicy))
            {
                // ' https://docs.microsoft.com/en-us/windows/uwp/launch-resume/create-And-register-an-inproc-background-task

                // IntLogAppend("DTS - removing tasks")
                // po co, skoro OneShot?
                // For Each oTask In BackgroundTaskRegistration.AllTasks
                // If oTask.Value.Name = "PKARsmsBackup_Daily" Then oTask.Value.Unregister(True)
                // Next

                IntLogAppend("DTS - building task");
                Windows.ApplicationModel.Background.BackgroundTaskBuilder builder = new Windows.ApplicationModel.Background.BackgroundTaskBuilder();
                Windows.ApplicationModel.Background.BackgroundTaskRegistration oRet;

                IntLogAppend("DTS - calculating mins");
                // Dim oDate1 = Date.Now.AddHours(1).AddDays(1)    ' 1h - zeby na nastepną dobę, +1 dzien (bez +1h by trafiał na 20 minut pozniej!)
                // Dim oDate0 = New Date(oDate1.Year, oDate1.Month, oDate1.Day)    ' polnoc
                // oDate0 = oDate0.AddMinutes(-20)
                // Dim oDate0 = New Date(oDate1.Year, oDate1.Month, oDate1.Day)    ' polnoc
                DateTime oDateMew;
                if (DateTime.Now.Hour > 20)
                    oDateMew = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 40, 0).AddDays((double)1);
                else
                    oDateMew = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 40, 0);// DZIS

                int iMin = (int)(oDateMew - DateTime.Now).TotalMinutes;
                IntLogAppend(" waiting mins:" + iMin.ToString(System.Globalization.CultureInfo.InvariantCulture));
                // Dim iMin = (24 * 60) - 20    ' 24 godziny po 60 minut bez 20 minut; czyli czas uruchomienia
                // iMin -= Date.Now.Hour() * 60  ' odjąć aktualny czas
                // iMin -= Date.Now.Minute()

                builder.SetTrigger(new Windows.ApplicationModel.Background.TimeTrigger((uint)iMin, true));
                builder.Name = "PKARsmsBackup_Daily";
                oRet = builder.Register();
            }
            else
                IntLogAppend("DTS - oBAS.Status = " + oBAS.ToString());
            // return default(Task);

        }

#if NETFX_CORE
        private Windows.ApplicationModel.Background.BackgroundTaskDeferral moTimerDeferal = null;
#endif

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
#if NETFX_CORE
            moTimerDeferal = args.TaskInstance.GetDeferral();
#endif
            IntLogAppend("OnBackActiv - START");
            DateTime oDate = DateTime.Now.AddHours((double)-DateTime.Now.Hour - 1);
            await WyciagnijSMS(oDate, true, false, null, "");
            await DodajTriggerPolnocny();
#if NETFX_CORE
            moTimerDeferal.Complete();
#endif

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


        public static async System.Threading.Tasks.Task<int> WyciagnijSMS(DateTime oDate, bool bInTimer, bool bShowSince, TextBlock uiMsgCnt, string sSufix)
        {
            IntLogAppend("WyciagnijSMS - START");
            // Dim oTextBox As TextBlock = Nothing

            // Try

            // If Not bInTimer Then
            // IntLogAppend("WSMS - not timer")
            // ' znajdz control o nazwie uiMsgCnt
            // Dim oStackPanel As StackPanel = TryCast(TryCast(TryCast(Window.Current.Content, Frame).Content.Content, Grid).Children(0), StackPanel)
            // For Each oChld As UIElement In oStackPanel.Children
            // Dim oTmp As TextBlock = TryCast(oChld, TextBlock)
            // If oTmp IsNot Nothing Then
            // If oTmp.Name = "uiMsgCnt" Then
            // oTextBox = oTmp
            // Exit For
            // End If
            // End If
            // Next
            // End If
            // Catch ex As Exception
            // ' w razie błędu oTextBox bedzie = Nothing, ale nie wyleci program
            // End Try

            // If uiProcesuje IsNot Nothing Then
            // uiProcesuje.Visibility = Visibility.Visible
            // uiProcesuje.IsActive = True
            // End If

            int iRet = -1;

            bool bError = false;
#if NETFX_CORE
            Windows.ApplicationModel.Chat.ChatMessageReader oRdr = null;
#else
            BeforeUno.ChatMessageReader oRdr = null;
#endif 
            try
            {
#if NETFX_CORE
                Windows.ApplicationModel.Chat.ChatMessageStore oStore = await Windows.ApplicationModel.Chat.ChatMessageManager.RequestStoreAsync();
#else
                BeforeUno.ChatMessageStore oStore = await BeforeUno.ChatMessageManager.RequestStoreAsync();
#endif
                if (oStore == null)
                {
                    if (uiMsgCnt != null) p.k.DialogBox("No permission (or this is not a phone)");
                    return -1;
                }

                IntLogAppend("WSMS - got oStore");
                
                oRdr = oStore.GetMessageReader();
                IntLogAppend("WSMS - got oRdr");
            }
            catch 
            {
                bError = true;
            }

            if (bError)
            {
                if (uiMsgCnt != null)
                    uiMsgCnt.Text = "ERROR - check permissions?";
                // If uiProcesuje IsNot Nothing Then
                // uiProcesuje.Visibility = Visibility.Visible
                // uiProcesuje.IsActive = True
                // End If
                return -1;
            }

            string sTxt = "";
            int iGuard = 0;

            int iLastRunCnt = p.k.GetSettingsInt("lastRunCnt");
            string sLastRun = "";
            if (bShowSince && iLastRunCnt > 0)
                sLastRun = " (/" + iLastRunCnt.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";


            while (iGuard < 10000)
            {
                iGuard = iGuard + 1;
                if (uiMsgCnt != null)
                {
                    string sTmp = "batch " + iGuard.ToString(System.Globalization.CultureInfo.InvariantCulture) + sLastRun;
                    uiMsgCnt.Text = sTmp;
                }
                IntLogAppend("WSMS - loop, iGuard=" + iGuard);

                System.Collections.Generic.IReadOnlyList<Windows.ApplicationModel.Chat.ChatMessage> oMsgList; // = default(IReadOnlyList<ChatMessage>);
                try     // Try dodane 20190223
                {
                    oMsgList = await oRdr.ReadBatchAsync();
                }
                catch 
                {
                    if (uiMsgCnt != null)
                        uiMsgCnt.Text = "ERROR - check permissions?";
                    // If uiProcesuje IsNot Nothing Then
                    // uiProcesuje.Visibility = Visibility.Visible
                    // uiProcesuje.IsActive = True
                    // End If
                    return -1;
                }

                if (oMsgList.Count < 1)
                    break;
                // Folder	From	FromAddress	To	ToAddress	Date	Message
                foreach (Windows.ApplicationModel.Chat.ChatMessage oMsg in oMsgList)
                {
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

                    // <Message><Recepients /><Body>A jak chcesz spędzić ten czas.</Body><IsIncoming>true</IsIncoming><IsRead>true</IsRead><Attachments /><LocalTimestamp>131606927899116393</LocalTimestamp><Sender>+48531346962</Sender></Message>

                    // 20190825: sTxt += oMsg.Body + "\n"; // Constants.vbCrLf;
                    sTxt += "\n";
                    iRet++;

                    if (oMsg.LocalTimestamp < oDate)
                        break;
                }
            }

            if (bShowSince)
                p.k.SetSettingsInt("lastRunCnt", iGuard);

            if (uiMsgCnt != null)
                uiMsgCnt.Text = "Saving...";

            Windows.Storage.StorageFolder sdCard = await GetSDcardFolder();

            if (sdCard == null)
            {
                if (uiMsgCnt != null)
                    uiMsgCnt.Text = "Cannot save - no SD card?";
                // If uiProcesuje IsNot Nothing Then
                // uiProcesuje.Visibility = Visibility.Visible
                // uiProcesuje.IsActive = True
                // End If
                return -1;    // error - nie ma karty
            }

            Windows.Storage.StorageFolder oFold = await sdCard.CreateFolderAsync("DataLogs", Windows.Storage.CreationCollisionOption.OpenIfExists);
            if (oFold == null)
                return -1;
            oFold = await oFold.CreateFolderAsync("BackupSMS", Windows.Storage.CreationCollisionOption.OpenIfExists);
            if (oFold == null)
                return -1;
            oFold = await oFold.CreateFolderAsync(DateTime.Now.ToString("yyyy"), Windows.Storage.CreationCollisionOption.OpenIfExists);
            if (oFold == null)
                return -1;
            oFold = await oFold.CreateFolderAsync(DateTime.Now.ToString("MM"), Windows.Storage.CreationCollisionOption.OpenIfExists);
            if (oFold == null)
                return -1;

            string sFile = "SMS " + DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss") + ".csv";
            Windows.Storage.StorageFile oFile = await oFold.CreateFileAsync(sFile, Windows.Storage.CreationCollisionOption.OpenIfExists);
            await Windows.Storage.FileIO.WriteTextAsync(oFile, sTxt);

            if (uiMsgCnt != null)
                uiMsgCnt.Text = "Saved " + iRet.ToString(System.Globalization.CultureInfo.InvariantCulture) + " messages.";

            return iRet;
            //return default(Task);
        }
    }
}



