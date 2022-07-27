﻿using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Acr.UserDialogs;
using Android;
using AndroidX.Core.App;

namespace BluetoothLETest.Droid
{
    [Activity(Label = "BluetoothLETest", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        static string[] PERMISSIONS = {
            Manifest.Permission.AccessFineLocation
        };
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            UserDialogs.Init(this);
            LoadApplication(new App());

            //if (Build.VERSION.SdkInt >= BuildVersionCodes.M) //23이상부터
            //{
            //    ActivityCompat.RequestPermissions(this, PERMISSIONS, 0);
            //}
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}