using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Android.Content;
using Android.Widget;
using Android;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;
using Android.App.Admin;
using Android.Content.PM;
using System.Linq;

namespace TryFarebox
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true, Name = "com.companyname.tryfarebox.MainActivity")]
    public class MainActivity : AppCompatActivity
    {
        private DevicePolicyManager _devicePolicyManager;
        private Button _exitAppButton, _gotoMDTButton;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            _exitAppButton = FindViewById<Button>(Resource.Id.exitAppButton);
            _gotoMDTButton = FindViewById<Button>(Resource.Id.gotoMDTButton);

            _exitAppButton.Click += ExitAppButton_Click;
            _gotoMDTButton.Click += GotoMDTButton_Click;


        }

        protected override void OnResume()
        {
            base.OnResume();

            _devicePolicyManager = (DevicePolicyManager)GetSystemService(DevicePolicyService);
        
            try
            {
                // IsLockPermitted will be true if AndroidDeviceOwner has set this app for screen pinning using "SetLockTaskPackages()" command.
                // Checks if this app is allowed to start in Kiosk mode.
                if (_devicePolicyManager.IsLockTaskPermitted(PackageName))
                {
                    //Starts the kiosk (screen pinning) mode.
                    StartLockTask();
                }
                else
                {
                    Toast.MakeText(this, "Kiosk Mode not permitted", ToastLength.Long).Show();
                }
            }
            catch (Exception e)
            {
                Toast.MakeText(this, $"Error {e.Message}", ToastLength.Long).Show();
            }
        }

        private void ExitAppButton_Click(object sender, EventArgs e)
        {
            Finish();
            var dpm = (DevicePolicyManager)GetSystemService(DevicePolicyService);

            if (dpm.IsLockTaskPermitted(PackageName))
            {
                //Ends kiosk (screen pinning) mode started by StartLockTask().
                this.StopLockTask();
            }

            //Showing the launcher activity (launcher page).
            var defaultLauncherIntent = new Intent(Intent.ActionMain);
            defaultLauncherIntent.AddCategory(Intent.CategoryHome);
            var infoList = PackageManager.QueryIntentActivities(defaultLauncherIntent, PackageInfoFlags.MatchDefaultOnly).ToList();
            foreach (var info in infoList)
            {
                if (!PackageName.Equals(info.ActivityInfo.PackageName))
                {
                    // This is the first match that isn't my package, so copy the
                    //  package and class names into to the HOME Intent
                    defaultLauncherIntent.SetClassName(info.ActivityInfo.PackageName, info.ActivityInfo.Name);
                    break;
                }
            }
            StartActivity(defaultLauncherIntent);
            
            //This Finish() must finish the main loading activity.
            this.Finish();
        }

        private void GotoMDTButton_Click(object sender, EventArgs e)
        {
            //Sending explicit broadcast, means this will received only by the apps that are listening to this intent, i.e. AndroidDeviceOwner in this case.
            var intent = new Intent("cnx.AndroidDeviceOwner.CHANGE_LAUNCHER_APP");

            //Getting list of packages (apps) that are listening for above mentioned broadcast.
            var infos = PackageManager.QueryBroadcastReceivers(intent, 0).ToList(); 

            //Sending explicit broadcast to all the apps that are listening to the above mentioned intent. 
            //In this case it will be AndroidDeviceOwner app only.
            foreach (var info in infos)
            {
                var componentName = new ComponentName(info.ActivityInfo.PackageName, info.ActivityInfo.Name);
                intent.SetComponent(componentName);
                intent.PutExtra("package_name", "com.connexionz.mdt.droid");
                SendBroadcast(intent);
            }

            var dpm = (DevicePolicyManager)GetSystemService(DevicePolicyService);

            if (dpm.IsLockTaskPermitted(PackageName))
            {
                //Ends kiosk (screen pinning) mode started by StartLockTask().
                this.StopLockTask();
            }

            FinishAndRemoveTask();
        }
    }
}
