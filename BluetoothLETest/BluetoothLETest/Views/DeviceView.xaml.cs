using Acr.UserDialogs;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using Plugin.BLE.Abstractions.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

/// <summary>
/// Status -> Scan -> Connect -> Get List Of Services -> Update
/// </summary>
namespace BluetoothLETest.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DeviceView : ContentPage
    {
        private readonly IBluetoothLE _bluetoothLe;
        IAdapter _adapter;
        ObservableCollection<IDevice> deviceList;
        IDevice _device;
        private Guid _previousGuid;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly IUserDialogs _userDialogs;

        IList<IService> Services;
        IService Service;
        IService ServiceWrite;

        //IList<ICharacteristic> Characteristics;
        ICharacteristic Characteristic;
        ICharacteristic CharacteristicWrite;

        public DeviceView()
        {
            InitializeComponent();
            _bluetoothLe = CrossBluetoothLE.Current;
            _adapter = CrossBluetoothLE.Current.Adapter;

            deviceList = new ObservableCollection<IDevice>();
            lv.ItemsSource = deviceList;

            _bluetoothLe.StateChanged += Ble_StateChanged;
            _adapter.DeviceAdvertised += Adapter_DeviceAdvertised;
            _adapter.DeviceDiscovered += Adapter_DeviceDiscovered;
            _adapter.DeviceConnected += Adapter_DeviceConnected;
            _adapter.DeviceDisconnected += Adapter_DeviceDisconnected;
            _adapter.DeviceConnectionLost += Adapter_DeviceConnectionLost;
            _adapter.ScanTimeoutElapsed += Adapter_ScanTimeoutElapsed;
            GetPreviousGuidAsync();

            Console.WriteLine(_adapter.ScanTimeout);

            _userDialogs = UserDialogs.Instance;

            _cancellationTokenSource = new CancellationTokenSource();
        }

        private void Adapter_ScanTimeoutElapsed(object sender, EventArgs e)
        {
            Console.WriteLine("==> Adapter_ScanTimeoutElapsed :");
        }

        private void Ble_StateChanged(object sender, Plugin.BLE.Abstractions.EventArgs.BluetoothStateChangedArgs e)
        {
            Console.WriteLine("==> Ble_StateChanged : " + e.NewState.ToString());
        }

        private void Adapter_DeviceConnectionLost(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceErrorEventArgs e)
        {
            
            Console.WriteLine("==> Adapter_DeviceConnectionLost : " + e.Device);
        }

        private async void Adapter_DeviceDisconnected(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            Console.WriteLine("==> Adapter_DeviceDisconnected : " + e.Device);
            Console.WriteLine(this._device.State);
            await _adapter.DisconnectDeviceAsync(this._device);

            if (this._device.State == DeviceState.Connected)
            {
                Console.WriteLine(">> Disconnected");
                await _adapter.DisconnectDeviceAsync(this._device);
            }
        }

        private void Adapter_DeviceConnected(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            Console.WriteLine("==> Adapter_DeviceConnected : " + e.Device);
        }

        private void Adapter_DeviceAdvertised(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            Console.WriteLine("==> Adapter_DeviceAdvertised : " + e.Device);
        }

        private void Adapter_DeviceDiscovered(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            var msg = string.Format("DeviceFound {0}", e.Device.Name);
            var devicesss = msg;

            var vm = deviceList.FirstOrDefault(d => d.Id == e.Device.Id);
            if (vm != null)
            {
                //vm.Update();
            }
            else
            {
                deviceList.Add(e.Device);

                Console.WriteLine(e.Device.Rssi);
            }
        }

        private Task GetPreviousGuidAsync()
        {

            //var pairedOrConnectedDeviceWithNullGatt = _adapter.GetSystemConnectedOrPairedDevices();
            Console.WriteLine();

            return Task.Run(() =>
            {
                var guidString = Preferences.Get("lastguid", string.Empty);
                PreviousGuid = !string.IsNullOrEmpty(guidString) ? Guid.Parse(guidString) : Guid.Empty;
            });
        }

        //private void _bleAdapterDeviceDiscovered(object sender, DeviceEventArgs e)
        //{
        //    var msg = string.Format("DeviceFound {0}", e.Device.Name);
        //    var devicesss = msg;
        //}

        /// <summary>
        /// Define the status
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStatus_Clicked(object sender, EventArgs e)
        {
            var state = _bluetoothLe.State;

            DisplayAlert("Notice", state.ToString(), "OK !");
            if (state == BluetoothState.Off)
            {
                txtErrorBle.BackgroundColor = Color.Red;
                txtErrorBle.TextColor = Color.White;
                txtErrorBle.Text = "Your Bluetooth is off ! Turn it on !";
            }

            var result = GetStateText();
            Console.WriteLine(result);
        }

        private string GetStateText()
        {
            switch (_bluetoothLe.State)
            {
                case BluetoothState.Unknown:
                    return "Unknown BLE state.";
                case BluetoothState.Unavailable:
                    return "BLE is not available on this device.";
                case BluetoothState.Unauthorized:
                    return "You are not allowed to use BLE.";
                case BluetoothState.TurningOn:
                    return "BLE is warming up, please wait.";
                case BluetoothState.On:
                    return "BLE is on.";
                case BluetoothState.TurningOff:
                    return "BLE is turning off. That's sad!";
                case BluetoothState.Off:
                    return "BLE is off. Turn it on!";
                default:
                    return "Unknown BLE state.";
            }
        }

        private async Task<bool> HasCorrectPermissions()
        {
            var permissionResult = await DependencyService.Get<Helpers.IPlatformHelpers>().CheckAndRequestBluetoothPermissions();
            if (permissionResult != PermissionStatus.Granted)
            {
                await DisplayAlert("Permission", "Permission denied. Not scanning.", "OK");
                AppInfo.ShowSettingsUI();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Scan the list of Devices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnScan_Clicked(object sender, EventArgs e)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            try
            {
                if (!await HasCorrectPermissions())
                {
                    return;
                }

                deviceList.Clear();
                //_adapter.DeviceDiscovered += (s, a) =>
                //{
                //    deviceList.Add(a.Device);
                //};

                //We have to test if the device is scanning 
                if (!_bluetoothLe.Adapter.IsScanning)
                {
                    _adapter.ScanMode = ScanMode.LowLatency;
                    await _adapter.StartScanningForDevicesAsync(_cancellationTokenSource.Token);
                    //await _adapter.StartScanningForDevicesAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Notice", ex.Message.ToString(), "Error !");
            }
        }

        /// <summary>
        /// Connect to a specific device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnConnect_Clicked(object sender, EventArgs e)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();

            try
            {
                btnStopScan_Clicked(sender, e);

                if (_device != null)
                {

                    var config = new ProgressDialogConfig()
                    {
                        Title = $"Connecting to '{_device.Id}'",
                        CancelText = "Cancel",
                        IsDeterministic = false,
                        OnCancel = tokenSource.Cancel
                    };

                    using (var progress = _userDialogs.Progress(config))
                    {
                        progress.Show();

                        bool UseAutoConnect = true;
                        await _adapter.ConnectToDeviceAsync(_device, new ConnectParameters(autoConnect: UseAutoConnect, forceBleTransport: true), tokenSource.Token);
                    }
                    await _userDialogs.AlertAsync($"Connected to {_device.Name}.");

                    PreviousGuid = _device.Id;
                }
                else
                {
                    await DisplayAlert("Notice", "No Device selected !", "OK");
                }
            }
            catch (DeviceConnectionException ex)
            {
                //Could not connect to the device
                await DisplayAlert("Notice", ex.Message.ToString(), "OK");
            }
            finally
            {
                tokenSource.Dispose();
                tokenSource = null;
            }
        }

        //블루투스 기계가 지정되어 있을 경우
        //tokenSource 토큰을 지정하지 않으면 계속 접속하다가 에러 발생함.
        private async void btnKnowConnect_Clicked(object sender, EventArgs e)
        {
            //IDevice device;
            CancellationTokenSource tokenSource = new CancellationTokenSource();

            try
            {
                var config = new ProgressDialogConfig()
                {
                    Title = $"Searching for '{PreviousGuid}'",
                    CancelText = "Cancel",
                    IsDeterministic = false,
                    OnCancel = tokenSource.Cancel
                };

                bool UseAutoConnect = true;

                using (var progress = _userDialogs.Progress(config))
                {
                    progress.Show();

                    _device = await _adapter.ConnectToKnownDeviceAsync(PreviousGuid, new ConnectParameters(autoConnect: UseAutoConnect, forceBleTransport: true), tokenSource.Token);
                }

                #region 직접 접속
                //if (_device != null)
                //{
                //    if (_device.State == DeviceState.Connected)
                //    {
                //        Characteristic.ValueUpdated -= Characteristic_ValueUpdated;
                //        Characteristic.Service.Dispose();
                //        Characteristic = null;

                //        if (Service != null)
                //        {
                //            Service.Dispose();
                //            Service = null;
                //        }

                //        await _adapter.DisconnectDeviceAsync(_device);
                //    }
                //}

                //_device = await _adapter.ConnectToKnownDeviceAsync(Guid.Parse("00000000-0000-0000-0000-e6cc245df604"), new ConnectParameters(autoConnect: UseAutoConnect, forceBleTransport: true));

                //if (_device == null) return;


                //Service = (IService)await _device.GetServiceAsync(Guid.Parse("6e400001-b5a3-f393-e0a9-e50e24dcca9e"));

                //if (Characteristic != null)
                //{
                //    await Characteristic.StopUpdatesAsync();
                //    Characteristic.ValueUpdated -= Characteristic_ValueUpdated;
                //    Characteristic.Service.Dispose();
                //    Characteristic = null;
                //}

                //Characteristic = await Service.GetCharacteristicAsync(Guid.Parse("6e400003-b5a3-f393-e0a9-e50e24dcca9e"));

                //Characteristic.ValueUpdated += Characteristic_ValueUpdated;

                //if (Characteristic != null)
                //{
                //    await Characteristic.StartUpdatesAsync();
                //}
                #endregion

                foreach (IService service in Services)
                {
                    foreach (ICharacteristic characteristic in await service.GetCharacteristicsAsync())
                    {
                        Console.WriteLine(characteristic.StringValue);
                        Console.WriteLine(characteristic.Value);
                        Console.WriteLine(characteristic.Uuid);
                        Console.WriteLine(characteristic.Name);
                        //if (characteristic.CanRead && characteristic.CanUpdate && characteristic.Properties == (CharacteristicPropertyType.Read | CharacteristicPropertyType.Notify | CharacteristicPropertyType.Indicate))
                        if (characteristic.CanUpdate && characteristic.Properties == CharacteristicPropertyType.Notify)
                        {
                            //기존 연결 초기화
                            if (Characteristic != null)
                            {
                                await Characteristic.StopUpdatesAsync();
                                Characteristic.ValueUpdated -= Characteristic_ValueUpdated;
                                Characteristic.Service.Dispose();
                                Characteristic = null;

                                Service.Dispose();
                                Service = null;
                            }

                            Service = service;
                            Characteristic = characteristic;

                            Console.WriteLine(characteristic.Uuid + ", " + characteristic.Id);

                            Characteristic.ValueUpdated += Characteristic_ValueUpdated;

                            if (Characteristic != null)
                            {
                                await Characteristic.StartUpdatesAsync();
                            }

                            break;
                        }
                    }
                }
            }
            catch (DeviceConnectionException ex)
            {
                _userDialogs.Toast(new ToastConfig(ex.Message) { BackgroundColor = Color.Red, Duration = TimeSpan.FromSeconds(3) });
            }
            catch (Exception ex)
            {
                _userDialogs.Toast(new ToastConfig(ex.Message) { BackgroundColor = Color.Red, Duration = TimeSpan.FromSeconds(3) });
            }
            finally
            {
                //이부분을 반듯이 추가해야 함.
                tokenSource.Dispose();
                tokenSource = null;
            }
        }

        /// <summary>
        /// Get list of services
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnGetServices_Clicked(object sender, EventArgs e)
        {
            if (_device != null)
            {
                Services = (IList<IService>)await _device.GetServicesAsync();

                foreach (IService service in Services)
                {
                    Console.WriteLine(service.Name);
                    foreach (ICharacteristic characteristic in await service.GetCharacteristicsAsync())
                    {
                        //if (characteristic.CanRead && characteristic.CanUpdate && characteristic.Properties == (CharacteristicPropertyType.Read | CharacteristicPropertyType.Notify | CharacteristicPropertyType.Indicate))
                        //if (characteristic.CanUpdate && characteristic.Properties == CharacteristicPropertyType.Notify)
                        if (characteristic.CanWrite && characteristic.Properties == (CharacteristicPropertyType.Write | CharacteristicPropertyType.WriteWithoutResponse))
                        {
                            //기존 연결 초기화
                            if (CharacteristicWrite != null)
                            {
                                //await Characteristic.StopUpdatesAsync();
                                CharacteristicWrite.ValueUpdated -= Characteristic_ValueUpdated;
                                CharacteristicWrite.Service.Dispose();
                                Characteristic = null;

                                //ServiceWrite.Dispose();
                                //ServiceWrite = null;
                            }

                            //Service = service; //1.서비스
                            CharacteristicWrite = characteristic; //2.서비스 > characteristic
                            Console.WriteLine(CharacteristicWrite.Uuid + ", " + CharacteristicWrite.Id);

                            //CharacteristicWrite.ValueUpdated += Characteristic_ValueUpdated; //안해도 된다.
                        }

                        if (characteristic.CanUpdate && characteristic.Properties == CharacteristicPropertyType.Notify)
                        {
                            //기존 연결 초기화
                            if (Characteristic != null)
                            {
                                //await Characteristic.StopUpdatesAsync();
                                Characteristic.ValueUpdated -= Characteristic_ValueUpdated;
                                Characteristic.Service.Dispose();
                                Characteristic = null;

                                if(Service != null)
                                {
                                    Service.Dispose();
                                    Service = null;
                                }
                            }

                            Service = service; //1.서비스
                            Characteristic = characteristic; //2.서비스 > characteristic
                            Console.WriteLine(characteristic.Uuid + ", " + characteristic.Id);

                            Characteristic.ValueUpdated += Characteristic_ValueUpdated;

                            if (Characteristic != null)
                            {
                                await Characteristic.StartUpdatesAsync();
                            }

                            //break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get Characteristics
        /// 사용하지 않음.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnGetcharacters_Clicked(object sender, EventArgs e)
        {
            var characteristics = await Service.GetCharacteristicsAsync();
            //Guid idGuid = Guid.Parse("guid");

            foreach (var item in characteristics)
            {
                if (item.CanUpdate)
                {
                    Characteristic = item;
                    break;
                }
            }

            //Characteristic = await Service.GetCharacteristicAsync(idGuid);
            //Characteristic.CanRead
        }

        IDescriptor descriptor;
        IList<IDescriptor> descriptors;

        private async void btnDescriptors_Clicked(object sender, EventArgs e)
        {
            descriptors = (IList<IDescriptor>)await Characteristic.GetDescriptorsAsync();
            foreach (var item in descriptors)
            {
                Console.WriteLine(item);
            }

            descriptor = await Characteristic.GetDescriptorAsync(Guid.Parse("guid"));
        }

        private async void btnDescRW_Clicked(object sender, EventArgs e)
        {
            var bytes = await descriptor.ReadAsync();
            await descriptor.WriteAsync(bytes);
        }

        private async void btnGetRW_Clicked(object sender, EventArgs e)
        {
            var bytes = await Characteristic.ReadAsync();
            await Characteristic.WriteAsync(bytes);
        }

        private async void btnUpdate_Clicked(object sender, EventArgs e)
        {
            if (Characteristic != null)
            {
                await Characteristic.StartUpdatesAsync(_cancellationTokenSource.Token);
            }
        }

        private void Characteristic_ValueUpdated(object sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
        {
            var bytes = e.Characteristic.Value;

            string tmp = GuidExtension.ToHexString(bytes);
            Console.WriteLine(tmp);

            if(tmp == "40") //깨우기
            {
                Console.WriteLine("깨우기");
            }
            else if(tmp.StartsWith("32-31"))
            {
                Console.WriteLine("Serial Number 가져오기");
            }
            else
            {
                Console.WriteLine("바코드 스캔하기");
            }
            //MainThread.BeginInvokeOnMainThread(() =>
            //{
            //    ScanResult.Text = tmp.ToString();
            //});

            string[] hexValuesSplit = tmp.Split('-');
            foreach (string hex in hexValuesSplit)
            {
                // Convert the number expressed in base-16 to an integer.
                int value = Convert.ToInt32(hex, 16);
                // Get the character corresponding to the integral value.
                string stringValue = Char.ConvertFromUtf32(value);
                char charValue = (char)value;
                Console.WriteLine("hexadecimal value = {0}, int value = {1}, char value = {2} or {3}", hex, value, stringValue, charValue);
            }

            //Console.WriteLine(String.Format("{0} {1}", "Result :", bytes));
            //var result1 = e.Characteristic.StringValue.ToString();
            //Console.WriteLine("result1:" + result1);
            //var result2 = e.Characteristic.StringValue.Replace("\r", "").ToString();
            //result2 = result2.Replace("\n", "").ToString();
            //Console.WriteLine("result2:" + result2);
            //Console.WriteLine(String.Format("{0} {1} {2} {3}", "Result1:", result1, "Result2:", result2));
            //Debug.WriteLine(String.Format("{0} {1} {2} {3}", "Result1:", result1, "Result2:", result2));
        }

        /// <summary>
        /// Select Items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lv_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (lv.SelectedItem == null)
            {
                return;
            }
            _device = lv.SelectedItem as IDevice;
        }

        private void txtErrorBle_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

        }

        private void btnStopScan_Clicked(object sender, EventArgs e)
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                Task.Delay(600);
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

        }

        public Guid PreviousGuid
        {
            get => _previousGuid;
            set
            {
                _previousGuid = value;
                Preferences.Set("lastguid", _previousGuid.ToString());
            }
        }

        private async void btnDisConnect_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (_device != null)
                {
                    if (_device.State != DeviceState.Connected)
                    {
                        return;
                    }

                    //_userDialogs.ShowLoading($"Disconnecting {device.Name}...");

                    Characteristic.ValueUpdated -= Characteristic_ValueUpdated;
                    Characteristic.Service.Dispose();
                    Characteristic = null;

                    Service.Dispose();
                    Service = null;

                    await _adapter.DisconnectDeviceAsync(_device);
                }
            }
            catch (Exception ex)
            {
                //await _userDialogs.AlertAsync(ex.Message, "Disconnect error");
            }
            finally
            {
                //_device.Update();
                //_userDialogs.HideLoading();
            }

        }

        private async void btnStartUpdate_Clicked(object sender, EventArgs e)
        {
            if (Characteristic != null)
            {
                await Characteristic.StartUpdatesAsync();
            }
        }

        private void btnStopUpdate_Clicked(object sender, EventArgs e)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(async() =>
                {
                    byte[] arr_byteStr = Encoding.Default.GetBytes("W"); //슬립 상태를 깨운다 @응답값 받음.

                    await CharacteristicWrite.WriteAsync(arr_byteStr);

                    //수신받은 Serial Number 세번째 자리로 스캐너를 구분하면 됩니다.
                    //1D - "j"
                    //2D - "k"
                    arr_byteStr = Encoding.Default.GetBytes("M");  //시리얼값

                    await CharacteristicWrite.WriteAsync(arr_byteStr);
                });
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //if (Characteristic != null)
            //{
            //    await Characteristic.StopUpdatesAsync();
            //}
        }
    }
}