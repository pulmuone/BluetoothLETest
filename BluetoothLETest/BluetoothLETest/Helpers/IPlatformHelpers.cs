using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace BluetoothLETest.Helpers
{
    public interface IPlatformHelpers
    {
        Task<PermissionStatus> CheckAndRequestBluetoothPermissions();
    }
}
