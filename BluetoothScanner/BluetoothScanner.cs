using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;


using Xamarin.Forms;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;


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

            // The root page of your application
            var content = new ContentPage
            {
                Title = "BluetoothScanner",
                Content = new StackLayout
                {
                    Children = {
                        lv
                    }
                }
            };

            content.ToolbarItems.Add(new ToolbarItem("Scan", null, () => lv.BeginRefresh()));

            MainPage = new NavigationPage(content);

        }

        protected override async void OnStart()
        {
        }

        protected override async void OnSleep()
        {
        }

        protected override async void OnResume()
        {
        }
    }
}
