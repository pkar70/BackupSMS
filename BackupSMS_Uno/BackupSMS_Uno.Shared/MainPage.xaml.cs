/*

STORE

2021.05.09
* export (z guziczków) jest albo na SD, albo do własnego - i wtedy jest Share
* po export zmienia się treść guziczka Since

2021.04.27
* TriggerPolnocny - z App.xaml.cs do pkmoduleshared (trochę inna logika działania, nie przeszkadza mu już PowerSaving mode)
* info o trigger bierze z trigger, a nie z Settings

STORE 10.2004

2020.03.11
* dodałem pkModuleShared, wyrzucając z App.cs swoje biblioteki (dialogi, settingsy)

TODO
 * reading SMS - albo nie ma Ring, albo nie przywraca guzikow po wczytaniu :)

 Dla Uno:
 * App.GetSDcardFolder()
 * ImportSMSow

 Version history:

2020.01.29
 * dodanie własnego cache Dictionary(numer,nazwa) - bo na Android to bardzo skomplikowane szukanie i szkoda czasu, może na Windows też przyspieszy
 * zamiana vbCrLf na "\n" (przy export, i analogicznie przy import)

2020.01.28
 * dodanie do Uno (i przetestowanie) wszystkiego związanego z Contact wykorzystywanego tutaj
  
 STORE 10.1909

 2019.08.25
 * migracja VB -> VC
 * dodawanie w nazwie pliku "since", "full"
 * po eksporcie - pokazuje ile było SMSów (w TextBox z komunikatami)
 * już nie podwaja tekstu msg przy export
 * import - albo wszystko, albo tylko te ktore sa pozniejsze niz poprzedni import
 *      [uwaga: na PC nie zwraca bledu, ale nie zapisuje do MessageStore]
 
  // * zapis na OneDrive, żeby było łatwiej importować?
 // * jeśli następna linia nie zaczyna sie od Inbox/Outbox to znaczy że w Msg była zmiana linii!

 
 STORE 1.1907.1, 2019.08.02

 v.1.1907:
 ProgressRing hides after creating backup

v.1.1904: (changed numbering scheme)
 added progressRing while processing
 small corrections in code
v1.2.5: app tests if SD card is accessible, and if not - disable actions
v1.2.3: one more runtime test
v1.2:
* added exporting 'since last export'
* exporting all SMS - it remembers how many batches of SMS it gets from system last time, and shows this info while exporting next time
* when app has no permission to contact, it appends '???' string as contact name, and not fail

  
  */

using System;
using System.Collections.Generic;
using System.IO;
//using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

#if __ANDROID__
using Chat = BeforeUno;
#else
using Chat = Windows.ApplicationModel.Chat;
#endif 


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BackupSMS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }


        // 2019.07.12 poprawka - wygasza ProgresRinga po zapisie


        #region "UIhandlers"
        private void GuzikiEnable(bool bIsEnabled, int iMax = 100)
        {
            uiReadDay.IsEnabled = bIsEnabled;
            uiReadAll.IsEnabled = bIsEnabled;
            uiReadSinceLast.IsEnabled = bIsEnabled;
            uiImport.IsEnabled = bIsEnabled;
            uiImportSinceLast.IsEnabled = bIsEnabled;

            p.k.ProgRingShow(!bIsEnabled, false, 0, iMax);
        }

        
        private async System.Threading.Tasks.Task<int> ExportSMSwrapper(DateTime oDate, bool bShowSince, string sSufix)
        {
            if (!await AndroidAskPermissiona()) return -1;

            GuzikiEnable(false);
            int iCnt = await App.WyciagnijSMS(oDate, false, bShowSince, uiMsgCnt, sSufix);
            uiMsgCnt.Text = "";
            if (bShowSince)
            {
                SetLastDate(false);
                UstawSince();
            }
            GuzikiEnable(true);
            return iCnt;
        }

        private async void uiRUn_Click(object sender, RoutedEventArgs e)
        {
            int iCnt = await ExportSMSwrapper(new DateTime(1999, 1, 1), true, "full");
            //if (iCnt > 0) App.DialogBox("Saved " + iCnt.ToString() + " messages.");
        }

        private async void uiRunDay_Click(object sender, RoutedEventArgs e)
        {
            DateTime oDate = DateTime.Now.AddDays(-1); // Hours((double)-DateTime.Now.Hour - 1);
            int iCnt = await ExportSMSwrapper(oDate, false, "");
        }

        private async void uiRunSince_Click(object sender, RoutedEventArgs e)
        {
            int iCnt = await ExportSMSwrapper(GetLastDate(false), false, "since");
            //if (iCnt > 0) App.DialogBox("Saved " + iCnt.ToString() + " messages.");
        }

        private async void uiAutoChange_Toggle(object sender, RoutedEventArgs e)
        { // obsluga timerowa i praca w tle tylko pod Windows - XAML ma win:

            // p.k.SetSettingsBool("autobackup", uiSwitch.IsOn);    - już nie stosujemy zmiennej
            if (uiSwitch.IsOn)
            {
                await AndroidAskPermissiona();     // bo musi zapytac przeciez z UI thread, nie dopiero o północy
                await p.k.DodajTriggerPolnocny();
            }
            else
            {
                p.k.UnregisterTriggers();   // wszystkie - bo i tak nie ma innych triggerów rejestrowanych
            }
        }

        private void SetLastDate(bool bImport)
        {
            string sPrefix = "";
            if (bImport) sPrefix = "import";

            p.k.SetSettingsInt(sPrefix + "lastYear", DateTime.Now.Year);
            p.k.SetSettingsInt(sPrefix + "lastMonth", DateTime.Now.Month);
            p.k.SetSettingsInt(sPrefix + "lastDay", DateTime.Now.Day);
        }

        private DateTime GetLastDate(bool bImport)
        {
            int iYr, iMn, iDy;
            string sPrefix = "";
            if (bImport) sPrefix = "import";

            iYr = p.k.GetSettingsInt(sPrefix + "lastYear");
            if (iYr < 2000)
                return new DateTime(2000, 1, 1);
            iMn = p.k.GetSettingsInt(sPrefix + "lastMonth");
            iDy = p.k.GetSettingsInt(sPrefix + "lastDay");

            return new DateTime(iYr, iMn, iDy);
        }


        private void UstawSince()
        {
            DateTime oDate = GetLastDate(false);

            if (oDate.Year < 2001)
                uiReadSinceLast.Visibility = Visibility.Collapsed;
            else
            {
                uiReadSinceLast.Visibility = Visibility.Visible;
                uiReadSinceLast.Content = "Since " + oDate.ToString("yy-MM-dd");
            }

            oDate = GetLastDate(true);

            if (oDate.Year < 2001)
                uiImportSinceLast.Visibility = Visibility.Collapsed;
            else
            {
                uiImportSinceLast.Visibility = Visibility.Visible;
                uiImportSinceLast.Content = "Since " + oDate.ToString("yy-MM-dd");
            }

        }

        private void uiClearLog_Click(object sender, RoutedEventArgs e)
        {
            p.k.SetSettingsString("internalog", "reset");
        }

        private void uiShowLog_Click(object sender, RoutedEventArgs e)
        {
            uiLog.Text = p.k.GetSettingsString("internalog");
        }

        private async void uiImport_Click(object sender, RoutedEventArgs e)
        {
            if (!await p.k.DialogBoxYNAsync("Are you sure? Import all SMS messages?")) return;

            GuzikiEnable(false);
            ImportSMSow(new DateTimeOffset(1990, 1, 1, 1, 1, 1, System.TimeSpan.FromSeconds(0)));
            SetLastDate(true);
            GuzikiEnable(true);
        }

        private void uiImportSince_Click(object sender, RoutedEventArgs e)
        {
            GuzikiEnable(false);
            ImportSMSow(GetLastDate(true));
            SetLastDate(true);
            GuzikiEnable(true);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            uiVers.Text = p.k.GetAppVers();
            p.k.ProgRingInit(true, true);

            // 2021.05: poprzednia logic: nie ma zapisywania gdy nie ma  karty SD
            // nowa logic: zapisywanie jest zawsze, najwyżej do folderu i Share
            //      przy braku SD nie ma tylko Timera

            {
                // w {}, żeby szybko zlikwidować oFold
                Windows.Storage.StorageFolder oFold = await p.k.GetLogFolderRootAsync(false);
                //if (App.GetSDcardFolder() == null)

                if (oFold is null)
                {
                    uiSwitch.Visibility = Visibility.Collapsed;
                    p.k.SetSettingsBool("noSDcard", true);
                    // uiMsgCnt.Text = "No SD card detected!";
                }
                else
                {
                    uiSwitch.Visibility = Visibility.Visible ;
                    p.k.SetSettingsBool("noSDcard", false);

                    uiSwitch.IsOn = p.k.IsTriggersRegistered();
                    if (!uiSwitch.IsOn)
                    {
                        if (p.k.GetSettingsBool("autobackup"))
                        {
                            // nie mamy triggera - ale może mieliśmy mieć, wedle starego Settings
                            uiSwitch.IsOn = true;
                            await p.k.DodajTriggerPolnocny();
                        }
                    }
                }
            }

            //uiAutoChange_Toggle(null, null);   // ustawianie triggera
            uiLog.Text = p.k.GetSettingsString("internalog");

            UstawSince();
#if DEBUG
            await p.k.CrashMessageShowAsync();
#endif

        }
#endregion

#region "Android specific"

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async System.Threading.Tasks.Task<bool> AndroidAskPermissiona()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
#if __ANDROID__

            // to co potrzebujemy
            List<string> requestPermission = new List<string>();
            requestPermission.Add(Android.Manifest.Permission.ReadSms);
            requestPermission.Add(Android.Manifest.Permission.ReadContacts);
            requestPermission.Add(Android.Manifest.Permission.WriteExternalStorage);

            return await BeforeUno.AndroidHelpers.AndroidPermissionAsync(requestPermission.ToArray(), null);
            //return await Windows.Extensions.PermissionsHelper.AndroidPermissionAsync(requestPermission.ToArray(), null);
#else
            return true;
#endif

        }

#if false
        private async System.Threading.Tasks.Task<bool> AndroidAskPermissionaOld()
        {
#if __ANDROID__

            // to co potrzebujemy
            List<string> requestPermission = new List<string>();
            requestPermission.Add(Android.Manifest.Permission.ReadSms);
            requestPermission.Add(Android.Manifest.Permission.ReadContacts);
            requestPermission.Add(Android.Manifest.Permission.WriteExternalStorage);

            // sprawdzamy czy mamy prawo...

            // najpierw w Manifest - brak to return false
            Android.Content.Context context = Android.App.Application.Context;
            Android.Content.PM.PackageInfo packageInfo =
                context.PackageManager.GetPackageInfo(context.PackageName, Android.Content.PM.PackageInfoFlags.Permissions);
            var manifestRequestedPermissions = packageInfo?.RequestedPermissions;
            if (manifestRequestedPermissions is null)
                return false;

            foreach (string permiss in requestPermission)
            {
                bool bInManifest = false;
                foreach (string oPerm in manifestRequestedPermissions)
                {
                    if (oPerm.Equals(permiss, StringComparison.OrdinalIgnoreCase))
                    {
                        bInManifest = true;
                        break;
                    }
                }

                if (!bInManifest)
                {
                    System.Diagnostics.Debug.Write("permission '" + permiss + "' not defined in Manifest");
                    return false;
                }
            }

            List<string> askForPermission = new List<string>();

            // check if permission is already granted
            foreach (string permiss in requestPermission)
            {
                if (AndroidX.Core.Content.ContextCompat.CheckSelfPermission(Uno.UI.ContextHelper.Current, permiss)
                    != Android.Content.PM.Permission.Granted)
                {
                    askForPermission.Add(permiss);
                }
            }

            if (askForPermission.Count < 1)
                return true;

            // system dialog asking for permission

            // this code would not compile here - but it compile in your own app.
            // to be compiled inside Uno, it has to be splitted into layers
            var tcs = new System.Threading.Tasks.TaskCompletionSource<Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs>();

            void handlerSMSperm(object sender, Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs e)
            {

                if (e.RequestCode == 1)
                {
                    tcs.TrySetResult(e);
                }
            }

            var current = Uno.UI.BaseActivity.Current;

            // ale moze nie mamy jak zapytać?
            if (current is null)
                return false;

            try
            {
                current.RequestPermissionsResultWithResults += handlerSMSperm;

                AndroidX.Core.App.ActivityCompat.RequestPermissions(Uno.UI.BaseActivity.Current, requestPermission.ToArray(), 1);

                var result = await tcs.Task;
                if (result.GrantResults.Length < 1)
                    return false;
                if (result.GrantResults[0] == Android.Content.PM.Permission.Granted)
                    return true;

            }
            finally
            {
                current.RequestPermissionsResultWithResults -= handlerSMSperm;
            }


            return false;

#else
            return true;
#endif

        }

        private async System.Threading.Tasks.Task<bool> AndroidDirInitOld()
        {
#if __ANDROID__
            var tcs = new System.Threading.Tasks.TaskCompletionSource<Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs>();

            void handler(object sender, Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs e)
            {

                if (e.RequestCode == 1)
                {
                    tcs.TrySetResult(e);
                }
            }

            var current = Uno.UI.BaseActivity.Current;
            try
            {
                current.RequestPermissionsResultWithResults += handler;

                AndroidX.Core.App.ActivityCompat.RequestPermissions(Uno.UI.BaseActivity.Current, new[] { Android.Manifest.Permission.WriteExternalStorage }, 1);

                var result = await tcs.Task;
                if (result.GrantResults.Length < 1)
                    return false;
                if (result.GrantResults[0] == Android.Content.PM.Permission.Granted)
                    return true;

            }
            finally
            {
                current.RequestPermissionsResultWithResults -= handler;
            }


            return false;
#else
            return true;
#endif
        }

        private static string defaultSmsApp = "";

        private async System.Threading.Tasks.Task<bool> SwitchSmsAppMain(string sNaAppName)
        {

            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Q)
            {
                // działa od API 19 do API 28, potem już nie
                // https://developer.android.com/reference/android/provider/Telephony.Sms.Intents
                var intent = new Android.Content.Intent(Android.Provider.Telephony.Sms.Intents.ActionChangeDefault);
                intent.PutExtra(Android.Provider.Telephony.Sms.Intents.ExtraPackageName, sNaAppName);

                await BeforeUno.AndroidHelpers.InvokeIntentAsync(intent);
            }
            else
            {
                string roleName = Android.App.Roles.RoleManager.RoleSms;
                // ale to ponizej nie dziala! Błąd w Xamarin??

                // var intent = Android.App.Roles.RoleManager.createRequestRoleIntent(roleName);
                //var intent = RequestRoleIntent(roleName);

                //.RoleSMS;
                //    .RequestRoleIntent(.Content.Intent(Android.Provider.Telephony.Sms.Intents.ActionChangeDefault);
                //intent.PutExtra(Android.Provider.Telephony.Sms.Intents.ExtraPackageName, sNaAppName);

                //await BeforeUno.AndroidHelpers.InvokeIntentAsync(intent);
            }

            // sprawdzenie rezultatu przełączenia
            var context = Android.App.Application.Context;
            string currSmsApp = Android.Provider.Telephony.Sms.GetDefaultSmsPackage(context);
            if (currSmsApp == sNaAppName)
                return true;

            return false; // zmiana nie zostala dokonana
        }

        private async System.Threading.Tasks.Task<bool> SwitchSmsApp(bool naMnie)
        {
#if __ANDROID__
            // https://android-developers.googleblog.com/2013/10/getting-your-sms-apps-ready-for-kitkat.html
            var context = Android.App.Application.Context;

            // potrzebna jest obsluga
            // only the app that receives the SMS_DELIVER_ACTION broadcast (the user-specified default SMS app) is able to write to the SMS Provider
            // tylko co w takim razie zrobic jak sie go tak naprawde nie obsluguje a przyjdzie SMS?

            if (naMnie)
            {
                var currSmsApp = Android.Provider.Telephony.Sms.GetDefaultSmsPackage(context);
                // jesli na siebie, to nie zmieniam - moze to dwa razy wywolanie?
                if (currSmsApp == context.PackageName)
                    return true;

                defaultSmsApp = currSmsApp;

                return await SwitchSmsAppMain(context.PackageName);


                // context.StartActivity(intent);
                // https://developer.android.com/guide/topics/permissions/default-handlers#java
                // albo: startActivityForResult(setSmsAppIntent, your-result-code);
                // wszystkie cztery trzeba zdefiniowac!
                //https://stackoverflow.com/questions/21720657/how-to-set-my-sms-app-default-in-android-kitkat
            }
            else
            {
                if (string.IsNullOrEmpty(defaultSmsApp))
                    return false;   // error: nie ma na co zmieniac
                if (defaultSmsApp == context.PackageName)
                    return true;    // ze niby zmieniac na samego siebie - czyli jest jak trzeba

                return await SwitchSmsAppMain(defaultSmsApp);

            }
#else
            return true;
#endif
        }
#endif
        #endregion

        private async void ImportSMSow(DateTimeOffset oDTsince)
        {
            var retval = await AndroidAskPermissiona();
            if (!retval) return;

            Windows.ApplicationModel.Chat.ChatMessageStore oStore = await Windows.ApplicationModel.Chat.ChatMessageManager.RequestStoreAsync();
            if (oStore == null)
            {
                p.k.DialogBox("No permission (or this is not a phone)");
                return;
            }

            oDTsince = oDTsince.AddMinutes(10);  // musi byc czas na przekopiowanie pliku :)

            int iImportLimit = 200;

            // browse plik

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".csv");

            Windows.Storage.StorageFile oFile;
            oFile = await picker.PickSingleFileAsync();
            if (oFile == null)
                return;

            string sTxt = await Windows.Storage.FileIO.ReadTextAsync(oFile);
            string[] aLinie = sTxt.Split('\n');
            uiMsgCnt.Text = "Read " + aLinie.Length + " messages.";

#if __ANDROID__


            // Inbox|Wojciech Lewandowski|+48601446602|||17/08/2019 10:24:32|Od 22 do 31 sierpnia w tym terminie 7 dniOd 22 do 31 sierpnia w tym terminie 7 dni
            // Outbox|||Kasia18|+48668454898|15/08/2019 22:31:14|Masz juz umowionego penisa na niedzielne popołudnie i noc na poniedziałek?Masz juz umowionego penisa na niedzielne popołudnie i noc na poniedziałek?
            if (!await oStore.SwitchDefaultSMSapp(true)) return;
            //if(!await SwitchSmsApp(true)) return;
#endif


            uiMsgCnt.Text = "Please wait, importing " + aLinie.Length + " messages...";
            GuzikiEnable(false, aLinie.Length);
            int iCntAll = 0;
            int iCntAdd = 0;

            foreach (string sLinia in aLinie)
            {
                string[] aArr = sLinia.Trim().Split('|');

                if (aArr.GetUpperBound(0) != 6) continue; // error
                if ((aArr[0] != "Inbox") && (aArr[0] != "Outbox"))  continue; // error (np. kontynuacja po zmianie linii - poprzednia wersja export
                if (aArr[5].Length != 19)   continue; // error

                Windows.ApplicationModel.Chat.ChatMessage oMsg;
                oMsg = new Windows.ApplicationModel.Chat.ChatMessage();
                oMsg.MessageKind = Windows.ApplicationModel.Chat.ChatMessageKind.Standard;
                oMsg.MessageOperatorKind = Windows.ApplicationModel.Chat.ChatMessageOperatorKind.Sms;


                if (aArr[0] == "Inbox")
                {
                    oMsg.IsIncoming = true;
                    // aArr[1]: nazwa OD
                    oMsg.From = aArr[2]; /// numer OD
                    oMsg.IsRead = true;
                    oMsg.IsSeen = true;
                    oMsg.Status = Windows.ApplicationModel.Chat.ChatMessageStatus.Received;
                }
                else
                {
                    oMsg.IsIncoming = false;
                    // aArr[3]: sklejone nazwy DO
                    // aArr[4]: sklejone numery DO
                    oMsg.Recipients.Add(aArr[4]);
                    oMsg.Status = Windows.ApplicationModel.Chat.ChatMessageStatus.Sent;
                }

                // aArr[5]: data
                int iD, iM, iY, iH, iMn, iS;
                if(!int.TryParse(aArr[5].Substring(0, 2), out iD)) iD=1;
                if (!int.TryParse(aArr[5].Substring(3, 2), out iM)) iM = 1;
                if (!int.TryParse(aArr[5].Substring(6, 4), out iY)) iY = 2019;
                if (!int.TryParse(aArr[5].Substring(11, 2), out iH)) iH = 0;
                if (!int.TryParse(aArr[5].Substring(14, 2), out iMn)) iMn = 0;
                if (!int.TryParse(aArr[5].Substring(17, 2), out iS)) iS = 0;
                oMsg.LocalTimestamp = new DateTimeOffset(iY, iM, iD, iH, iMn, iS, System.TimeSpan.FromSeconds(0));
                oMsg.NetworkTimestamp = new DateTimeOffset(iY, iM, iD, iH, iMn, iS, System.TimeSpan.FromSeconds(0));

                // aArr[6]: body
                // ominięcie błędu w wersji < 1908: powtarzało przy export sBody
                iD = aArr[6].Length / 2;
                if (aArr[6].Substring(0, iD) == aArr[6].Substring(iD, iD))
                    oMsg.Body = aArr[6].Substring(0, iD);
                else
                    oMsg.Body = aArr[6];
                oMsg.Body = oMsg.Body.Replace("\\n", "\n");

                iCntAll++;
                p.k.ProgRingInc();

                // ale wstawiamy tylko wtedy gdy jest data taka jak trzeba ("since")
                if (oDTsince < oMsg.LocalTimestamp)
                {
                    await oStore.SaveMessageAsync(oMsg);
                    iCntAdd++;
                }

#if DEBUG
                iImportLimit--;
                if (iImportLimit < 1) break;
#endif
            }

                GuzikiEnable(true);
            uiMsgCnt.Text = "";
            p.k.DialogBox("Imported " + iCntAdd + " messages (out of " + iCntAll + ")");

#if __ANDROID__
            await oStore.SwitchDefaultSMSapp(false);
#endif

        }


    }
}
