using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

// https://android-developers.googleblog.com/2013/10/getting-your-sms-apps-ready-for-kitkat.html

namespace BackupSMS
{
    [BroadcastReceiver(Permission = Android.Manifest.Permission.BroadcastSms)]  // Name = "SmsReceiver", 
    [IntentFilter(new[] { Android.Provider.Telephony.Sms.Intents.SmsDeliverAction })]
    public class SmsReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Toast.MakeText(context, "SMS received (and lost!)", ToastLength.Short).Show();
        }
    }

    [BroadcastReceiver(Permission = Android.Manifest.Permission.BroadcastWapPush)]  // Name = "MmsReceiver", 
    [IntentFilter(new[] { Android.Provider.Telephony.Sms.Intents.WapPushDeliverAction }, DataMimeType = "application/vnd.wap.mms-message")]
    public class MmsReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Toast.MakeText(context, "MMS received (and lost!)", ToastLength.Short).Show();
        }
    }


    [Activity(Label = "SMS Activity Placeholder")]    // , Name = "ComposeSmsActivity"
    [IntentFilter(new[] { Android.Content.Intent.ActionSend, Android.Content.Intent.ActionSendto }, 
        Categories = new [] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable }, 
        DataSchemes = new [] {"sms", "smsto", "mms", "mmsto"})]
    public class ActivityDlaSMS : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Create your application here
        }
    }

    [Service( Exported =true,  // Name = "HeadlessSmsSendService"
        Permission = Android.Manifest.Permission.SendRespondViaMessage
        )]
    [IntentFilter(new[] { "android.intent.action.RESPOND_VIA_MESSAGE" },
        Categories = new [] {Android.Content.Intent.CategoryDefault},
        DataSchemes = new[] { "sms", "smsto", "mms", "mmsto" })]
    public class ServiceDlaSMS : Service
    {
        public override IBinder OnBind(Intent intent)
        {
            return null;
        }
    }
}