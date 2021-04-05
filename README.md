# farebox-demo

An Android app that is used by Connexionz to emulate the interaction between MDT and Farebox.

This app consists of the code that emulates its interaction between AndroidDeviceOwner app and MDT app.

## Overall work flow

Three apps will be involved in this workflow :-

- Farebox app,
- MDT app and
- AndroidDeviceOwner app - This app is responsible for switching.

When the tablet starts, Farebox app is shown in Kiosk mode. Clicking on the "Switch" button on the Farebox app brings MDT app in the foreground and now MDT app runs in Kiosk mode. Clicking on "Switch" button on MDT switches the control back to Farebox app. All this switching is handled by AndroidDeviceOwner app.

> NOTE - The app in the repository is created using Xamarin Native. So, it uses C# code. The app uses native android APIs, so C# code can be directly replaced by Java or Kotlin code.

Following are the things that are required to be implemented in Farebox app for this workflow to work. The repository includes sample code for the same.

> NOTE - File name MainActivity in the following documentation denotes the first activity that shows up when the app is launched.

1. In _AndroidManifest.xml_, mark `MainActivity`'s launchMode as `singleInstance`, as following:

```xml
<activity
    android:name="com.companyname.tryfarebox.MainActivity"
    android:label="@string/app_name"
    android:launchMode="singleInstance">

</activity>
```

2. In _AndroidManifest.xml_, inside `<activity> </activity>` create an `intent-filter` that will mark the MainActivity the main launcher. The code snippet is as following:

```xml
<activity
    android:name="com.companyname.tryfarebox.MainActivity"
    android:label="@string/app_name"
    android:launchMode="singleInstance">
    <intent-filter>
        <action android:name="android.intent.action.MAIN" />
        <category android:name="android.intent.category.HOME" />
        <category android:name="android.intent.category.LAUNCHER" />
        <category android:name="android.intent.category.DEFAULT" />
    </intent-filter>
</activity>
```

3. Inside `MainActivity` (which inherits AppCompatActivity), inside `OnResume()`, write following code. This piece of code starts the app in Kiosk (screen pinning) mode.

```cs
DevicePolicyManager devicePolicyManager = (DevicePolicyManager)GetSystemService(DevicePolicyService);

try
{
    // IsLockPermitted will be true if AndroidDeviceOwner has set this app for screen pinning
    //   using "SetLockTaskPackages()" command.
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
```

4. On click of `Switch` button, execute the following code. `cnx.AndroidDeviceOwner.CHANGE_LAUNCHER_APP` is the name of the broadcast intent that is listened by `AndroidDeviceOwner` app. And `com.connexionz.mdt.droid` is the package name of MDT app.

```cs
private void SwitchToMDTButton_Click(object sender, EventArgs e)
{
    //Sending explicit broadcast, means this will received only by the apps that are listening to
    // this intent, i.e. AndroidDeviceOwner in this case.
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

    //Ends kiosk (screen pinning) mode started by StartLockTask().
    if (dpm.IsLockTaskPermitted(PackageName))
    {
        this.StopLockTask();
    }

    FinishAndRemoveTask();
}
```

5. To Exit the Kiosk (screen pinning) mode, execute the following code. This code will exit the app, and show the main launcher chooser window.

```cs
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
    var infoList = PackageManager
                    .QueryIntentActivities(defaultLauncherIntent, PackageInfoFlags.MatchDefaultOnly)
                    .ToList();

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
```

> NOTE : The above piece of code must finish all the activities that are in the stack.
