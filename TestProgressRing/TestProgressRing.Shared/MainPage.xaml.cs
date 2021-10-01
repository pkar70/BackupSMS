using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TestProgressRing
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

        private void GuzikiEnable(bool bIsEnabled)
        {
            uiReadAll.IsEnabled = bIsEnabled;
            //uiReadSinceLast.IsEnabled = bIsEnabled;
#if __ANDROID__
            // powtórka - bo teraz może już będzie znało rozmiary
            double dVal;
            dVal = (Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth)) / 2;
            // for Android
            if (dVal < 1) dVal = 100;

            uiProcesuje.Width = dVal;
            uiProcesuje.Height = dVal;

#endif 
            uiProcesuje.Visibility = bIsEnabled ? Visibility.Collapsed : Visibility.Visible;
            uiProcesuje.IsActive = !bIsEnabled;

        }


        private string MimeMapOneExt(string sExt)
        {
            return sExt + "\t" + Android.Webkit.MimeTypeMap.Singleton.GetMimeTypeFromExtension(sExt) + "\n";
        }
        private void DumpMiMeMap()
        {
            string sOut = "";
            sOut = sOut + MimeMapOneExt("aac");
            sOut = sOut + MimeMapOneExt("abw");
            sOut = sOut + MimeMapOneExt("arc");
            sOut = sOut + MimeMapOneExt("avi");
            sOut = sOut + MimeMapOneExt("azw");
            sOut = sOut + MimeMapOneExt("bin");
            sOut = sOut + MimeMapOneExt("bmp");
            sOut = sOut + MimeMapOneExt("bz");
            sOut = sOut + MimeMapOneExt("bz2");
            sOut = sOut + MimeMapOneExt("csh");
            sOut = sOut + MimeMapOneExt("css");
            sOut = sOut + MimeMapOneExt("csv");
            sOut = sOut + MimeMapOneExt("doc");
            sOut = sOut + MimeMapOneExt("docx");
            sOut = sOut + MimeMapOneExt("eot");
            sOut = sOut + MimeMapOneExt("epub");
            sOut = sOut + MimeMapOneExt("gz");
            sOut = sOut + MimeMapOneExt("gif");
            sOut = sOut + MimeMapOneExt("htm");
            sOut = sOut + MimeMapOneExt("html");
            sOut = sOut + MimeMapOneExt("ico");
            sOut = sOut + MimeMapOneExt("ics");
            sOut = sOut + MimeMapOneExt("jar");
            sOut = sOut + MimeMapOneExt("jpeg");
            sOut = sOut + MimeMapOneExt("jpg");
            sOut = sOut + MimeMapOneExt("js");
            sOut = sOut + MimeMapOneExt("json");
            sOut = sOut + MimeMapOneExt("jsonld");
            sOut = sOut + MimeMapOneExt("mid");
            sOut = sOut + MimeMapOneExt("midi");
            sOut = sOut + MimeMapOneExt("mjs");
            sOut = sOut + MimeMapOneExt("mp3");
            sOut = sOut + MimeMapOneExt("mpeg");
            sOut = sOut + MimeMapOneExt("mpkg");
            sOut = sOut + MimeMapOneExt("odp");
            sOut = sOut + MimeMapOneExt("ods");
            sOut = sOut + MimeMapOneExt("odt");
            sOut = sOut + MimeMapOneExt("oga");
            sOut = sOut + MimeMapOneExt("ogv");
            sOut = sOut + MimeMapOneExt("ogx");
            sOut = sOut + MimeMapOneExt("opus");
            sOut = sOut + MimeMapOneExt("otf");
            sOut = sOut + MimeMapOneExt("png");
            sOut = sOut + MimeMapOneExt("pdf");
            sOut = sOut + MimeMapOneExt("php");
            sOut = sOut + MimeMapOneExt("ppt");
            sOut = sOut + MimeMapOneExt("pptx");
            sOut = sOut + MimeMapOneExt("rar");
            sOut = sOut + MimeMapOneExt("rtf");
            sOut = sOut + MimeMapOneExt("sh");
            sOut = sOut + MimeMapOneExt("svg");
            sOut = sOut + MimeMapOneExt("swf");
            sOut = sOut + MimeMapOneExt("tar");
            sOut = sOut + MimeMapOneExt("tif");
            sOut = sOut + MimeMapOneExt("tiff");
            sOut = sOut + MimeMapOneExt("ts");
            sOut = sOut + MimeMapOneExt("ttf");
            sOut = sOut + MimeMapOneExt("txt");
            sOut = sOut + MimeMapOneExt("vsd");
            sOut = sOut + MimeMapOneExt("wav");
            sOut = sOut + MimeMapOneExt("weba");
            sOut = sOut + MimeMapOneExt("webm");
            sOut = sOut + MimeMapOneExt("webp");
            sOut = sOut + MimeMapOneExt("woff");
            sOut = sOut + MimeMapOneExt("woff2");
            sOut = sOut + MimeMapOneExt("xhtml");
            sOut = sOut + MimeMapOneExt("xls");
            sOut = sOut + MimeMapOneExt("xlsx");
            sOut = sOut + MimeMapOneExt("xml");
            sOut = sOut + MimeMapOneExt("xul");
            sOut = sOut + MimeMapOneExt("zip");
            sOut = sOut + MimeMapOneExt("3gp");
            sOut = sOut + MimeMapOneExt("3g2");
            sOut = sOut + MimeMapOneExt("7z");

        }

        private void uiRUnMP_Click(object sender, RoutedEventArgs e)
        {
            DumpMiMeMap();

            //GuzikiEnable(false);


            //for(int i=0; i<10;i++)
            //{
            //    Thread.Sleep(1000);
            //    uiMsgCnt.Text = i.ToString();
            //}


            //GuzikiEnable(true);
            //uiMsgCnt.Text = "koniec - MainPage";
        }

        private async void uiRUnApp_Click(object sender, RoutedEventArgs e)
        {
            GuzikiEnable(false);



            await App.ZrobTo(uiMsgCnt);

            GuzikiEnable(true);
            uiMsgCnt.Text = "koniec - App";
        }
    }
}
