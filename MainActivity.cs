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
                //Clears all the home button preferences
                //PackageManager.ClearPackagePreferredActivities(PackageName);

                // IsLockPermitted will be true if it is set in TryAndroidDeviceOwner app using "SetLockTaskPackages()" command.
                // SetLockTaskPackages() is in DeviceOwner app.
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

            //Clears all the home button preferences.
            //Here removing the MDT from being the launcher app.
            //This will show a menu to ask for selecting a new launcher app.
            //PackageManager.ClearPackagePreferredActivities(PackageName);

            //Showing the launcher activity (launcher page).
            var homeIntent = new Intent(Intent.ActionMain);
            homeIntent.AddCategory(Intent.CategoryHome);
            var infoList = PackageManager.QueryIntentActivities(homeIntent, PackageInfoFlags.MatchDefaultOnly).ToList();
            foreach (var info in infoList)
            {
                if (!PackageName.Equals(info.ActivityInfo.PackageName))
                {
                    // This is the first match that isn't my package, so copy the
                    //  package and class names into to the HOME Intent
                    homeIntent.SetClassName(info.ActivityInfo.PackageName, info.ActivityInfo.Name);
                    break;
                }
            }
            StartActivity(homeIntent);
            Finish();
        }

        private void GotoMDTButton_Click(object sender, EventArgs e)
        {
            //Sending explicit intents, means this will received only by the apps that are listening to this.
            var intent = new Intent("cnx.AndroidDeviceOwner.CHANGE_LAUNCHER_APP");
            var infos = PackageManager.QueryBroadcastReceivers(intent, 0).ToList(); //Getting list of packages (apps) that are listening for above mentioned broadcast.

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
            //PackageManager.ClearPackagePreferredActivities(PackageName);

            FinishAndRemoveTask();
        }
    }
}
