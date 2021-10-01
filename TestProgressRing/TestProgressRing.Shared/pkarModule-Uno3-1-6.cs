using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using System.Linq;

// wersja dla Uno 3.1.6

/*
  
SYNC z VB, 2021.04.15

SYNC z VB, 2021.01.28 
  
2020.11.08
* Storage(File|Folder).(Write|Read)AllTextAsync także dla Android (bo korzysta z tego AndroUWP)
 
2020.11.06
 * triggers: IsTriggersRegistered, UnregisterTriggers, CanRegisterTriggersAsync, RegisterTimerTrigger, RegisterToastTrigger
2020.11.04
* Get/SetSettingsBool dla AppBarToggleButton (analogicznie jak CheckBox)

2020.11.02
* #if _PK_NUMBOX_ .. - Settingsy dla NumberBoxa (WinUI 2.3+)
* GPSdistance na Windows.Devices.Geolocation.BasicGeoposition.DistanceTo(BasicGeoposition)
* wkopiowane double ParseDouble() [z ZrodloBase]


2020.10.29
* uwspólnienie jako _mojeSuby\pkarModule-Uno3-1-6.cs
* mklink pkModuleShared.cs ..\..\..\_mojeSuby\pkarModule-Uno3-1-6.cs
* sync ze wszystkich pkModuleShared.cs (tu jest wszystko, co było w którejkolwiek wersji)
* wyrzucam "starocie" (przedtem tylko komentowane było)
* sync z uwspólnioną pkModule.vb (głównie Extensions, oraz ProgRing/Bar)

2020.10.27
* kolejne uproszczenia w związku z Uno 3.1.6

2020.08.27
* skoro zmiana Uno.UI, to wycinam #if tam gdzie to możliwe (IsMobile, ClipGet)
 
2019.12.22
 * skoro Uno.UI na nowym base, to GetAppVersion oraz OpenBrowser mogą być przywrócone
  
2019.09.10
 * nowa funkcja: GetPlatform (android, uwp, ios, wasm, other) - także jako bool, int, string
    
2019.09.03
 * dodałem MakeToast (dzięki Nugetowi)
 * .. dzięki czemu dodałem CrashMessageAdd
 * dodałem CrashMessageExit
    
2019.08.31
 * włączyłem pełną code analysis, i dodałem:
    * .ConfigureAwait(true) [czyli default, można byłoby zablokować ten warning]
    * ToString i int.Parse (i podobne): cultureinvariant, albo currentculture
    * trochę testów na null 

 2019.08.27
* nowa funkcja: string GetAppVers() - działa teoretycznie dla UWP, Android oraz iOS
* przerobienie ClipBoard na wersje uniwersalną (wymaga Nugeta)
* IsMobile: UWP jak do tej pory, macOS zawsze NIE, reszta (Android, iOS, WASM) zawsze TAK
* zmiana sposobu uzyskiwania nazwy komputera (może za to uniwersalna)

 2019.08.26
* migracja do VC
* wykomentowanie tego co nie jest crossplatform (przerzucenie tego do pkarmodule.cs w UWP)

*/


/*
 
    Wersje minimalne:

    UWP:
        Lumia 532: 15063.1805, 1703
        Lumia 650: 15254.582, 1709
        Lumia Aska: 14393, 1607
        Lumia mama: 15063, 1703
    Android:
        Aśki tablet: Android 6.0, Marshmallow, API level 23

*/

// do Strings:
// "errAnyError", resDlgYes, resDlgNo

namespace p
{
    public static partial class k
    {
        #region "CrashMessage"

        private static string mHostName = "";
        private static string mAppName = "";

        /// <summary>Inicjalizacja modułu - nazwa app, oraz hostname</summary>
        public static void CrashMessageInit()
        {
            if (string.IsNullOrEmpty(mHostName)) mHostName = GetHostName();
            if (string.IsNullOrEmpty(mAppName)) mAppName = Windows.ApplicationModel.Package.Current.DisplayName;
        }

        /// <summary>DialogBox z dotychczasowym logiem i skasowanie logu</summary>
        public async static System.Threading.Tasks.Task CrashMessageShowAsync()
        {
            CrashMessageInit();
            string sTxt = GetSettingsString("appFailData");
            if (string.IsNullOrEmpty(sTxt))
                return;
            await DialogBoxAsync(mAppName + " FAIL message:\n" + sTxt).ConfigureAwait(true);
            SetSettingsString("appFailData", "");
        }


        /// <summary>
        ///      Dodaj do logu,
        ///      gdy debug to pokaż toast i wyślij DebugOut,
        ///      gdy release to toast gdy GetSettingsBool("crashShowToast") 
        ///      </summary>
        public static void CrashMessageAdd(string sTxt)
        {
            CrashMessageInit();
            string sAdd = DateTime.Now.ToString("HH:mm:ss") + " " + sTxt + "\n";
#if DEBUG
            // linia z MyCameras - Toast replikowany, więc powinien podać z którego telefonu :)
            MakeToast(mAppName + "@" + mHostName + ", " + DateTime.Now.ToString("HH:mm:ss"), sTxt);
            DebugOut(sAdd);
#else
            if (GetSettingsBool("crashShowToast")) MakeToast(sAdd);
#endif
            SetSettingsString("appFailData", GetSettingsString("appFailData") + sAdd);
        }

        /// <summary>
        ///      Dodaj do logu,
        ///      gdy debug to pokaż toast i wyślij DebugOut,
        ///      gdy release to toast gdy GetSettingsBool("crashShowToast") 
        ///      </summary>
        public static void CrashMessageAdd(string sTxt, string exMsg)
        {
            CrashMessageAdd(sTxt + "\n" + exMsg);
        }

        // wersja w MyCameras nie miała optional ze stack
        /// <summary>
        ///      Dodaj do logu,
        ///      gdy debug to pokaż toast i wyślij DebugOut,
        ///      gdy release to toast gdy GetSettingsBool("crashShowToast") 
        ///      </summary>
        public static void CrashMessageAdd(string sTxt, Exception ex, bool bWithStack = false)
        {
            string sMsg = ex.Message;
            if (bWithStack && (!string.IsNullOrEmpty(ex.StackTrace)))
                sMsg = sMsg + "\n" + ex.StackTrace;

            CrashMessageAdd(sTxt, sMsg);
        }

        /// <summary>
        ///      Dodaj do logu, ewentualnie toast, i zakończ App
        ///      </summary>
        public static void CrashMessageExit(string sTxt, string exMsg)
        {
            CrashMessageAdd(sTxt, exMsg);
#if NETFX_CORE || __ANDROID__
            Windows.UI.Xaml.Application.Current.Exit();
#elif __IOS__
            System.Threading.Thread.CurrentThread.Abort();
#endif
        }

        /// <summary>
        ///      Wyślij DebugOut dodając prefix --PKAR-- (do łatwiejszego znajdywania w logu)
        ///      </summary>
        public static void DebugOut(string sMsg)
        {
            System.Diagnostics.Debug.WriteLine("--PKAR---:    " + sMsg);
        }

        /// <summary>
        ///      Wyślij via DebugOut, a takze albo pokaż dialog albo zrób toast
        ///      </summary>
        public static void ToastOrDialog(bool bDialog, string sMsg)
        {
            DebugOut(sMsg);
            if (bDialog)
                DialogBox(sMsg);
            else
                MakeToast(sMsg);
        }

        /// <summary>
        ///      Wyślij via DebugOut, a takze albo pokaż dialog albo zrób toast - z czekaniem
        ///      </summary>
        public async static System.Threading.Tasks.Task ToastOrDialogAsync(bool bDialog, string sMsg)
        {
            DebugOut(sMsg);
            if (bDialog)
                await DialogBoxAsync(sMsg);
            else
                MakeToast(sMsg);
        }


        #endregion

        #region "Clipboard"
        // -- CLIPBOARD ---------------------------------------------

        public static void ClipPut(string sTxt)
        {
            Windows.ApplicationModel.DataTransfer.DataPackage oClipCont = new Windows.ApplicationModel.DataTransfer.DataPackage
            {
                RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy
            };
            oClipCont.SetText(sTxt);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(oClipCont);
        }

        public static void ClipPutHtml(string sHtml)
        {
            Windows.ApplicationModel.DataTransfer.DataPackage oClipCont = new Windows.ApplicationModel.DataTransfer.DataPackage();
            oClipCont.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            oClipCont.SetHtmlFormat(sHtml);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(oClipCont);
        }


        /// <summary>
        ///      w razie Catch() zwraca ""
        ///      </summary>
        public async static System.Threading.Tasks.Task<string> ClipGetAsync()
        {
            try
            {
                Windows.ApplicationModel.DataTransfer.DataPackageView oClipCont = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent(); //< --nie ma w UNO
                return await oClipCont.GetTextAsync();
            }
            catch
            {
            }
            return "";
        }
        #endregion

        #region "Get/Set Settings"
        // -- Get/Set Settings ---------------------------------------------

        #region "string"

        // odwołanie się do zmiennych
        public static string GetSettingsString(string sName, string sDefault = "")
        {
            string sTmp;
            sTmp = sDefault;

            //if (Acr.Settings.CrossSettings.Current.Contains(sName))
            //    sTmp = Acr.Settings.CrossSettings.Current.Get<string>(sName);
            if (Windows.Storage.ApplicationData.Current.RoamingSettings.Values.ContainsKey(sName))
                sTmp = Windows.Storage.ApplicationData.Current.RoamingSettings.Values[sName].ToString();
            if (Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey(sName))
                sTmp = Windows.Storage.ApplicationData.Current.LocalSettings.Values[sName].ToString();

            return sTmp;
        }

        public static void SetSettingsString(string sName, string sValue, bool bRoam)
        {
            if (bRoam)
            {
                Windows.Storage.ApplicationData.Current.RoamingSettings.Values[sName] = sValue;
                //Acr.Settings.CrossSettings.Current.SetValue(sName, sValue);
            }
            Windows.Storage.ApplicationData.Current.LocalSettings.Values[sName] = sValue;
        }

        // obsługa ekranowa i inne typ podobne
        public static string GetSettingsString(Windows.UI.Xaml.Controls.TextBlock oTBox, string sName, string sDefault = "")
        {
            if (oTBox is null) return "";
            string sTmp = GetSettingsString(sName, sDefault);
            oTBox.Text = sTmp;
            return sTmp;
        }
        // to wyzej: textBLOCK, to nizej: textBOX
        public static string GetSettingsString(Windows.UI.Xaml.Controls.TextBox oTBox, string sName, string sDefault = "")
        {
            if (oTBox is null) return "";
            string sTmp = GetSettingsString(sName, sDefault);
            oTBox.Text = sTmp;
            return sTmp;
        }



        public static void SetSettingsString(string sName, string sValue)
        {
            SetSettingsString(sName, sValue, false);
        }


        public static void SetSettingsString(string sName, Windows.UI.Xaml.Controls.TextBox sValue, bool bRoam)
        {
            if (sValue is null) return;
            SetSettingsString(sName, sValue.Text, bRoam);
        }

        public static void SetSettingsString(string sName, Windows.UI.Xaml.Controls.TextBox sValue)
        {
            if (sValue is null) return;
            SetSettingsString(sName, sValue.Text, false);
        }
        public static void SetSettingsString(Windows.UI.Xaml.Controls.TextBox sValue, string sName)
        {
            if (sValue is null) return;
            SetSettingsString(sName, sValue.Text, false);
        }

#if _PK_NUMBOX_
        public static string GetSettingsString(Microsoft.UI.Xaml.Controls.NumberBox oNumBox, string sName, string sDefault = "")
        {
            if (oNumBox is null) return sDefault;
            string sTmp = GetSettingsString(sName, sDefault);
            oNumBox.Text = sTmp;
            return sTmp;
        }
        public static void SetSettingsString(Microsoft.UI.Xaml.Controls.NumberBox oNumBox, string sName, bool bRoam = false)
        {
            if (oNumBox is null) return;
            string sVal = oNumBox.Text;
            SetSettingsString(sName, sVal, bRoam);
        }


#endif
        #endregion
        #region "int"

        public static int GetSettingsInt(string sName, int iDefault = 0)
        {
            int sTmp;

            sTmp = iDefault;

            {
                var withBlock = Windows.Storage.ApplicationData.Current;
                if (withBlock.RoamingSettings.Values.ContainsKey(sName))
                    sTmp = System.Convert.ToInt32(withBlock.RoamingSettings.Values[sName].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                if (withBlock.LocalSettings.Values.ContainsKey(sName))
                    sTmp = System.Convert.ToInt32(withBlock.LocalSettings.Values[sName].ToString(), System.Globalization.CultureInfo.InvariantCulture);
            }

            return sTmp;
        }

        public static void SetSettingsInt(string sName, int sValue)
        {
            SetSettingsInt(sName, sValue, false);
        }

        public static void SetSettingsInt(string sName, int sValue, bool bRoam)
        {
            {
                var withBlock = Windows.Storage.ApplicationData.Current;
                if (bRoam)
                    withBlock.RoamingSettings.Values[sName] = sValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                withBlock.LocalSettings.Values[sName] = sValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public static void SetSettingsInt(string sName, double dValue)
        {
            SetSettingsInt(sName, (int)dValue, false);
        }

        public static void SetSettingsInt(string sName, double dValue, bool bRoam)
        {
            SetSettingsInt(sName, (int)dValue, bRoam);
        }


#if _PK_NUMBOX_
        public static int GetSettingsInt(Microsoft.UI.Xaml.Controls.NumberBox oNumBox, string sName, int iDzielnik = 1, int iDefault = 0)
        {
            if (oNumBox is null) return iDefault;
            int iTmp = GetSettingsInt(sName, iDefault);
            oNumBox.Value = iTmp / iDzielnik;
            return iTmp;
        }
        public static void SetSettingsInt(Microsoft.UI.Xaml.Controls.NumberBox oNumBox, string sName, int iMnoznik = 1, bool bRoam = false)
        {
            if (oNumBox is null) return;
            double dVal = oNumBox.Value;
            if (double.IsNaN(dVal)) dVal = 0;
            int iVal = (int)(dVal * iMnoznik);
            SetSettingsInt(sName, iVal, bRoam);
        }


#endif

        #endregion

        #region "bool"
        public static bool GetSettingsBool(string sName, bool iDefault = false)
        {
            bool sTmp;

            sTmp = iDefault;
            {
                var withBlock = Windows.Storage.ApplicationData.Current;
                if (withBlock.RoamingSettings.Values.ContainsKey(sName))
                    sTmp = System.Convert.ToBoolean(withBlock.RoamingSettings.Values[sName].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                if (withBlock.LocalSettings.Values.ContainsKey(sName))
                    sTmp = System.Convert.ToBoolean(withBlock.LocalSettings.Values[sName].ToString(), System.Globalization.CultureInfo.InvariantCulture);
            }

            return sTmp;
        }

        public static bool GetSettingsBool(Windows.UI.Xaml.Controls.ToggleSwitch oSwitch, bool iDefault = false)
        {
            bool sTmp;
            sTmp = GetSettingsBool(oSwitch.Name, iDefault);
            oSwitch.IsOn = sTmp;
            return sTmp;
        }
        public static bool GetSettingsBool(Windows.UI.Xaml.Controls.Primitives.ToggleButton oSwitch, bool iDefault = false)
        {
            bool sTmp;
            sTmp = GetSettingsBool(oSwitch.Name, iDefault);
            oSwitch.IsChecked = sTmp;
            return sTmp;
        }

        public static bool GetSettingsBool(Windows.UI.Xaml.Controls.ToggleSwitch oSwitch, string sName, bool iDefault = false)
        {
            if (oSwitch is null) return iDefault;
            bool sTmp;
            sTmp = GetSettingsBool(sName, iDefault);
            oSwitch.IsOn = sTmp;
            return sTmp;
        }
        public static bool GetSettingsBool(Windows.UI.Xaml.Controls.AppBarToggleButton oSwitch, string sName, bool iDefault = false)
        {
            if (oSwitch is null) return iDefault;
            bool sTmp;
            sTmp = GetSettingsBool(sName, iDefault);
            oSwitch.IsChecked = sTmp;
            return sTmp;
        }

        public static void SetSettingsBool(string sName, bool sValue)
        {
            SetSettingsBool(sName, sValue, false);
        }

        public static void SetSettingsBool(string sName, bool sValue, bool bRoam)
        {
            {
                var withBlock = Windows.Storage.ApplicationData.Current;
                if (bRoam)
                    withBlock.RoamingSettings.Values[sName] = sValue.ToString();
                withBlock.LocalSettings.Values[sName] = sValue.ToString();
            }
        }

        public static void SetSettingsBool(string sName, bool? sValue, bool bRoam = false)
        {
            if (sValue.HasValue && sValue.Value)
                SetSettingsBool(sName, true, bRoam);
            else
                SetSettingsBool(sName, false, bRoam);
        }

        public static void SetSettingsBool(Windows.UI.Xaml.Controls.ToggleSwitch sValue, string sName, bool bRoam = false)
        {
            if (sValue is null) return;
            SetSettingsBool(sName, sValue.IsOn, bRoam);
        }
        public static void SetSettingsBool(Windows.UI.Xaml.Controls.AppBarToggleButton sValue, string sName, bool bRoam = false)
        {
            if (sValue is null) return;
            SetSettingsBool(sName, sValue.IsChecked, bRoam);
        }

        public static void SetSettingsBool(string sName, Windows.UI.Xaml.Controls.ToggleSwitch sValue, bool bRoam)
        {
            if (sValue is null) return;
            SetSettingsBool(sName, sValue.IsOn, bRoam);
        }

        public static void SetSettingsBool(Windows.UI.Xaml.Controls.ToggleSwitch sValue, bool bRoam = false)
        {
            SetSettingsBool(sValue.Name, sValue.IsOn, bRoam);
        }
        public static void SetSettingsBool(Windows.UI.Xaml.Controls.Primitives.ToggleButton sValue, bool bRoam = false)
        {
            SetSettingsBool(sValue.Name, sValue.IsChecked, bRoam);
        }


        public static void SetSettingsBool(string sName, Windows.UI.Xaml.Controls.ToggleSwitch sValue)
        {
            if (sValue is null) return;
            SetSettingsBool(sName, sValue.IsOn, false);
        }
        #endregion
        #region "Date"
        public static void SetSettingsDate(string sName)
        {
            string sValue = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SetSettingsString(sName, sValue);
        }


        #endregion

        #endregion

        public static bool IsFromCmdLine(Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            if (!IsFamilyDesktop())
                return false;

            // 1021 = ActivationKind.CommandLineLaunch; ale to jest obecne dopiero od 16299 czyli = Win 1709
            if ((int)args.Kind != 1021)
                return false;

            return true;
        }

        #region "testy sieciowe"
        // -- Testy sieciowe ---------------------------------------------


        public static bool IsFamilyMobile()
        { // Brewiarz: wymuszanie zmiany dark/jasne
          // GrajCyganie: zmiana wielkosci okna
          // pociagi: ile rzadkow ma pokazac (rozmiar ekranu)
          // kamerki: full screen wlacz/wylacz tylko dla niego
          // sympatia...
          // TODO: WASM w zależności od rozmiaru ekranu?
            return (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile");
            //return Windows.System.Profile.AnalyticsInfo.DeviceForm.ToLower().Contains("mobile");
        }

        public static bool IsFamilyDesktop()
        {
            return (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop");
        }

        public static bool NetIsIPavailable(bool bMsg)
        {

            if (GetSettingsBool("offline"))
                return false;

            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                return true;
            if (bMsg)
                DialogBox("ERROR: no IP network available");
            return false;
        }

        public static bool NetIsCellInet()
        {
            return Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWwanConnectionProfile;
        }

#if __ANDROID__
        private static bool AndroidReminderObsolete()
        {
            var oContext = Android.App.Application.Context;
            Android.Net.ConnectivityManager cm =
                (Android.Net.ConnectivityManager)oContext.GetSystemService(Android.Content.Context.ConnectivityService);

            // OBSOLETE w Android 29 (=Q)
            // https://developer.android.com/reference/android/net/NetworkInfo
            // postawione tutaj, Å¼eby przy kazdej kompilacji sprawdzac czy jeszcze w Android to jest
            Android.Net.NetworkInfo info = cm.ActiveNetworkInfo;
            if (info == null) return false;
            if (!info.IsConnected) return false;

            // drugie reminder
            string sDirName = Android.OS.Environment.DirectoryMusic;
            string sPathName = Android.OS.Environment.GetExternalStoragePublicDirectory(sDirName).CanonicalPath;
            // ta ścieżka jest niedostępna dla app od Android Q, API29, Android 10

            return true;
        }
#endif

        public static string GetHostName()
        {
            string sNazwa = System.Net.Dns.GetHostName();
            return sNazwa;
            //IReadOnlyList<Windows.Networking.HostName> hostNames = Windows.Networking.Connectivity.NetworkInformation.GetHostNames();
            //foreach (Windows.Networking.HostName oItem in hostNames)
            //{
            //    if (oItem.DisplayName.Contains(".local"))
            //        return oItem.DisplayName.Replace(".local", "");
            //}
            //return "";
        }


        public static bool IsThisMoje()
        {
            string sTmp = GetHostName().ToLower();
            if (sTmp == "home-pkar")
                return true;
            if (sTmp == "lumia_pkar")
                return true;
            if (sTmp == "kuchnia_pk")
                return true;
            if (sTmp == "ppok_pk")
                return true;
            // If sTmp.Contains("pkar") Then Return True
            // If sTmp.EndsWith("_pk") Then Return True
            return false;
        }

        // unimplemented in Uno yet
#if false

        public async static System.Threading.Tasks.Task<bool> NetWiFiOffOn()
        {

            // https://social.msdn.microsoft.com/Forums/ie/en-US/60c4a813-dc66-4af5-bf43-e632c5f85593/uwpbluetoothhow-to-turn-onoff-wifi-bluetooth-programmatically?forum=wpdevelop
            var result222 = await Windows.Devices.Radios.Radio.RequestAccessAsync();
            IReadOnlyList<Windows.Devices.Radios.Radio> radios = await Windows.Devices.Radios.Radio.GetRadiosAsync();

            foreach (var oRadio in radios)
            {
                if (oRadio.Kind == Windows.Devices.Radios.RadioKind.WiFi)
                {
                    Windows.Devices.Radios.RadioAccessStatus oStat = await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.Off);
                    if (oStat != Windows.Devices.Radios.RadioAccessStatus.Allowed)
                        return false;
                    await Task.Delay(3 * 1000);
                    oStat = await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.On);
                    if (oStat != Windows.Devices.Radios.RadioAccessStatus.Allowed)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        ///      Zwraca -1 (no radio), 0 (off), 1 (on), ale gdy bMsg to pokazuje dokładniej błąd (nie włączony, albo nie ma radia Bluetooth) - wedle stringów podanych, które mogą być jednak identyfikatorami w Resources
        ///      </summary>
        public async static Task<int> NetIsBTavailableAsync(bool bMsg, bool bRes = false, string sBtDisabled = "ERROR: Bluetooth is not enabled", string sNoRadio = "ERROR: Bluetooth radio not found")
        {


            // Dim result222 As Windows.Devices.Radios.RadioAccessStatus = Await Windows.Devices.Radios.Radio.RequestAccessAsync()
            // If result222 <> Windows.Devices.Radios.RadioAccessStatus.Allowed Then Return -1

            IReadOnlyList<Windows.Devices.Radios.Radio> oRadios = await Windows.Devices.Radios.Radio.GetRadiosAsync();

            /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            bool bHasBT = false;

            foreach (Windows.Devices.Radios.Radio oRadio in oRadios)
            {
                if (oRadio.Kind == Windows.Devices.Radios.RadioKind.Bluetooth)
                {
                    if (oRadio.State == Windows.Devices.Radios.RadioState.On)
                        return 1;
                    bHasBT = true;
                }
            }

            if (bHasBT)
            {
                if (bMsg)
                {
                    if (bRes)
                        await DialogBoxResAsync(sBtDisabled);
                    else
                        await DialogBoxAsync(sBtDisabled);
                }
                return 0;
            }
            else
            {
                if (bMsg)
                {
                    if (bRes)
                        await DialogBoxResAsync(sNoRadio);
                    else
                        await DialogBoxAsync(sNoRadio);
                }
                return -1;
            }
        }

        /// <summary>
        ///      Zwraca true/false czy State (po call) jest taki jak bOn; wymaga devCap=radios
        ///      </summary>
        public async static Task<bool> NetTrySwitchBTOnAsync(bool bOn)
        {
            int iCurrState = await NetIsBTavailableAsync(false);
            if (iCurrState == -1)
                return false;

            // jeśli nie trzeba przełączać... 
            if (bOn && iCurrState == 1)
                return true;
            if (!bOn && iCurrState == 0)
                return true;

            // czy mamy prawo przełączyć? (devCap=radios)
            Windows.Devices.Radios.RadioAccessStatus result222 = await Windows.Devices.Radios.Radio.RequestAccessAsync();
            if (result222 != Windows.Devices.Radios.RadioAccessStatus.Allowed)
                return false;


            IReadOnlyList<Windows.Devices.Radios.Radio> radios = await Windows.Devices.Radios.Radio.GetRadiosAsync();

            foreach (var oRadio in radios)
            {
                if (oRadio.Kind == Windows.Devices.Radios.RadioKind.Bluetooth)
                {
                    Windows.Devices.Radios.RadioAccessStatus oStat;
                    if (bOn)
                        oStat = await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.On);
                    else
                        oStat = await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.Off);
                    if (oStat != Windows.Devices.Radios.RadioAccessStatus.Allowed)
                        return false;
                }
            }

            return true;
        }


#endif

        #endregion

        #region "DialogBoxy"
        // -- DialogBoxy ---------------------------------------------



        public async static System.Threading.Tasks.Task DialogBoxAsync(string sMsg)
        {
            Windows.UI.Popups.MessageDialog oMsg = new Windows.UI.Popups.MessageDialog(sMsg);
            await oMsg.ShowAsync();
        }

        public static void DialogBox(string sMsg)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            DialogBoxAsync(sMsg);
#pragma warning restore CS4014
        }

        public static string GetLangString(string sMsg, string sDefault = "")
        {
            if (string.IsNullOrEmpty(sMsg))
                return "";

            string sRet = sMsg;
            if (!string.IsNullOrEmpty(sDefault)) sRet = sDefault;
            try
            {
                sRet = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString(sMsg);
            }
            catch { }
            return sRet;
        }

        public async static System.Threading.Tasks.Task DialogBoxResAsync(string sMsg)
        {
            sMsg = GetLangString(sMsg);
            await DialogBoxAsync(sMsg).ConfigureAwait(true);
        }
        public static void DialogBoxRes(string sMsg)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            DialogBoxResAsync(sMsg);
#pragma warning restore CS4014
        }

        public async static System.Threading.Tasks.Task DialogBoxResAsync(string sMsg, string sErrData)
        {
            sMsg = GetLangString(sMsg) + " " + sErrData;
            await DialogBoxAsync(sMsg).ConfigureAwait(true);
        }
        public static void DialogBoxRes(string sMsg, string sErrData)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            DialogBoxResAsync(sMsg, sErrData);
#pragma warning restore CS4014
        }


        public async static System.Threading.Tasks.Task DialogBoxErrorAsync(int iNr, string sMsg)
        {
            string sTxt = GetLangString("errAnyError");
            sTxt = sTxt + " (" + iNr.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")" + "\n" + sMsg;
            await DialogBoxAsync(sTxt).ConfigureAwait(true);
        }

        public static void DialogBoxResError(int iNr, string sMsg)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            DialogBoxErrorAsync(iNr, GetLangString(sMsg));
#pragma warning restore CS4014
        }

        public async static System.Threading.Tasks.Task<bool> DialogBoxYNAsync(string sMsg, string sYes = "Tak", string sNo = "Nie")
        {
            Windows.UI.Popups.MessageDialog oMsg = new Windows.UI.Popups.MessageDialog(sMsg);
            Windows.UI.Popups.UICommand oYes = new Windows.UI.Popups.UICommand(sYes);
            Windows.UI.Popups.UICommand oNo = new Windows.UI.Popups.UICommand(sNo);
            oMsg.Commands.Add(oYes);
            oMsg.Commands.Add(oNo);
            oMsg.DefaultCommandIndex = 1;    // default: No
            oMsg.CancelCommandIndex = 1;
            Windows.UI.Popups.IUICommand oCmd = await oMsg.ShowAsync();
            if (oCmd == null)
                return false;
            if (oCmd.Label == sYes)
                return true;

            return false;
        }

        public async static System.Threading.Tasks.Task<bool> DialogBoxResYNAsync(string sMsgResId, string sYesResId = "resDlgYes", string sNoResId = "resDlgNo")
        {
            string sMsg, sYes, sNo;

            {
                var withBlock = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                sMsg = withBlock.GetString(sMsgResId);
                sYes = withBlock.GetString(sYesResId);
                sNo = withBlock.GetString(sNoResId);
            }

            if (string.IsNullOrEmpty(sMsg))
                sMsg = sMsgResId;  // zabezpieczenie na brak string w resource
            if (string.IsNullOrEmpty(sYes))
                sYes = sYesResId;
            if (string.IsNullOrEmpty(sNo))
                sNo = sNoResId;

            return await DialogBoxYNAsync(sMsg, sYes, sNo).ConfigureAwait(true);
        }

        /// <summary>
        ///      Dla Cancel zwraca ""
        ///      </summary>
        public async static System.Threading.Tasks.Task<string> DialogBoxInputAsync(string sMsgResId, string sDefaultResId = "", string sYesResId = "resDlgContinue", string sNoResId = "resDlgCancel")
        {
            string sMsg, sYes, sNo, sDefault;

            sMsg = GetLangString(sMsgResId);
            sYes = GetLangString(sYesResId);
            sNo = GetLangString(sNoResId);
            sDefault = "";
            if (!string.IsNullOrEmpty(sDefaultResId))
                sDefault = GetLangString(sDefaultResId);

            if (string.IsNullOrEmpty(sMsg))
                sMsg = sMsgResId;  // zabezpieczenie na brak string w resource
            if (string.IsNullOrEmpty(sYes))
                sYes = sYesResId;
            if (string.IsNullOrEmpty(sNo))
                sNo = sNoResId;
            if (string.IsNullOrEmpty(sDefault))
                sDefault = sDefaultResId;

            Windows.UI.Xaml.Controls.TextBox oInputTextBox = new Windows.UI.Xaml.Controls.TextBox
            {
                AcceptsReturn = false,
                Text = sDefault
            };
            Windows.UI.Xaml.Controls.ContentDialog oDlg = new Windows.UI.Xaml.Controls.ContentDialog
            {
                Content = oInputTextBox,
                PrimaryButtonText = sYes,
                SecondaryButtonText = sNo,
                Title = sMsg
            };

            var oCmd = await oDlg.ShowAsync();
#if !NETFX_CORE
            oDlg.Dispose();
#endif
            if (oCmd != Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
                return "";

            return oInputTextBox.Text;
        }

        public static void DialogOrToast(bool bDialog, string sMessage)
        {
            DebugOut(sMessage);
            if (bDialog)
                DialogBox(sMessage);
            else
                MakeToast(sMessage);
        }

        #endregion

        #region "CheckPlatform etc"

        public static string GetPlatform()
        {
#if NETFX_CORE
            return "uwp";
#elif __ANDROID__
            return "android";
#elif __IOS__
        return "ios";
#elif __WASM__
        return "wasm";
#else
        return "other";
#endif
        }

        public static bool GetPlatform(string sPlatform)
        {
            if (string.IsNullOrEmpty(sPlatform)) return false;
            if (GetPlatform().ToLower() == sPlatform.ToLower()) return true;
            return false;
        }

        public static bool GetPlatform(bool bUwp, bool bAndro, bool bIos, bool bWasm, bool bOther)
        {
#if NETFX_CORE
            return bUwp;
#elif __ANDROID__
            return bAndro;
#elif __IOS__
        return bIos;
#elif __WASM__
            return bWasm;
#else
        return bOther;
#endif
        }

        public static int GetPlatform(int bUwp, int bAndro, int bIos, int bWasm, int bOther)
        {
#if NETFX_CORE
            return bUwp;
#elif __ANDROID__
            return bAndro;
#elif __IOS__
        return bIos;
#elif __WASM__
            return bWasm;
#else
        return bOther;
#endif
        }

        public static string GetPlatform(string bUwp, string bAndro, string bIos, string bWasm, string bOther)
        {
#if NETFX_CORE
            return bUwp;
#elif __ANDROID__
            return bAndro;
#elif __IOS__
        return bIos;
#elif __WASM__
            return bWasm;
#else
        return bOther;
#endif
        }

        #endregion

        #region "ProgressBar/Ring"
        //  dodałem 25 X 2020

        private static Windows.UI.Xaml.Controls.ProgressRing _mProgRing = null;
        private static Windows.UI.Xaml.Controls.ProgressBar _mProgBar = null;
        private static int _mProgRingShowCnt = 0;

        public static void ProgRingInit(bool bRing, bool bBar)
        {
            // 2020.11.24: dodaję force-off do ProgRing na Init
            _mProgRingShowCnt = 0;   // skoro inicjalizuje, to znaczy że na pewno trzeba wyłączyć

            Windows.UI.Xaml.Controls.Frame oFrame = Windows.UI.Xaml.Window.Current.Content as Windows.UI.Xaml.Controls.Frame;
            Windows.UI.Xaml.Controls.Page oPage = oFrame?.Content as Windows.UI.Xaml.Controls.Page;
            Windows.UI.Xaml.Controls.Grid oGrid = oPage?.Content as Windows.UI.Xaml.Controls.Grid;
            if (oGrid is null)
            {
                // skoro to nie Grid, to nie ma jak umiescic koniecznych elementow
                DebugOut("ProgRingInit wymaga Grid jako podstawy Page");
                throw new ArgumentException("ProgRingInit wymaga Grid jako podstawy Page");
            }

            // *TODO* sprawdz czy istnieje juz taki Control?

            int iCols = 0;
            if (oGrid.ColumnDefinitions is object)
                iCols = oGrid.ColumnDefinitions.Count; // moze byc 0
            int iRows = 0;
            if (oGrid.RowDefinitions is object)
                iRows = oGrid.RowDefinitions.Count; // moze byc 0
            if (bRing)
            {
                _mProgRing = new Windows.UI.Xaml.Controls.ProgressRing();
                _mProgRing.Name = "uiPkAutoProgRing";
                _mProgRing.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center;
                _mProgRing.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center;
                _mProgRing.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                Windows.UI.Xaml.Controls.Canvas.SetZIndex(_mProgRing, 10000);
                if (iRows > 1)
                {
                    Windows.UI.Xaml.Controls.Grid.SetRow(_mProgRing, 0);
                    Windows.UI.Xaml.Controls.Grid.SetRowSpan(_mProgRing, iRows);
                }

                if (iCols > 1)
                {
                    Windows.UI.Xaml.Controls.Grid.SetColumn(_mProgRing, 0);
                    Windows.UI.Xaml.Controls.Grid.SetColumnSpan(_mProgRing, iCols);
                }

                oGrid.Children.Add(_mProgRing);
            }

            if (bBar)
            {
                _mProgBar = new Windows.UI.Xaml.Controls.ProgressBar();
                _mProgBar.Name = "uiPkAutoProgBar";
                _mProgBar.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Bottom;
                _mProgBar.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
                _mProgBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                Windows.UI.Xaml.Controls.Canvas.SetZIndex(_mProgRing, 10000);
                if (iRows > 1)
                    Windows.UI.Xaml.Controls.Grid.SetRow(_mProgBar, iRows - 1);
                if (iCols > 1)
                {
                    Windows.UI.Xaml.Controls.Grid.SetColumn(_mProgBar, 0);
                    Windows.UI.Xaml.Controls.Grid.SetColumnSpan(_mProgBar, iCols);
                }

                oGrid.Children.Add(_mProgBar);
            }
        }

        public static void ProgRingShow(bool bVisible, bool bForce = false, double dMin = 0d, double dMax = 100d)
        {
            if (_mProgBar is object)
            {
                _mProgBar.Minimum = dMin;
                _mProgBar.Value = dMin;
                _mProgBar.Maximum = dMax;
            }

            if (bForce)
            {
                if (bVisible)
                {
                    _mProgRingShowCnt = 1;
                }
                else
                {
                    _mProgRingShowCnt = 0;
                }
            }
            else if (bVisible)
            {
                _mProgRingShowCnt += 1;
            }
            else
            {
                _mProgRingShowCnt -= 1;
            }

            DebugOut("ProgRingShow(" + bVisible + ", " + bForce + "...), current ShowCnt=" + _mProgRingShowCnt);

            try
            {
                if (_mProgRingShowCnt > 0)
                {
                    DebugOut("ProgRingShow - mam pokazac");
                    if (_mProgRing is object)
                    {
                        double dSize;
                        var oGrid = _mProgRing.Parent as Windows.UI.Xaml.Controls.Grid;
                        dSize = Math.Min(oGrid.ActualHeight, oGrid.ActualWidth) / 2;
                        dSize = Math.Max(dSize, 200); // jakby jeszcze nie było ustawione (Android!)
                        _mProgRing.Width = dSize;
                        _mProgRing.Height = dSize;
                        _mProgRing.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        _mProgRing.IsActive = true;
                    }

                    if (_mProgBar is object)
                        _mProgBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                else
                {
                    DebugOut("ProgRingShow - mam ukryc");
                    if (_mProgRing is object)
                    {
                        _mProgRing.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                        _mProgRing.IsActive = false;
                    }

                    if (_mProgBar is object)
                        _mProgBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
            catch
            {

            }

        }

        public static void ProgRingVal(double dValue)
        {
            if (_mProgBar is null)
            {
                // skoro to nie Grid, to nie ma jak umiescic koniecznych elementow
                DebugOut("ProgRing(double) wymaga wczesniej ProgRingInit");
                throw new ArgumentException("ProgRing(double) wymaga wczesniej ProgRingInit");
            }

            _mProgBar.Value = dValue;
        }

        public static void ProgRingInc()
        {
            if (_mProgBar is null)
            {
                // skoro to nie Grid, to nie ma jak umiescic koniecznych elementow
                DebugOut("ProgRing(double) wymaga wczesniej ProgRingInit");
                throw new ArgumentException("ProgRing(double) wymaga wczesniej ProgRingInit");
            }

            double dVal = _mProgBar.Value + 1;
            if (dVal > _mProgBar.Maximum)
            {
                DebugOut("ProgRingInc na wiecej niz Maximum?");
                _mProgBar.Value = _mProgBar.Maximum;
            }
            else
            {
                _mProgBar.Value = dVal;
            }
        }


        #endregion

        // --- INNE FUNKCJE ------------------------

        #region "toasty"
        //public static void SetBadgeNo(int iInt)
        //{
        //    // https://docs.microsoft.com/en-us/windows/uwp/controls-and-patterns/tiles-and-notifications-badges

        //    Windows.Data.Xml.Dom.XmlDocument oXmlBadge;
        //    oXmlBadge = Windows.UI.Notifications.BadgeUpdateManager.GetTemplateContent(Windows.UI.Notifications.BadgeTemplateType.BadgeNumber);

        //    Windows.Data.Xml.Dom.XmlElement oXmlNum;
        //    oXmlNum = (Windows.Data.Xml.Dom.XmlElement)oXmlBadge.SelectSingleNode("/badge");
        //    oXmlNum.SetAttribute("value", iInt.ToString());

        //    Windows.UI.Notifications.BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(new Windows.UI.Notifications.BadgeNotification(oXmlBadge));
        //}

        public static string XmlSafeString(string sInput)
        {
            if (sInput is null) return "";
            string sTmp;
            sTmp = sInput.Replace("&", "&amp;");
            sTmp = sTmp.Replace("<", "&lt;");
            sTmp = sTmp.Replace(">", "&gt;");
            return sTmp;
        }

        public static string XmlSafeStringQt(string sInput)
        {
            string sTmp;
            sTmp = XmlSafeString(sInput);
            sTmp = sTmp.Replace("\"", "&quote;");
            return sTmp;
        }

        public static string ToastAction(string sAType, string sAct, string sGuid, string sContent)
        {
            string sTmp = sContent;
            if (!string.IsNullOrEmpty(sTmp))
                sTmp = GetSettingsString(sTmp, sTmp);

            string sTxt = "<action " + "activationType=\"" + sAType + "\" " + "arguments=\"" + sAct + sGuid + "\" " + "content=\"" + sTmp + "\"/> ";
            return sTxt;
        }

        /// <summary>
        ///      dwa kolejne teksty, sMsg oraz sMsg1
        ///      </summary>
        public static void MakeToast(string sMsg, string sMsg1 = "")
        {
            // Mój Uno: razem z Android, ich Uno - tylko UWP
#if NETFX_CORE || __ANDROID__
            var sXml = "<visual><binding template='ToastGeneric'><text>" + XmlSafeString(sMsg);
            if (!string.IsNullOrEmpty(sMsg1))
                sXml = sXml + "</text><text>" + XmlSafeString(sMsg1);
            sXml = sXml + "</text></binding></visual>";
            var oXml = new Windows.Data.Xml.Dom.XmlDocument();
            oXml.LoadXml("<toast>" + sXml + "</toast>");
            var oToast = new Windows.UI.Notifications.ToastNotification(oXml);

            // WYMAGA MOJEJ KOMPILACJI
            Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().Show(oToast);

#elif __IOS__
            string sTitle, sBody;
            if (sMsg1 == "")
            {
                sTitle = "";
                sBody = sMsg;
            }
            else
            {
                sTitle = sMsg;
                sBody = sMsg1;
            }
            Plugin.LocalNotifications.CrossLocalNotifications.Current.Show(sTitle, sBody);
#endif
        }

        public static void MakeToast(DateTime oDate, string sMsg, string sMsg1 = "")
        {
            string sXml = "<visual><binding template='ToastGeneric'><text>" + XmlSafeString(sMsg);
            if (sMsg1 != "") sXml = sXml + "</text><text>" + XmlSafeString(sMsg1);
            sXml += "</text></binding></visual>";
            var oXml = new Windows.Data.Xml.Dom.XmlDocument();
            oXml.LoadXml("<toast>" + sXml + "</toast>");
            try
            {
                // ' Dim oToast = New Windows.UI.Notifications.ScheduledToastNotification(oXml, oDate, TimeSpan.FromHours(1), 10)
                var oToast = new Windows.UI.Notifications.ScheduledToastNotification(oXml, oDate);
                Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().AddToSchedule(oToast);
            }
            catch
            {
            }
        }


        public static void RemoveScheduledToasts()
        {
            var oToastMan = Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier();
            try
            {
                while (oToastMan.GetScheduledToastNotifications().Count > 0)
                    oToastMan.RemoveFromSchedule(oToastMan.GetScheduledToastNotifications().ElementAt(0));
            }
            catch
            {
                // ' ponoc na desktopm nie dziala
            }
        }


        #endregion

        public static string GetAppVers()
        {
            return Windows.ApplicationModel.Package.Current.Id.Version.Major + "." +
                Windows.ApplicationModel.Package.Current.Id.Version.Minor + "." +
                Windows.ApplicationModel.Package.Current.Id.Version.Build;

        }

        /// <summary>
        ///      jeśli oTB=null, to wtedy dodaje textblock w grid.row=1, grid.colspan=max
        ///      </summary>
        public static void GetAppVers(Windows.UI.Xaml.Controls.TextBlock oTB)
        {
            if (oTB == null)
            {
                Windows.UI.Xaml.Controls.Frame oFrame = (Windows.UI.Xaml.Controls.Frame)Windows.UI.Xaml.Window.Current.Content;
                Windows.UI.Xaml.Controls.Page oPage = (Windows.UI.Xaml.Controls.Page)oFrame.Content;
                Windows.UI.Xaml.Controls.Grid oGrid = oPage.Content as Windows.UI.Xaml.Controls.Grid;
                if (oGrid == null)
                {
                    // skoro to nie Grid, to nie ma jak umiescic koniecznych elementow
                    DebugOut("GetAppVers(null) wymaga Grid jako podstawy Page");
                    throw new ArgumentException("GetAppVers(null) wymaga Grid jako podstawy Page");
                }

                int iCols = 0;
                if (oGrid.ColumnDefinitions != null)
                    iCols = oGrid.ColumnDefinitions.Count; // może być 0
                int iRows = 0;
                if (oGrid.RowDefinitions != null)
                    iRows = oGrid.RowDefinitions.Count; // może być 0

                oTB = new Windows.UI.Xaml.Controls.TextBlock();
                oTB.Name = "uiPkAutoVersion";
                oTB.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center;
                oTB.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center;
                oTB.FontSize = 10;

                if (iRows > 2)
                    Windows.UI.Xaml.Controls.Grid.SetRow(oTB, 1);
                if (iCols > 1)
                {
                    Windows.UI.Xaml.Controls.Grid.SetColumn(oTB, 0);
                    Windows.UI.Xaml.Controls.Grid.SetColumnSpan(oTB, iCols);
                }
                oGrid.Children.Add(oTB);
            }

            string sTxt = GetAppVers();
#if DEBUG 
            sTxt += " (debug)";
#endif
            oTB.Text = sTxt;
        }


        public static int WinVer()
        {
            // Unknown = 0,
            // Threshold1 = 1507,   // 10240
            // Threshold2 = 1511,   // 10586
            // Anniversary = 1607,  // 14393 Redstone 1
            // Creators = 1703,     // 15063 Redstone 2
            // FallCreators = 1709 // 16299 Redstone 3
            // April = 1803		// 17134
            // October = 1809		// 17763
            // ? = 190?		// 18???
            // April  1803, 17134, RS5

#if NETFX_CORE
            ulong u = ulong.Parse(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
            u = (u & 0xFFFF0000L) >> 16;
            return (int)u;
#elif __ANDROID__
            return (int)Android.OS.Build.VERSION.SdkInt;
#else
            return 0;
#endif
        }


        //private static Windows.Web.Http.HttpClient moHttp = new Windows.Web.Http.HttpClient();

        //public async static System.Threading.Tasks.Task<string> HttpPageAsync(string sUrl, string sErrMsg, string sData = "")
        //{
        //    try
        //    {
        //        if (!NetIsIPavailable(true))
        //            return "";
        //        if (string.IsNullOrEmpty(sUrl))
        //            return "";

        //        if ((sUrl.Substring(0, 4) ?? "") != "http")
        //            sUrl = "http://beskid.geo.uj.edu.pl/p/dysk" + sUrl;

        //        if (moHttp == null)
        //        {
        //            moHttp = new Windows.Web.Http.HttpClient();
        //            moHttp.DefaultRequestHeaders.UserAgent.TryParseAdd("GrajCyganie");
        //        }

        //        var sError = "";
        //        Windows.Web.Http.HttpResponseMessage oResp = null;

        //        try
        //        {
        //            if (!string.IsNullOrEmpty(sData))
        //            {
        //                var oHttpCont = new Windows.Web.Http.HttpStringContent(sData, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
        //                oResp = await moHttp.PostAsync(new Uri(sUrl), oHttpCont);
        //            }
        //            else
        //                oResp = await moHttp.GetAsync(new Uri(sUrl));
        //        }
        //        catch (Exception ex)
        //        {
        //            sError = ex.Message;
        //        }

        //        if (!string.IsNullOrEmpty(sError))
        //        {
        //            await DialogBox("error " + sError + " at " + sErrMsg + " page");
        //            return "";
        //        }

        //        if ((oResp.StatusCode == 303) || (oResp.StatusCode == 302) || (oResp.StatusCode == 301))
        //        {
        //            // redirect
        //            sUrl = oResp.Headers.Location.ToString;
        //            // If sUrl.ToLower.Substring(0, 4) <> "http" Then
        //            // sUrl = "https://sympatia.onet.pl/" & sUrl   ' potrzebne przy szukaniu
        //            // End If

        //            if (!string.IsNullOrEmpty(sData))
        //            {
        //                // Dim oHttpCont = New HttpStringContent(sData, Text.Encoding.UTF8, "application/x-www-form-urlencoded")
        //                var oHttpCont = new Windows.Web.Http.HttpStringContent(sData, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
        //                oResp = await moHttp.PostAsync(new Uri(sUrl), oHttpCont);
        //            }
        //            else
        //                oResp = await moHttp.GetAsync(new Uri(sUrl));
        //        }

        //        if (oResp.StatusCode > 290)
        //        {
        //            await DialogBox("ERROR " + oResp.StatusCode + " getting " + sErrMsg + " page");
        //            return "";
        //        }

        //        string sResp = "";
        //        try
        //        {
        //            sResp = await oResp.Content.ReadAsStringAsync;
        //        }
        //        catch (Exception ex)
        //        {
        //            sError = ex.Message;
        //        }

        //        if (!string.IsNullOrEmpty(sError))
        //        {
        //            await DialogBox("error " + sError + " at ReadAsStringAsync " + sErrMsg + " page");
        //            return "";
        //        }

        //        return sResp;
        //    }
        //    catch (Exception ex)
        //    {
        //        CrashMessageExit("@HttpPageAsync", ex.Message);
        //    }

        //    return "";
        //}

        public static string RemoveHtmlTags(string sHtml)
        {
            int iInd0, iInd1;
            if (sHtml is null) return "";
            iInd0 = sHtml.IndexOf("<script", StringComparison.Ordinal);
            if (iInd0 > 0)
            {
                iInd1 = sHtml.IndexOf("</script>", iInd0, StringComparison.Ordinal);
                if (iInd1 > 0)
                    sHtml = sHtml.Remove(iInd0, (iInd1 - iInd0) + 9);
            }

            iInd0 = sHtml.IndexOf("<", StringComparison.Ordinal);
            iInd1 = sHtml.IndexOf(">", StringComparison.Ordinal);
            while (iInd0 > -1)
            {
                if (iInd1 > -1)
                    sHtml = sHtml.Remove(iInd0, (iInd1 - iInd0) + 1);
                else
                    sHtml = sHtml.Substring(0, iInd0);
                sHtml = sHtml.Trim();

                iInd0 = sHtml.IndexOf("<", StringComparison.Ordinal);
                iInd1 = sHtml.IndexOf(">", StringComparison.Ordinal);
            }

            sHtml = sHtml.Replace("&nbsp;", " ");
            sHtml = sHtml.Replace('\r', '\n');
            sHtml = sHtml.Replace("\n\n", "\n");
            sHtml = sHtml.Replace("\n\n", "\n");
            sHtml = sHtml.Replace("\n\n", "\n");

            return sHtml.Trim();
        }


        public static void OpenBrowser(Uri oUri, bool bForceEdge = false)
        { // bForceEdge ma sens tylko pod Windows przecież (a poza tym i tak coraz mniej, bo DevEdge/Chromium)
#if NETFX_CORE
            if (bForceEdge)
            {
                Windows.System.LauncherOptions options = new Windows.System.LauncherOptions
                {
                    TargetApplicationPackageFamilyName = "Microsoft.MicrosoftEdge_8wekyb3d8bbwe"
                };
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Windows.System.Launcher.LaunchUriAsync(oUri, options);
#pragma warning restore CS4014
            }
            else
#endif
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Windows.System.Launcher.LaunchUriAsync(oUri);
#pragma warning restore CS4014
        }

        public static void OpenBrowser(string sUri, bool bForceEdge = false)
        {
            Uri oUri = new Uri(sUri);
            OpenBrowser(oUri, bForceEdge);
        }


        public static string FileLen2string(long iBytes)
        {
            if (iBytes == (long)1)
                return "1 byte";
            if (iBytes < (long)10000)
                return iBytes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " bytes";
            iBytes = iBytes / (long)1024;
            if (iBytes == (long)1)
                return "1 kibibyte";
            if (iBytes < (long)2000)
                return iBytes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " kibibytes";
            iBytes = iBytes / (long)1024;
            if (iBytes == (long)1)
                return "1 mebibyte";
            if (iBytes < (long)2000)
                return iBytes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " mebibytes";
            iBytes = iBytes / (long)1024;
            if (iBytes == (long)1)
                return "1 gibibyte";
            return iBytes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " gibibytes";
        }


        //public static DateTime UnixTimeToTime(long lTime)
        //{
        //    // 1509993360
        //    DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        //    dtDateTime = dtDateTime.AddSeconds((double)lTime);   // UTC
        //                                                         // dtDateTime.Kind = DateTimeKind.Utc
        //    return dtDateTime.ToLocalTime();
        //}

        //public static int GPSdistanceDwa(Windows.Devices.Geolocation.BasicGeoposition oPos0, Windows.Devices.Geolocation.BasicGeoposition oPos1)
        //{
        //    // https://stackoverflow.com/questions/28569246/how-to-get-distance-between-two-locations-in-windows-phone-8-1

        //    try
        //    {
        //        int iRadix = 6371000;
        //        double tLat = (oPos1.Latitude - oPos0.Latitude) * Math.PI / 180;
        //        double tLon = (oPos1.Longitude - oPos0.Longitude) * Math.PI / 180;
        //        double a = Math.Sin(tLat / 2) * Math.Sin(tLat / 2) + Math.Cos(Math.PI / 180 * oPos0.Latitude) * Math.Cos(Math.PI / 180 * oPos1.Latitude) * Math.Sin(tLon / 2) * Math.Sin(tLon / 2);
        //        double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        //        double d = iRadix * c;

        //        return (int)d;
        //    }
        //    catch
        //    {
        //        return 0;
        //    }// nie powinno sie nigdy zdarzyc, ale na wszelki wypadek...
        //}
        //public static int GPSdistanceDwa(double dLat0, double dLon0, double dLat, double dLon)
        //{
        //    // https://stackoverflow.com/questions/28569246/how-to-get-distance-between-two-locations-in-windows-phone-8-1

        //    try
        //    {
        //        int iRadix = 6371000;
        //        double tLat = (dLat - dLat0) * Math.PI / 180;
        //        double tLon = (dLon - dLon0) * Math.PI / 180;
        //        double a = Math.Sin(tLat / 2) * Math.Sin(tLat / 2) + Math.Cos(Math.PI / 180 * dLat0) * Math.Cos(Math.PI / 180 * dLat) * Math.Sin(tLon / 2) * Math.Sin(tLon / 2);
        //        double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        //        double d = iRadix * c;

        //        return (int)d;
        //    }
        //    catch
        //    {
        //        return 0;
        //    }// nie powinno sie nigdy zdarzyc, ale na wszelki wypadek...
        //}


        public static Windows.Devices.Geolocation.BasicGeoposition NewGeoPos(double dLatitude, double dLongitude)
        {
            return new Windows.Devices.Geolocation.BasicGeoposition
            {
                Latitude = dLatitude,
                Longitude = dLongitude
            };
        }

        #region "triggers"
        public static bool IsTriggersRegistered(string sNamePrefix)
        {
            sNamePrefix = sNamePrefix.Replace(" ", "").Replace("'", "");

            try
            {
                foreach (var oTask in Windows.ApplicationModel.Background.BackgroundTaskRegistration.AllTasks)
                {
                    if (oTask.Value.Name.ToLower().StartsWith(sNamePrefix.ToLower())) return true;
                }
            }
            catch
            {
                // np. gdy nie ma permissions, to może być FAIL
            }

            return false;
        }

        /// <summary>
        /// jakikolwiek z prefixem Package.Current.DisplayName
        /// </summary>
        public static bool IsTriggersRegistered()
        {
            return IsTriggersRegistered(Windows.ApplicationModel.Package.Current.DisplayName);
        }

        /// <summary>
        /// wszystkie z prefixem Package.Current.DisplayName
        /// </summary>
        public static void UnregisterTriggers()
        {
            UnregisterTriggers(Windows.ApplicationModel.Package.Current.DisplayName);
        }

        public static void UnregisterTriggers(string sNamePrefix)
        {
            sNamePrefix = sNamePrefix.Replace(" ", "").Replace("'", "");

            try
            {
                foreach (var oTask in Windows.ApplicationModel.Background.BackgroundTaskRegistration.AllTasks)
                {
                    if (string.IsNullOrEmpty(sNamePrefix) || oTask.Value.Name.ToLower().StartsWith(sNamePrefix.ToLower()))
                    {
                        oTask.Value.Unregister(true);
                    }
                }
            }
            catch
            {
                // np. gdy nie ma permissions, to może być FAIL
            }
        }

        public static async System.Threading.Tasks.Task<bool> CanRegisterTriggersAsync()
        {
            Windows.ApplicationModel.Background.BackgroundAccessStatus oBAS;
            oBAS = await Windows.ApplicationModel.Background.BackgroundExecutionManager.RequestAccessAsync();

            if (oBAS == Windows.ApplicationModel.Background.BackgroundAccessStatus.AlwaysAllowed) return true;
            if (oBAS == Windows.ApplicationModel.Background.BackgroundAccessStatus.AllowedSubjectToSystemPolicy) return true;

            return false;
        }

        public static Windows.ApplicationModel.Background.BackgroundTaskRegistration RegisterTimerTrigger(string sName, uint iMinutes, bool bOneShot = false, Windows.ApplicationModel.Background.SystemCondition oCondition = null)
        {
            try
            {
                var builder = new Windows.ApplicationModel.Background.BackgroundTaskBuilder();

                builder.SetTrigger(new Windows.ApplicationModel.Background.TimeTrigger(iMinutes, bOneShot));
                builder.Name = sName;
                if (oCondition is object) builder.AddCondition(oCondition);
                var oRet = builder.Register();
                return oRet;
            }
            catch
            {
                // np. gdy nie ma permissions, to może być FAIL
            }

            return null;
        }

        private static string GetTriggerPolnocnyName()
        {
            string sName = Windows.ApplicationModel.Package.Current.DisplayName + "_polnocny";
            sName = sName.Replace(" ", "").Replace("'", "");
            return sName;
        }

        /// <summary>
        /// Tak naprawdę powtarzalny - w OnBackgroundActivated wywołaj IsThisTriggerPolnocny
        /// </summary>
        public static async System.Threading.Tasks.Task DodajTriggerPolnocny()
        {
            if (!await p.k.CanRegisterTriggersAsync()) return;

            DateTime oDateNew = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 40, 0);
            if (DateTime.Now.Hour > 21)
                oDateNew = oDateNew.AddDays(1);

            uint iMin = (uint)(oDateNew - DateTime.Now).TotalMinutes;

            string sName = GetTriggerPolnocnyName();
            p.k.RegisterTimerTrigger(sName, iMin, false);
        }

        /// <summary>
        /// para z DodajTriggerPolnocny, do wywoływania w OnBackgroundActivated
        /// </summary>
        public static bool IsThisTriggerPolnocny(Windows.ApplicationModel.Activation.BackgroundActivatedEventArgs args)
        {
            string sName = GetTriggerPolnocnyName();
            if (args.TaskInstance.Task.Name != sName) return false;

            // no dobrze, jest to trigger północny, ale czy o północy...
            string sCurrDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SetSettingsString("lastPolnocnyTry", sCurrDate);

            bool bRet = false;
            DateTime oDateNew = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 40, 0);

            if (DateTime.Now.Hour == 23 && DateTime.Now.Minute > 20)    // 40 minut, ale system dodaje ±15 minut!
            {
                // tak, to jest północny o północy
                bRet = true;
                oDateNew = oDateNew.AddDays(1);
                SetSettingsString("lastPolnocnyOk", sCurrDate);
            }
            else
            {
                // północny, ale nie o północy
                bRet = false;
            }
            int iMin = 0;
            iMin = (int)(oDateNew - DateTime.Now).TotalMinutes;

            // Usuwamy istniejący, robimy nowy
            UnregisterTriggers(sName);
            RegisterTimerTrigger(sName, (uint)iMin, false);

            return bRet;
        }


        public static Windows.ApplicationModel.Background.BackgroundTaskRegistration RegisterToastTrigger(string sName)
        {
            try
            {
                var builder = new Windows.ApplicationModel.Background.BackgroundTaskBuilder();

                builder.SetTrigger(new Windows.ApplicationModel.Background.ToastNotificationActionTrigger());
                builder.Name = sName;
                var oRet = builder.Register();
                return oRet;
            }
            catch
            {
                // np. gdy nie ma permissions, to może być FAIL
            }

            return null;
        }

        // ServicingCompleted unimplemted w Uno
#if false
        public static Background.BackgroundTaskRegistration RegisterServicingCompletedTrigger(string sName)
        {
            try
            {
                Background.BackgroundTaskBuilder builder = new Background.BackgroundTaskBuilder();
                Windows.ApplicationModel.Background.BackgroundTaskRegistration oRet;

                builder.SetTrigger(new Background.SystemTrigger(Background.SystemTriggerType.ServicingComplete, true));
                builder.Name = sName;
                oRet = builder.Register();
                return oRet;
            }
            catch (Exception ex)
            {
            }

            return null/* TODO Change to default(_) if this is not a reference type */;
        }
#endif

        #endregion

        #region "RemoteSystem"
        public static bool IsTriggerAppService(Windows.ApplicationModel.Activation.BackgroundActivatedEventArgs args)
        {
#if NETFX_CORE
            Windows.ApplicationModel.AppService.AppServiceTriggerDetails oDetails =
                args.TaskInstance.TriggerDetails as Windows.ApplicationModel.AppService.AppServiceTriggerDetails;
            if (oDetails is null) return false;
            return true;
#else
            return false;
#endif
        }

        public static string AppServiceStdCmd(string sCommand, string sLocalCmds)
        {
            string sTmp = "";

            switch (sCommand.ToLower())
            {
                case "ping":
                    return "pong";

                case "ver":
                    return p.k.GetAppVers();
                case "localdir":
                    return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
                case "appdir":
                    return Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
                case "installeddate":
                    return Windows.ApplicationModel.Package.Current.InstalledDate.ToString("yyyy.MM.dd HH:mm:ss");

                case "help":
                    return "App specific commands:\n" + sLocalCmds;
                case "debug vars":
                    return DumpSettings();
                case "debug triggers":
                    return DumpTriggers();
                case "debug toasts":
                    return DumpToasts();
                case "debug memsize":
                    return Windows.System.MemoryManager.AppMemoryUsage.ToString() + "/" + Windows.System.MemoryManager.AppMemoryUsageLimit.ToString();
                case "debug rungc":
                    sTmp = "Memory usage before Global Collector call: " + Windows.System.MemoryManager.AppMemoryUsage.ToString() + "\n";
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    sTmp = sTmp + "After: " + Windows.System.MemoryManager.AppMemoryUsage.ToString() + "/" + Windows.System.MemoryManager.AppMemoryUsageLimit.ToString();
                    return sTmp;
                case "debug crashmsg":
                    sTmp = GetSettingsString("appFailData", "");
                    if (sTmp == "") sTmp = "No saved crash info";
                    return sTmp;
                case "debug crashmsg clear":
                    sTmp = GetSettingsString("appFailData", "");
                    if (sTmp == "") sTmp = "No saved crash info";
                    SetSettingsString("appFailData", "");
                    return sTmp;
                case "lib unregistertriggers":
                    sTmp = DumpTriggers();
                    UnregisterTriggers(""); // całkiem wszystkie
                    return sTmp;
                case "lib isfamilymobile":
                    return IsFamilyMobile().ToString();
                case "lib isfamilydesktop":
                    return IsFamilyDesktop().ToString();
                case "lib netisipavailable":
                    return NetIsIPavailable(false).ToString();
                case "lib netiscellinet":
                    return NetIsCellInet().ToString();
                case "lib gethostname":
                    return GetHostName();
                case "lib isthismoje":
                    return IsThisMoje().ToString();
                case "lib istriggersregistered":
                    return IsTriggersRegistered().ToString();

                case "lib pkarmode 1":
                    SetSettingsBool("pkarMode", true);
                    return "DONE";
                case "lib pkarmode 0":
                    SetSettingsBool("pkarMode", false);
                    return "DONE";
                case "lib pkarmode":
                    return GetSettingsBool("pkarMode").ToString();
            }

            return "";  // oznacza: to nie jest standardowa komenda
        }

        private static string DumpSettings()
        {
            string sRoam = "";
            try
            {
                foreach (var oVal in Windows.Storage.ApplicationData.Current.RoamingSettings.Values)
                {
                    sRoam += oVal.Key + "\t" + oVal.Value.ToString() + "\n";
                }
            }
            catch { };

            string sLocal = "";
            try
            {
                foreach (var oVal in Windows.Storage.ApplicationData.Current.LocalSettings.Values)
                {
                    sLocal += oVal.Key + "\t" + oVal.Value.ToString() + "\n";
                }
            }
            catch { };

            string sRet = "Dumping Settings\n";
            if (sRoam != "")
                sRet += "\nRoaming:\n" + sRoam;
            else
                sRet += "(no roaming settings)\n";

            if (sLocal != "")
                sRet += "\nLocal:\n" + sLocal;
            else
                sRet += "(no local settings)\n";


            return sRet;
        }

        private static string DumpTriggers()
        {
            string sRet = "";

            try
            {
                foreach (var oTask in Windows.ApplicationModel.Background.BackgroundTaskRegistration.AllTasks)
                {
                    sRet += oTask.Value.Name + "\n";    //GetType niestety nie daje rzeczywistego typu
                }
            }
            catch { };

            if (sRet == "") return "No registered triggers\n";

            return "Dumping Triggers\n\n" + sRet;

        }
        private static string DumpToasts()
        {
            string sResult = "";

            foreach (Windows.UI.Notifications.ScheduledToastNotification oToast
                in Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().GetScheduledToastNotifications())
            {
                sResult = sResult + oToast.DeliveryTime.ToString("yyyy-MM-dd HH:mm:ss") + "\n";
            }

            if (sResult == "")
                sResult = "(no toasts scheduled)";
            else
                sResult = "Toasts scheduled for dates: \n" + sResult;

            return sResult;
        }

        private static async System.Threading.Tasks.Task<string> DumpSDKvers()
        {
            var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync("AppxManifest.XML");
            using (Stream stream = await file.OpenStreamForReadAsync())
            {
                //var doc = XDocument.Load(stream);
                //<Package
                // <Dependencies>
                //  <TargetDeviceFamily Name="Windows.Universal" MinVersion="10.0.14393.0" MaxVersionTested="10.0.19041.0" />

            }
            return "bla";
        }

        #endregion

        #region "Datalog"
#if NETFX_CORE
        private async static System.Threading.Tasks.Task<Windows.Storage.StorageFolder> GetSDcardFolderAsync()
        {
            // uwaga: musi być w Manifest RemoteStorage oraz fileext!

            Windows.Storage.StorageFolder oRootDir;

            try
            {
                oRootDir = Windows.Storage.KnownFolders.RemovableDevices;
            }
            catch
            {
                return null;
            }// brak uprawnień, może być także THROW

            try
            {
                IReadOnlyList<Windows.Storage.StorageFolder> oCards = await oRootDir.GetFoldersAsync();
                return oCards.FirstOrDefault();
            }
            catch
            {
            }

            return null;
        }

#endif

        private async static System.Threading.Tasks.Task<Windows.Storage.StorageFolder> GetLogFolderRootDatalogsOnSDcardAsync()
        {
#if !NETFX_CORE
            return null;
#else
            Windows.Storage.StorageFolder oSdCard = null;
            Windows.Storage.StorageFolder oFold;

            oSdCard = await GetSDcardFolderAsync();

            if (oSdCard is null) return null;

            oFold = await oSdCard.CreateFolderAsync("DataLogs", Windows.Storage.CreationCollisionOption.OpenIfExists);
            if (oFold == null) return null;

            string sAppName = Windows.ApplicationModel.Package.Current.DisplayName;
            sAppName = sAppName.Replace(" ", "").Replace("'", "");
            oFold = await oFold.CreateFolderAsync(sAppName, Windows.Storage.CreationCollisionOption.OpenIfExists);

            return oFold;
#endif
        }

        private async static System.Threading.Tasks.Task<Windows.Storage.StorageFolder> GetLogFolderRootDatalogsInAppAsync()
        {
            Windows.Storage.StorageFolder oSdCard = null;
            Windows.Storage.StorageFolder oFold;

            oSdCard = Windows.Storage.ApplicationData.Current.LocalFolder;
            oFold = await oSdCard.CreateFolderAsync("DataLogs", Windows.Storage.CreationCollisionOption.OpenIfExists);
            return oFold;
        }

        public async static System.Threading.Tasks.Task<Windows.Storage.StorageFolder> GetLogFolderRootAsync(bool bUseOwnFolderIfNotSD = true)
        {
            Windows.Storage.StorageFolder oFold = null;

#if NETFX_CORE
            if (IsFamilyMobile())
            {
                oFold = await GetLogFolderRootDatalogsOnSDcardAsync();
            }
            if (oFold is object) return oFold;
#endif
            // albo w UWP nie ma karty, albo poza UWP
            if (!bUseOwnFolderIfNotSD) return null;
            oFold = await GetLogFolderRootDatalogsInAppAsync();

            return oFold;
        }


        public async static System.Threading.Tasks.Task<Windows.Storage.StorageFolder> GetLogFolderYearAsync(bool bUseOwnFolderIfNotSD = true)
        {
            Windows.Storage.StorageFolder oFold = await GetLogFolderRootAsync(bUseOwnFolderIfNotSD);
            if (oFold == null)
                return null;
            oFold = await oFold.CreateFolderAsync(DateTime.Now.ToString("yyyy"), Windows.Storage.CreationCollisionOption.OpenIfExists);
            return oFold;
        }

        public async static System.Threading.Tasks.Task<Windows.Storage.StorageFolder> GetLogFolderMonthAsync(bool bUseOwnFolderIfNotSD = true)
        {
            Windows.Storage.StorageFolder oFold = await GetLogFolderYearAsync(bUseOwnFolderIfNotSD);
            if (oFold == null)
                return null;

            oFold = await oFold.CreateFolderAsync(DateTime.Now.ToString("MM"), Windows.Storage.CreationCollisionOption.OpenIfExists);
            return oFold;
        }

        public async static System.Threading.Tasks.Task<Windows.Storage.StorageFile> GetLogFileMonthlyAsync(string sBaseName, string sExtension, bool bUseOwnFolderIfNotSD = true)
        {
            Windows.Storage.StorageFolder oFold = await GetLogFolderYearAsync(bUseOwnFolderIfNotSD);
            if (oFold is null) return null;

            if (string.IsNullOrEmpty(sExtension)) sExtension = ".txt";
            if (!sExtension.StartsWith(".")) sExtension = "." + sExtension;

            string sFile;
            if (string.IsNullOrEmpty(sBaseName))
                sFile = DateTime.Now.ToString("yyyy.MM") + sExtension;
            else
                sFile = sBaseName + " " + DateTime.Now.ToString("yyyy.MM") + sExtension;

            return await oFold.CreateFileAsync(sFile, Windows.Storage.CreationCollisionOption.OpenIfExists);
        }


        public async static System.Threading.Tasks.Task<Windows.Storage.StorageFile> GetLogFileDailyAsync(string sBaseName, string sExtension, bool bUseOwnFolderIfNotSD = true)
        {
            Windows.Storage.StorageFolder oFold = await GetLogFolderMonthAsync(bUseOwnFolderIfNotSD);
            if (oFold == null)
                return null;

            if (!sExtension.StartsWith("."))
                sExtension = "." + sExtension;

            string sFile = sBaseName + " " + DateTime.Now.ToString("yyyy.MM.dd") + sExtension;
            return await oFold.CreateFileAsync(sFile, Windows.Storage.CreationCollisionOption.OpenIfExists);
        }

        public async static System.Threading.Tasks.Task<Windows.Storage.StorageFile> GetLogFileDailyAsync(string sFileName, bool bUseOwnFolderIfNotSD = true)
        {
            Windows.Storage.StorageFolder oFold = await GetLogFolderMonthAsync(bUseOwnFolderIfNotSD);
            if (oFold == null)
                return null;

            return await oFold.CreateFileAsync(sFileName, Windows.Storage.CreationCollisionOption.OpenIfExists);
        }

        #endregion

        public static void DebugBTprintArray(byte[] aArr, int iSpaces)
        {
            string sPrefix = "";
            for (int i = 1; i <= iSpaces; i++)
                sPrefix += " ";


            if (aArr.Length > 6)
                System.Diagnostics.Debug.WriteLine(sPrefix + "length: " + aArr.Length);

            string sBytes = "";
            string sAscii = sBytes;

            for (int i = 0; i <= Math.Min(aArr.Length - 1, 32); i++) // bylo oVal
            {
                byte cBajt = aArr.ElementAt(i);

                // hex: tylko 16 bajtow
                if (i < 16)
                {
                    try
                    {
                        sBytes = sBytes + " 0x" + string.Format("{0:X}", cBajt);
                    }
                    catch
                    {
                        sBytes = sBytes + " ??";
                    }
                }

                // ascii: do 32 bajtow
                if (cBajt > 31 & cBajt < 160)
                    sAscii = sAscii + Convert.ToChar(cBajt);
                else
                    sAscii = sAscii + "?";
            }

            if (aArr.Length - 1 > 16)
                sBytes = sBytes + " ...";
            if (aArr.Length - 1 > 32)
                sAscii = sAscii + " ...";

            System.Diagnostics.Debug.WriteLine(sPrefix + "binary: " + sBytes);
            System.Diagnostics.Debug.WriteLine(sPrefix + "ascii:  " + sAscii);
        }

        // duża seria funkcji Bluetooth - nieprzenoszona, bo i tak nie ma Bluetooth w Uno

        public static ulong MacStringToULong(string sStr)
        {
            if (string.IsNullOrEmpty(sStr)) throw new ArgumentNullException("sStr", "MacStringToULong powinno miec parametr");
            if (!sStr.Contains(":")) throw new ArgumentException("MacStringToULong - nie ma dwukropków w sStr");

            sStr = sStr.Replace(":", "");
            ulong uLng = ulong.Parse(sStr, System.Globalization.NumberStyles.HexNumber);

            return uLng;
        }


        public static Windows.Devices.Geolocation.BasicGeoposition GetDomekGeopos(UInt16 iDecimalDigits = 0)
        {
            Windows.Devices.Geolocation.BasicGeoposition oTestPoint = new Windows.Devices.Geolocation.BasicGeoposition();
            //' 50.01985 
            //' 19.97872

            switch (iDecimalDigits)
            {
                case 1:
                    oTestPoint.Latitude = 50.0;
                    oTestPoint.Longitude = 19.9;
                    break;
                case 2:
                    oTestPoint.Latitude = 50.01;
                    oTestPoint.Longitude = 19.97;
                    break;
                case 3:
                    oTestPoint.Latitude = 50.019;
                    oTestPoint.Longitude = 19.978;
                    break;
                case 4:
                    oTestPoint.Latitude = 50.0198;
                    oTestPoint.Longitude = 19.9787;
                    break;
                case 5:
                    oTestPoint.Latitude = 50.01985;
                    oTestPoint.Longitude = 19.97872;
                    break;
                default:
                    oTestPoint.Latitude = 50.0;
                    oTestPoint.Longitude = 20.0;
                    break;
            }

            return oTestPoint;
        }


        public static async System.Threading.Tasks.Task<bool> IsFullVersion()
        {
#if DEBUG
            return true;
#else
#if !NETFX_CORE
            return false;
#else
            // if(IsThisMoje()) return true;

            if (!Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Services.Store.StoreContext"))
                return false;

            var oLicencja = await Windows.Services.Store.StoreContext.GetDefault().GetAppLicenseAsync();
            if (!oLicencja.IsActive) return false; // bez licencji? jakżeż to możliwe?
            if (oLicencja.IsTrial) return false;
            return true;
#endif
#endif
        }
    }
}


    static partial class Extensions
    {

        #region "Zapis/Odczyt tekstu z pliku"

#if NETFX_CORE
    public static async System.Threading.Tasks.Task WriteAllTextAsync(this Windows.Storage.StorageFile oFile, string sTxt)
    {
        System.IO.Stream oStream = await oFile.OpenStreamForWriteAsync(); 
        Windows.Storage.Streams.DataWriter oWriter = new Windows.Storage.Streams.DataWriter(oStream.AsOutputStream()); // <- NO UNO
        oWriter.WriteString(sTxt);
        await oWriter.FlushAsync();
        await oWriter.StoreAsync(); // <- NO UNO
        oWriter.Dispose();
        // oStream.Flush()
        // oStream.Dispose()
    }

    public static async System.Threading.Tasks.Task<string> ReadAllTextAsync(this Windows.Storage.StorageFile oFile)
    {
        // ' zamiast File.ReadAllText(oFile.Path)
        Stream oStream = await oFile.OpenStreamForReadAsync();
        Windows.Storage.Streams.DataReader oReader = new Windows.Storage.Streams.DataReader(oStream.AsInputStream()); // <- NO UNO
        uint iSize = (uint)oStream.Length;
        await oReader.LoadAsync(iSize); // <- NO UNO
        string sTxt = oReader.ReadString(iSize);
        oReader.Dispose();
        oStream.Dispose();
        return sTxt;
    }

#elif __ANDROID__
        public static async System.Threading.Tasks.Task<string> ReadAllTextAsync(this Windows.Storage.StorageFile oFile)
        { // niby wersja dużo prostsza, bez Readerów
            return await Windows.Storage.FileIO.ReadTextAsync(oFile);
        }

        public static async System.Threading.Tasks.Task WriteAllTextAsync(this Windows.Storage.StorageFile oFile, string sTxt)
        {
            await Windows.Storage.FileIO.WriteTextAsync(oFile, sTxt);
        }
#endif

        public static async System.Threading.Tasks.Task WriteAllTextToFileAsync(this Windows.Storage.StorageFolder oFold, string sFileName, string sTxt, Windows.Storage.CreationCollisionOption oOption = Windows.Storage.CreationCollisionOption.FailIfExists)
        { // wymaga powyzszej funkcji
            Windows.Storage.StorageFile oFile = await oFold.CreateFileAsync(sFileName, oOption);
            if (oFile is null) return;
            await oFile.WriteAllTextAsync(sTxt);
        }

        public static async System.Threading.Tasks.Task<string> ReadAllTextFromFileAsync(this Windows.Storage.StorageFolder oFold, string sFileName)
        {
            Windows.Storage.StorageFile oFile = await oFold.TryGetItemAsync(sFileName) as Windows.Storage.StorageFile;
            if (oFile is null) return null;
            return await oFile.ReadAllTextAsync();
        }

        /// <summary>
        /// appenduje string, i dodaje vbCrLf
        /// </summary>
        public static async System.Threading.Tasks.Task AppendLineAsync(this Windows.Storage.StorageFile oFile, string sTxt)
        {
            await oFile.AppendStringAsync(sTxt + "\n");
        }

        /// <summary>
        /// appenduje string, nic nie dodając. Zwraca FALSE gdy nie udało się otworzyć pliku.
        /// </summary>
        public static async System.Threading.Tasks.Task<bool> AppendStringAsync(this Windows.Storage.StorageFile oFile, string sTxt)
        {
            if (string.IsNullOrEmpty(sTxt)) return true;

            Stream oStream = null;
            try
            {
                oStream = await oFile.OpenStreamForWriteAsync();
            }
            catch
            {
                return false; // ' mamy błąd otwarcia pliku
            }

            oStream.Seek(0, SeekOrigin.End);
            Windows.Storage.Streams.DataWriter oWriter = new Windows.Storage.Streams.DataWriter(oStream.AsOutputStream());
            oWriter.WriteString(sTxt);
            await oWriter.FlushAsync();
#if NETFX_CORE
            await oWriter.StoreAsync();
#endif
            oWriter.Dispose();

            return true;
            //'oStream.Flush() - already closed
            //'oStream.Dispose()
        }

        #endregion

        #region "GPS related"
        public static double DistanceTo(this Windows.Devices.Geolocation.BasicGeoposition oPos0, Windows.Devices.Geolocation.BasicGeoposition oPos)
        {
            int iRadix = 6371000;
            double tLat = (oPos.Latitude - oPos0.Latitude) * Math.PI / 180;
            double tLon = (oPos.Longitude - oPos0.Longitude) * Math.PI / 180;
            double a = Math.Sin(tLat / 2) * Math.Sin(tLat / 2) + Math.Cos(Math.PI / 180 * oPos0.Latitude) * Math.Cos(Math.PI / 180 * oPos.Latitude) * Math.Sin(tLon / 2) * Math.Sin(tLon / 2);
            double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            double d = iRadix * c;

            return d;
        }

        public static double DistanceTo(this Windows.Devices.Geolocation.Geoposition oGeopos0, Windows.Devices.Geolocation.Geoposition oGeopos1)
        {
            return oGeopos0.Coordinate.Point.Position.DistanceTo(oGeopos1.Coordinate.Point.Position);
        }

        public static double DistanceTo(this Windows.Devices.Geolocation.BasicGeoposition oPos0, double dLatitude, double dLongitude)
        {
            int iRadix = 6371000;
            double tLat = (dLatitude - oPos0.Latitude) * Math.PI / 180;
            double tLon = (dLongitude - oPos0.Longitude) * Math.PI / 180;
            double a = Math.Sin(tLat / 2) * Math.Sin(tLat / 2) + Math.Cos(Math.PI / 180 * oPos0.Latitude) * Math.Cos(Math.PI / 180 * dLatitude) * Math.Sin(tLon / 2) * Math.Sin(tLon / 2);
            double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            double d = iRadix * c;

            return d;
        }

        /// <summary>
        /// Czy punkt leży w miarę w Polsce (<500 km od środka geometrycznego Polski)
        /// </summary>
        public static bool IsInsidePoland(this Windows.Devices.Geolocation.BasicGeoposition oPos)
        {// https://pl.wikipedia.org/wiki/Geometryczny_%C5%9Brodek_Polski

            Windows.Devices.Geolocation.BasicGeoposition oSrodek = p.k.NewGeoPos(52.2159333, 19.1344222);

            double dOdl;
            dOdl = oPos.DistanceTo(oSrodek);
            if (dOdl / 1000 > 500) return false;

            return true;    // ale to nie jest pewne, tylko: "możliwe"
        }

        #endregion


        #region "konwersje string/double"
        public static double ParseDouble(this String sVariable, double dDefault)
        {
            double dDouble;
            if (!double.TryParse(sVariable, out dDouble))
                dDouble = dDefault;
            return dDouble;
        }

        public static int MinMax(this int iValue, int iMin, int iMax)
        {
            int dTmp = iValue;
            dTmp = Math.Max(iMin, dTmp);
            dTmp = Math.Min(dTmp, iMax);
            return dTmp;
        }

        public static double MinMax(this double dDouble, double dMin, double dMax)
        {
            double dTmp = dDouble;
            dTmp = Math.Max(dMin, dTmp);
            dTmp = Math.Min(dTmp, dMax);
            return dTmp;
        }

#if NETFX_CORE
        public static void OpenExplorer(this Windows.Storage.StorageFolder oFold)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Windows.System.Launcher.LaunchFolderAsync(oFold);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
#endif 

        public static async System.Threading.Tasks.Task<bool> FileExistsAsync(this Windows.Storage.StorageFolder oFold, string sFileName)
        {
            try
            {
                Windows.Storage.StorageFile oTemp = (Windows.Storage.StorageFile)await oFold.TryGetItemAsync(sFileName);
                if (oTemp is null) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string ToHexBytesString(this ulong iVal)
        {
            string sTmp = string.Format("{0:X}", iVal);
            if (sTmp.Length % 2 != 0)
                sTmp = "0" + sTmp;

            string sRet = "";
            bool bDwukrop = false;

            while (sTmp.Length > 0)
            {
                if (bDwukrop)
                    sRet += ":";
                bDwukrop = true;
                sRet = sRet + sTmp.Substring(0, 2);
                sTmp = sTmp.Substring(2);
            }

            //' gniazdko BT18, daje 15:A6:00:E8:07 (bez 00:)
            //' 71:0A:22:CD:4F:20
            //' 12345678901234567
            if (sRet.Length < 17) sRet = "00:" + sRet;
            if (sRet.Length < 17) sRet = "00:" + sRet;

            return sRet;
        }

        ///<summary>
        ///Zwraca od sStart do końca (lub bez zmian, gdy nie ma sStart)
        ///</summary>
        public static string TrimBefore(this string baseString, string sStart)
        {
            int iInd = baseString.IndexOf(sStart);
            if (iInd < 0) return baseString;
            return baseString.Substring(iInd);
        }

        ///<summary>
        /// Zwraca od początku do sEnd do końca (lub bez zmian, gdy nie ma sEnd)
        ///</summary>
        public static string TrimAfter(this string baseString, string sEnd)
        {
            int iInd = baseString.IndexOf(sEnd);
            if (iInd < 0) return baseString;
            return baseString.Substring(0, iInd + sEnd.Length);
        }

        ///<summary>
        /// Zwraca od sStart do końca (lub bez zmian, gdy nie ma sStart) - szuka od końca
        ///</summary>
        public static string TrimBeforeLast(this string baseString, string sStart)
        {
            int iInd = baseString.LastIndexOf(sStart);
            if (iInd < 0) return baseString;
            return baseString.Substring(iInd);
        }

        ///<summary>
        /// Zwraca od początku do sEnd do końca (lub bez zmian, gdy nie ma sEnd) - szuka od końca
        ///</summary>
        public static string TrimAfterLast(this string baseString, string sEnd)
        {
            int iInd = baseString.LastIndexOf(sEnd);
            if (iInd < 0) return baseString;
            return baseString.Substring(0, iInd + sEnd.Length);
        }

        ///<summary>
        /// Wycina fragment od sStart do sEnd, jeśli któregoś nie ma - nie tyka
        ///</summary>
        public static string RemoveBetween(this string baseString, string sStart, string sEnd)
        {
            int iIndS = baseString.IndexOf(sStart);
            if (iIndS < 0) return baseString;
            int iIndE = baseString.IndexOf(sEnd);
            if (iIndE < 0) return baseString;
            return baseString.Remove(iIndS, iIndE - iIndS + 1);
        }

#endregion

        public async static System.Threading.Tasks.Task<string> GetDocumentHtml(this Windows.UI.Xaml.Controls.WebView uiWebView)
        {
            try
            {
                return await uiWebView.InvokeScriptAsync("eval", new string[] { "document.documentElement.outerHTML;" });
            }
            catch
            {
                return "";
            }// jesli strona jest pusta, jest Exception
        }


    }

