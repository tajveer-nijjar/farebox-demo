using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TryFarebox
{
    [BroadcastReceiver(Name = "com.companyname.tryfarebox.DriverLoginSuccessfulBroadcastReceiver")]
    public class DriverLoginSuccessfulBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            var data = intent.GetStringExtra("data");
            if(data != null)
            {
                Toast.MakeText(context, $"Received: {data}", ToastLength.Long).Show();
            }
        }
    }
}