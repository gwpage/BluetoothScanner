using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;

using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;

using Xamarin.Forms;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BluetoothScanner
{
    public class App : Application
    {
        public class DeviceDetails
        {
            public string Name { get; set; }
            public string Gid { get; set; }
        }

        public class ViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            bool _isBusy;
            public bool isBusy
            {
                get
                {
                    return _isBusy;
                }
                set
                {
                    if(_isBusy != value)
                    {
                        _isBusy = value;
                        PropertyChanged(this, new PropertyChangedEventArgs("isBusy"));
                    }
                }
            }

            ObservableCollection<DeviceDetails> _details = new ObservableCollection<DeviceDetails>();
            public ObservableCollection<DeviceDetails> details
            {
                get
                {
                    return _details;
                }
                set
                {
                    _details = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("details"));
                }
            }

            Command _scan;
            public Command Scan
            {
                get
                {
                    if (_scan == null)
                    {
                        _scan = new Command(async () => await executeScan());
                    }
                    return _scan;
                }
            }

            async Task executeScan()
            {
                if(isBusy)
                {
                    return;
                }

                isBusy = true;
                await ScanForDevices();
                var adapter = CrossBluetoothLE.Current.Adapter;
                var list = deviceList;
                details.Clear();
                foreach (var l in list.Where(x => !string.IsNullOrWhiteSpace(x.Name)).OrderBy(x => x.Name))
                {
                    var d = new DeviceDetails() { Name = l.Name, Gid = l.Id.ToString() };
                    Console.WriteLine($"{d.Name} - {d.Gid}");
                    details.Add(d);
                }
                isBusy = false;
            }

            List<IDevice> deviceList;
            public async Task ScanForDevices()
            {
                var adapter = CrossBluetoothLE.Current.Adapter;
                deviceList = new List<IDevice>();

                adapter.DeviceDiscovered += (s, a) =>
                {
                    deviceList.Add(a.Device);
                };

                await adapter.StartScanningForDevicesAsync();

            }
        }

        //Label scanResult;
        //CardSwiper Swiper;

        ViewModel vm = new ViewModel();
        

        public App()
        {
            BindingContext = vm;


            ListView lv = new ListView()
            {
                ItemsSource = vm.details,
                IsPullToRefreshEnabled = true,
                RefreshCommand = vm.Scan
            };
            lv.SetBinding(ListView.IsRefreshingProperty, "isBusy", BindingMode.OneWay);

            DataTemplate dt = new DataTemplate(typeof(TextCell));
            dt.SetBinding(TextCell.TextProperty, "Name");
            dt.SetBinding(TextCell.DetailProperty, "Gid");

            lv.ItemTemplate = dt;

            // wiley's iphone x -> B4: {D5D3452B-4702-D526-FCF4-A3265170F8A5}
            //wiley's iphone x -> CD: {2F95239A-F068-3E00-4213-86DD5F5388FF}
            // nutrition inventory ipad 1 -> B41EEB4: {FD6CCF08-1F3A-FA77-715E-99A5FF144093}
            // nutrition inventory ipad 2 -> B414ACD: {E8C80EDC-4E87-9764-672E-DA933D87091E}

            /*
            Swiper = new CardSwiper("{2F95239A-F068-3E00-4213-86DD5F5388FF}", (cardNumber) =>
            {
                scanResult.Text = cardNumber;
            });*/

            // The root page of your application
            var content = new ContentPage
            {
                Title = "BluetoothScanner",
                Content = new StackLayout
                {
                    //VerticalOptions = LayoutOptions.Center,
                    Children = {
                        //new Button{
                            //Text="Scan!",
                            //Command = vm.Scan //new Command(async () => {
                                /*
                            await ScanForDevices();
                                var adapter = CrossBluetoothLE.Current.Adapter;
                                var list = deviceList;
                                vm.details.Clear();
                                foreach(var l in list)
                                {
                                    var d = new DeviceDetails(){Name = l.Name, Gid = l.Id.ToString()};
                                    Console.WriteLine($"{d.Name} - {d.Gid}");
                                    vm.details.Add(d);
                                }
                                */
                                //lv.ItemsSource = details;
                            //})
                        //},
                       // new Button{
                        //    Text="Connect!",
                         //   Command = new Command(async () => await Swiper.Connect())
                        //},
                        lv

                        //scanResult
                    }
                }
            };

            content.ToolbarItems.Add(new ToolbarItem("Scan", null, () => lv.BeginRefresh()));

            MainPage = new NavigationPage(content);

        }

        List<IDevice> deviceList;
        public async Task ScanForDevices()
        {
            var adapter = CrossBluetoothLE.Current.Adapter;
            deviceList = new List<IDevice>();

            adapter.DeviceDiscovered += (s, a) =>
            {
                deviceList.Add(a.Device);
            };

            await adapter.StartScanningForDevicesAsync();

        }
        public async Task QuickConnect()
        {
            try
            {
                var adapter = CrossBluetoothLE.Current.Adapter;
                var device = await adapter.ConnectToKnownDeviceAsync(Guid.Parse("{D5D3452B-4702-D526-FCF4-A3265170F8A5}"));
                var services = await device.GetServicesAsync();
                var mainService = await device.GetServiceAsync(Guid.Parse("{0508e6f8-ad82-898f-f843-e3410cb60103}"));
                var characteristics = await mainService.GetCharacteristicsAsync();
                var readNotifyCharacteristic = await mainService.GetCharacteristicAsync(Guid.Parse("{0508e6f8-ad82-898f-f843-e3410cb60201}"));
                var descriptors = await readNotifyCharacteristic.GetDescriptorsAsync();


                List<byte> rawData = new List<byte>();



                readNotifyCharacteristic.ValueUpdated += async (o, args) =>
                {
                    //args.Characteristic.
                    var bytes = args.Characteristic.Value;


                    for (int i = 1; i < bytes.Length; i++)
                    {
                        rawData.Add(bytes[i]);
                    }

                    if (bytes[0] == 0xff)
                    {
                        var uncompressedRawData = CompressionUtilities.DeCompressBuffer(rawData.ToArray());

                        var maskedCardNumber = uncompressedRawData.Skip(624).Take(112).ToArray();
                        var decodedMaskedCardNum = System.Text.Encoding.Default.GetString(maskedCardNumber);

                        var encryptedCardNumber = uncompressedRawData.Skip(123).Take(112).ToArray();
                        var DUKPTKeySerialNumber = uncompressedRawData.Skip(499).Take(10).ToArray();
                        var BaseDerivationKey = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef, 0xfe, 0xdc, 0xba, 0x98, 0x76, 0x54, 0x32, 0x10 };
                        /*
                        var DUKPTKey = 

                        for (int i = 6; i >= 0; i--)
                        {
                            var block = 
                        }


                        var DecryptedCardNumberBits = EncryptedCardNumberBits.Xor(DUKPTKey);

                        for (int i = 6; i >= 0; i++)
                        {
                            DecryptedCardNumberBits = 
                        }

                        var DecryptedCardNumber = System.Text.Encoding.Default.GetString(DecryptedCardNumberBits);

*/
                        StringBuilder sb = new StringBuilder();

                        foreach (var b in uncompressedRawData)
                        {
                            sb.AppendFormat("{0:x2}",b);
                        }

                        Console.WriteLine(sb);
                    }

                };
                await readNotifyCharacteristic.StartUpdatesAsync();

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private byte[] exclusiveOr(byte[] a1, byte[] a2)
        {
            byte[] result = new byte[a1.Length];

            for (int i = 0; i < a1.Length; i++)
            {
                result[i] = (byte)(a1[i] ^ a2[i]);
            }

            return result;
        }

        protected override async void OnStart()
        {
            //await Swiper.Connect();

            // Handle when your app starts
            /*
            try
            {
                var adapter = CrossBluetoothLE.Current.Adapter;
                //device 1: 7b92c687-b80f-759d-63dc-04e648854085
                //device 2: BA5B0C70-C3A4-2823-5A51-F3FDB8ED344C
                var device = await adapter.ConnectToKnownDeviceAsync(Guid.Parse("{BA5B0C70-C3A4-2823-5A51-F3FDB8ED344C}"));
                var services = await device.GetServicesAsync();
                var mainService = await device.GetServiceAsync(Guid.Parse("{0508e6f8-ad82-898f-f843-e3410cb60103}"));
                var characteristics = await mainService.GetCharacteristicsAsync();
                var readNotifyCharacteristic = await mainService.GetCharacteristicAsync(Guid.Parse("{0508e6f8-ad82-898f-f843-e3410cb60201}"));

                var sb = new StringBuilder();
                var regex = new Regex(@"\d{9}");
                readNotifyCharacteristic.ValueUpdated += async (o, args) =>
                {

                    var bytes = args.Characteristic.Value;
                    var bytesAsString = System.Text.Encoding.Default.GetString(bytes);
                    var bytesAsCleanString = Regex.Replace(bytesAsString, @"[^\u0009\u000A\u000D\u0020-\u007E]", string.Empty);
                    if (!string.IsNullOrEmpty(bytesAsCleanString))
                    {
                        sb.Append(bytesAsCleanString);
                    }
                    var match = regex.Match(sb.ToString());

                    if (match.Success)
                    {
                        await readNotifyCharacteristic.StopUpdatesAsync();
                        scanResult.Text = match.Value;
                        sb.Clear();
                        await readNotifyCharacteristic.StartUpdatesAsync();
                    }
                };
                await readNotifyCharacteristic.StartUpdatesAsync();


                int i = 0;
            }
            catch (Exception ex)
            {
                ;
            }
            */
        }

        protected override async void OnSleep()
        {
            //await Swiper.Disconnect();
            // Handle when your app sleeps
        }

        protected override async void OnResume()
        {
             //Handle when your app resumes
            //await Swiper.Connect();
        }
    }
}
