using BluetoothLETest.Helpers;
using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: Dependency(typeof(BluetoothLETest.iOS.Helpers.PlatformHelpers))]
namespace BluetoothLETest.iOS.Helpers
{
    public class PlatformHelpers : IPlatformHelpers
    {
        public Task<PermissionStatus> CheckAndRequestBluetoothPermissions()
        {
            return Task.FromResult(PermissionStatus.Granted);

        }
    }
}