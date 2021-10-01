using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
using Newtonsoft.Json;
/*
                    ZBIORCZA

            zebranie w jeden plik wszystko z *VB
        do kontroli co jest, a czego nie ma w UNO
        w sensie: warning UNO0001


        Pomijam to, co jest w:
            * app robionych regularnie (MazurCiC, Anniversaries, KrakTram, EnviroStatus, CallStat, BackupSMS
            * pkModuleShared.cs 
             
        Upraszczam kod, tak zeby bylo tylko to, co jest w Uno 3.7.0-dev144 zaznaczone jako NIEMA
            (sprawdzam Android, iOS, macOS, WASM - wszystkie HEAD jakie zrobilo)


I'm using it in my app that I want to convert from UWP/VB to Uno/C#

(ideally, for all platforms)

*/

namespace TestProgressRing.Shared
{

    public sealed partial class Zbiorcza : Windows.UI.Xaml.Controls.Page
    {
        public Zbiorcza()
        {
            
        }
    }

#pragma warning disable CS0168 // Variable is declared but never used
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed


    // WLASNE - wyciete z calosci, dla uproszczenia
    public class CommonFromApp
    {
        public void Commonki(Windows.ApplicationModel.Activation.LaunchActivatedEventArgs e)
        {
            // WASM IOS MACOS .PreviousExecutionState
            if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
            {
            }

            // WASM IOS MACOS .PrelaunchActivated
            if (e.PrelaunchActivated == false)
            {
            }
        }
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            // WASM IOS MACOS .GetDeferral()
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();
            // WASM IOS MACOS .Complete()
            deferral.Complete();
        }

        private Windows.ApplicationModel.Background.BackgroundTaskDeferral moTimerDeferal = null/* TODO Change to default(_) if this is not a reference type */;

        protected async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            // WASM IOS MACOS .TaskInstance
            // WASM IOS MACOS .GetDeferral()
            moTimerDeferal = args.TaskInstance.GetDeferral();
            // WASM IOS MACOS .Complete()
            moTimerDeferal.Complete();
        }


    }

    #region AccuMon
    public sealed partial class AccuMonApp : Windows.UI.Xaml.Application
    {

        public static async Task SprawdzStanBateryjki()
        {
            // ISSUE #5906
            string sTxt;
            // ALL Windows.Devices.Power.Battery*
            Windows.Devices.Power.BatteryReport oBattRep = Windows.Devices.Power.Battery.AggregateBattery.GetReport();

            sTxt = oBattRep.Status.ToString();
            sTxt = sTxt + " " + oBattRep.RemainingCapacityInMilliwattHours + "/" + oBattRep.FullChargeCapacityInMilliwattHours + " (design: " + oBattRep.DesignCapacityInMilliwattHours + ")";

        }

        public static async Task AppendLogFile(string sLogLine)
        {

            Windows.Storage.StorageFile oFile = await p.k.GetLogFileDailyAsync("accumon", "txt");
            // WASM IOS MACOS Windows.Storage.FileIO
            // WASM IOS MACOS Windows.Storage.FileIO.AppendTextAsync
            await Windows.Storage.FileIO.AppendTextAsync(oFile, sLogLine);
        }
    }
    #endregion
    #region AcerDLP

public sealed class AcerDLPMainPage 
    {
        private void PlayStream(MemoryStream mStrWav)
        {
            mStrWav.Seek(0, SeekOrigin.Begin);
            // WASM IOS MACOS .AsRandomAccessStream()
            Windows.Storage.Streams.IRandomAccessStream soundSource = mStrWav.AsRandomAccessStream();
            // WASM IOS MACOS .Seek
            soundSource.Seek(0);
        }

    }

    #endregion
    #region AliFreebie

public sealed class AliFreebieMainPage : Page
    {

        private async Task BrowseFolderAsync()
        {
            // WASM: Windows.Storage.Pickers.FolderPicker.*
            var picker = new Windows.Storage.Pickers.FolderPicker();
            // WASM Windows.Storage.Pickers.PickerViewMode
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            // WASM Windows.Storage.Pickers.PickerLocationId
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add(".png");

            Windows.Storage.StorageFolder oFold;
            // WASM: Windows.Storage.Pickers.FolderPicker.*
            oFold = await picker.PickSingleFolderAsync();

            // WASM, ANDRO Windows.Storage.AccessCache.StorageApplicationPermissions
            Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", oFold);
        }

        private async void uiEmail_Click(object sender, RoutedEventArgs e)
        {

            Windows.ApplicationModel.Email.EmailMessage oMsg = new Windows.ApplicationModel.Email.EmailMessage();
            oMsg.Subject = "AliFreebie";

            string sTxt = "Freebie z dnia: ";

            oMsg.Body = sTxt;

            // załączniki działają tylko w default windows mail app
            Windows.Storage.StorageFile oFile = await Windows.Storage.StorageFile.GetFileFromPathAsync("");
            var oStream = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromFile(oFile);

            // WASM DROID Windows.ApplicationModel.Email.EmailAttachment*
            var oAttch = new Windows.ApplicationModel.Email.EmailAttachment("oItem.sFileName", oStream);
            // WASM DROID .Attachments
            oMsg.Attachments.Add(oAttch);

            await Windows.ApplicationModel.Email.EmailManager.ShowComposeNewEmailAsync(oMsg);
        }


        private void uiOpenExpl_Click(object sender, RoutedEventArgs e)
        {
            Windows.Storage.StorageFolder oFold = Windows.Storage.ApplicationData.Current.LocalFolder;
            // DROID Windows.System.Launcher.LaunchFolderAsync
	// ISSUE #5910
            Windows.System.Launcher.LaunchFolderAsync(oFold);
        }

    }

    #endregion
    #region Ballots


    public class BallotsApp
    {

        public async void GetMyFolder()
        {
            Windows.Storage.StorageFolder oFold;
            oFold = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFolderAsync("PickedFolderToken");
        }

        public static async void OpenXLS()
        {
            var sFile = "file:///IEEEnormy.xlsm";
            Windows.System.Launcher.LaunchUriAsync(new Uri(sFile));
        }


        // RemoteSystems
        protected async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            // tile update / warnings
            var moTaskDeferal = args.TaskInstance.GetDeferral(); // w pkarmodule.App

        }

        // CommandLine, Toasts
        protected async void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.CommandLineLaunch)
            {
                CommandLineActivatedEventArgs commandLine = args as CommandLineActivatedEventArgs;
                CommandLineActivationOperation operation = commandLine?.Operation;
                string strArgs = operation?.Arguments;

                Window.Current.Close();
            }
        }
    }




    public class ieeeBallots 
    {

        private async void uiDownload_Click(object sender, RoutedEventArgs e)
        {
            Windows.Web.Http.HttpResponseMessage oResp = null/* TODO Change to default(_) if this is not a reference type */;
            Windows.Storage.StorageFile oLocalFile1 = null;
            Stream oStream1 = await oLocalFile1.OpenStreamForWriteAsync();
            await oResp.Content.WriteToStreamAsync(oStream1.AsOutputStream());
            oStream1.Flush();
            oStream1.Dispose();
        }

        private async void uiVote_Click(object sender, RoutedEventArgs e)
        {
            Windows.Web.Http.HttpClient oIeeeHttp = null;
            Windows.Web.Http.HttpResponseMessage oResp = null/* TODO Change to default(_) if this is not a reference type */;
            var oHttpCont = new Windows.Web.Http.HttpStringContent("", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
            oResp = await oIeeeHttp.PostAsync(new Uri(""), oHttpCont);

            string sPage = await oResp.Content.ReadAsStringAsync();
        }
    }

public class BallotsMainPage : Page
    {
        private async void uiSetFolder_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add(".png");

            Windows.Storage.StorageFolder oFold;
            oFold = await picker.PickSingleFolderAsync();
            if (oFold == null)
                return;

            Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", oFold);
        }

    }


public sealed class PknPublic 
    {
        private Windows.Web.Http.HttpClient moHttpClient;

        private async void DownloadFile()
        {

            string sUri = "https://pzn.pkn.pl/polls/polls/api/polling-file/" ;

            Windows.Web.Http.HttpResponseMessage oResp = null/* TODO Change to default(_) if this is not a reference type */;
            if (moHttpClient == null)
                moHttpClient = new Windows.Web.Http.HttpClient();

            oResp = await moHttpClient.GetAsync(new Uri(sUri));
            if ((int)oResp.StatusCode > 290)
            {
                string aaa = "... ERROR http standard page: " + oResp.StatusCode;
                string aaa1 = "Error podczas ściagania pliku" + oResp.ReasonPhrase;
            }

            Windows.Storage.StorageFile oLocalFile = null/* TODO Change to default(_) if this is not a reference type */;

           try
            {
                Stream oStream = await oLocalFile.OpenStreamForWriteAsync();
                await oResp.Content.WriteToStreamAsync(oStream.AsOutputStream());
                oStream.Flush();
                oStream.Dispose();
            }
            catch (Exception ex)
            {
            }

        }

    }


    #endregion
    #region BarCodeLike




    public class ListaItems
    {

        public async void SaveItemsAsync()
        {
            Windows.Storage.StorageFolder oFold = Windows.Storage.ApplicationData.Current.RoamingFolder;
            string sTxt = "";
            await oFold.WriteAllTextToFileAsync("items.json", sTxt, Windows.Storage.CreationCollisionOption.ReplaceExisting);
        }

        public async void LoadItemsAsync()
        {
            string sTxt = await Windows.Storage.ApplicationData.Current.RoamingFolder.ReadAllTextFromFileAsync("items.json");
        }
    }


    #endregion
    #region Biblia 

    // nic specjalnego

    #endregion
    #region Brewiarz

    // nic specjalnego

    #endregion

    // dotąd

    #region CeleZyciowe
    #endregion
    #region ComicStrip
    #endregion
    #region ComixInMyLang
    #endregion
    #region DailyItinerary
    #endregion
    #region DevDashboard
    #endregion
    #region FilteredRss
    #endregion
    #region FotoRamka
    #endregion
    #region FSTmon
    #endregion
    #region GrajCyganie
    #endregion
    #region GutenbergUWP
    #endregion
    #region InstaMonitor
    #endregion
    #region Kataloger
    #endregion
    #region ListaZakupowa
    #endregion
    #region Lupka
    #endregion
    #region LycaIle
    #endregion
    #region MediumCalTile
    #endregion
    #region miniHeyBand
    #endregion
    #region MTPgetter
    #endregion
    #region MyCameras
    #endregion
    #region NntpAutoGet
    #endregion
    #region NullTile
    #endregion
    #region OjStrzez
    #endregion
    #region PCmak
    #endregion
    #region PilnowanieBT
    #endregion
    #region pkarcmd
    #endregion
    #region Pociagi
    #endregion
    #region PrzypomnijTu
    #endregion
    #region RandkaMulti
    #endregion
    #region RgbLed
    #endregion
    #region SkanowanieZdjec
    #endregion
    #region StereoViewer
    #endregion
    #region SzukajKumpla
    #endregion
    #region SzukajPicka
    #endregion
    #region SzumnySen
    #endregion
    #region TrackingCainiao
    #endregion
    #region TramTicket
    #endregion
    #region UnicodeDecoder
    #endregion
    #region Waga
    #region AirBox
    #endregion
    #region MijiaThermo
    #endregion
    #region Waga
    #endregion
    #region WeedQbPlug
    #endregion
    #endregion
    #region WezPigulke
    #endregion
    #region Wycofania

    public sealed class Detailsy : Page
    {

        // https://inthehand.com/2015/08/20/add-sharing-to-your-uwp-app/
        private void uiShare_Click(object sender, RoutedEventArgs e)
        {
            Windows.ApplicationModel.DataTransfer.DataTransferManager.GetForCurrentView().DataRequested += SzarnijDane;
            Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI();
        }

        private void SzarnijDane(DataTransferManager sender, DataRequestedEventArgs args)
        {
		// ISSUE #5913
            args.Request.FailWithDisplayText("Nothing to share");

            args.Request.Data.SetHtmlFormat("");
            args.Request.Data.Properties.Title = Windows.ApplicationModel.Package.Current.DisplayName;
        }
    }



    public class KonwersjaIkonki 
    {
        public void Convert(object value, Type targetType, object parameter, System.String language)
        {

            Uri oUri = new Uri("ms-appx:///Assets/icon-.png");
            Windows.UI.Xaml.Media.Imaging.BitmapImage oBmpImg = new Windows.UI.Xaml.Media.Imaging.BitmapImage(oUri);
        }

    }




public class Source_Rapex 
    {


        private async void RapexGetResultPageAsync(int iPageNo, bool bMsg)
        {
            string sUrl = "https://ec.europa.eu/safety-gate-alerts/public/api/notification/carousel/?language=pl";
            string sData = "{\"language\":\"pl\",\"page\":\"" + iPageNo + "\"}";

		// ISSUE #5915
            Windows.Web.Http.HttpClient moHttp = new Windows.Web.Http.HttpClient();
            var oHttpCont = new Windows.Web.Http.HttpStringContent(sData, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
            moHttp.DefaultRequestHeaders.Accept.Clear();
            moHttp.DefaultRequestHeaders.Accept.Add(new Windows.Web.Http.Headers.HttpMediaTypeWithQualityHeaderValue("application/json"));
            moHttp.DefaultRequestHeaders.Accept.Add(new Windows.Web.Http.Headers.HttpMediaTypeWithQualityHeaderValue("text/plain"));
            moHttp.DefaultRequestHeaders.Accept.Add(new Windows.Web.Http.Headers.HttpMediaTypeWithQualityHeaderValue("*/*"));

            Windows.Web.Http.HttpResponseMessage oResp = null;

            oResp = await moHttp.PostAsync(new Uri(sUrl), oHttpCont);


            string sResp = "";
                sResp = await oResp.Content.ReadAsStringAsync();

        }
    }


public class Source_RASFF 
    {
        protected async Task ReadDataMain(bool bMsg)
        {
		// ISSUE #5914
            Windows.Web.Syndication.SyndicationClient oRssClnt = new Windows.Web.Syndication.SyndicationClient();
            Windows.Web.Syndication.SyndicationFeed oRssFeed = null;
                oRssFeed = await oRssClnt.RetrieveFeedAsync(new Uri("https://webgate.ec.europa.eu/rasff-window/consumers/?event=rss&country=PL"));

            if (oRssFeed.Items.Count < 1)
            {
            }

            foreach (Windows.Web.Syndication.SyndicationItem oRssItem in oRssFeed.Items)
            {
                string oNew_sLink = oRssItem.Links.ElementAt(0).Uri.AbsoluteUri.ToString();

                string oNew_sTitle = oRssItem.Title.Text;

                string oNew_sData = oRssItem.Summary.Text;   // w Atom summary, w Rss description; feed nie ma daty jako daty :)

            }
        }
    }


public class Source_UOKIK 
    {
        protected async Task ReadDataMain(bool bMsg)
        {
		// ISSUE #5914

            Windows.Web.Syndication.SyndicationClient oRssClnt = new Windows.Web.Syndication.SyndicationClient();
            oRssClnt.BypassCacheOnRetrieve = true;
            Windows.Web.Syndication.SyndicationFeed oRssFeed;
            oRssFeed = await oRssClnt.RetrieveFeedAsync(new Uri("https://www.uokik.gov.pl/rss/5.xml"));

            foreach (Windows.Web.Syndication.SyndicationItem oRssItem in oRssFeed.Items)
            {

                string oNew_sTitle = oRssItem.Title.Text;

                string oNew_sLink = oRssItem.Links.ElementAt(0).Uri.AbsoluteUri.ToString();

                string oNew_sData = oRssItem.PublishedDate.ToString("yyyy.MM.dd HH:mm:ss");

                string oNew_sHtmlInfo = oRssItem.Summary.Text;   // w Atom summary, w Rss description

            }
        }
    }


public class Source_UOKIK_Reg 
    {
        protected async Task ReadDataMain(bool bMsg)
        {

            System.Xml.XmlDocument oXml = new System.Xml.XmlDocument();
            oXml.LoadXml("");
            System.Xml.XmlNodeList oElems = oXml.GetElementsByTagName("tbody");
            if (oElems.Count != 1)
            {
            }

            foreach (System.Xml.XmlNode oRow in oElems.Item(0).ChildNodes)
            {
                System.Xml.XmlNode oTD2 = oRow.ChildNodes.Item(1);  // <tr..><td><img></td><td>......
                System.Xml.XmlNode oH2 = oTD2.ChildNodes.Item(0); // <td><h2><a ...>title...</a>
                string oNew_sTitle = oH2.InnerText;

                System.Xml.XmlNode oAelem = oH2.ChildNodes.Item(0);
                string oNew_sLink = "http://publikacje.uokik.gov.pl/hermes3_pub/" + oAelem.Attributes.GetNamedItem("href").InnerText;

                string oNew_sId = oTD2.ChildNodes.Item(3).InnerText;

            }
        }
    }


public class Source_Base
    {
        public virtual void ConfigCreate(StackPanel oStack)
        {
            TextBlock oTB = new TextBlock();

            HyperlinkButton oLnk = new HyperlinkButton();

            ToggleSwitch oTS = new ToggleSwitch();

            oTS = new ToggleSwitch();

            var oBindExpr = oTS.GetBindingExpression(ToggleSwitch.IsOnProperty);
            Windows.UI.Xaml.Data.Binding oBind = new Windows.UI.Xaml.Data.Binding();
            oBind.ElementName = oTS.Name;
            oBind.Path = new PropertyPath("IsOn");

            oTS = new ToggleSwitch();
            oTS.SetBinding(ToggleSwitch.IsEnabledProperty, oBind);

            Windows.UI.Xaml.Shapes.Rectangle oKreska = new Windows.UI.Xaml.Shapes.Rectangle();
            oKreska.Name = "uiConfig_";
            oKreska.Height = 1;
            oKreska.HorizontalAlignment = HorizontalAlignment.Stretch;
            oKreska.Margin = new Thickness(30, 5, 30, 5);
            oKreska.Stroke = new SolidColorBrush(Windows.UI.Colors.Blue);
            oStack.Children.Add(oKreska);
        }


        public virtual void ConfigSave(StackPanel oStack)
        {
            foreach (UIElement oItem in oStack.Children)
            {
            }
        }


    }




    #endregion
    #region ZegarSloneczny
    #endregion
}
