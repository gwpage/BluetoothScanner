using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace BluetoothScanner
{
    public class CardSwiper
    {
        private Guid GUID { get; set; }
        private IDevice Device { get; set; }
        private IService Service { get; set; }
        private ICharacteristic Characteristic { get; set; }
        private Action<string> SwipeResultHandler { get; set; }
        private List<byte> RawData { get; set; }


        /// <summary>
        /// Initializes an instance of the card swiper. Should be called in constructor of any view
        /// that wants to listen for swipes.
        /// </summary>
        /// <param name="GUID">The GUID of the device to connect to.</param>
        /// <param name="SwipeResultHandler">
        /// A lambda expression that consumes a string, which will be a 9 digit number,
        ///  which is the end result of the swipe process. Must be able to
        /// handle '000000000', which indicates an error has occurred. 
        /// </param>
        public CardSwiper(string GUID, Action<string> SwipeResultHandler)
        {
            this.GUID = Guid.Parse(GUID);
            this.SwipeResultHandler = SwipeResultHandler;
        }

        /// <summary>
        /// Call this in the OnAppearing function of your view.
        /// </summary>
        /// <returns></returns>
        public async Task Connect()
        {
            Device = await CrossBluetoothLE.Current.Adapter.ConnectToKnownDeviceAsync(GUID);
            Service = await Device.GetServiceAsync(Guid.Parse("{0508e6f8-ad82-898f-f843-e3410cb60103}"));
            Characteristic = await Service.GetCharacteristicAsync(Guid.Parse("{0508e6f8-ad82-898f-f843-e3410cb60201}"));

            RawData = new List<byte>();
            Characteristic.ValueUpdated += Listener;
            await Characteristic.StartUpdatesAsync();
        }

        /// <summary>
        /// Call this in the OnDisappearing function of your view.
        /// </summary>
        /// <returns></returns>
        public async Task Disconnect()
        {
            await Characteristic.StopUpdatesAsync();
            Characteristic.ValueUpdated -= Listener;
            Service.Dispose();
            Device.Dispose();
        }

        private void Listener (object sender, CharacteristicUpdatedEventArgs args)
        {
            try
            {
                var bytes = args.Characteristic.Value;


                for (int i = 1; i < bytes.Length; i++)
                {
                    RawData.Add(bytes[i]);
                }

                if (bytes[0] == 0xff)
                {
                    var uncompressedRawData = DeCompressBuffer(RawData.ToArray());
                    var cardNumberAsBytes = uncompressedRawData.Skip(624).Take(112).ToArray();
                    var cardNumberAsString = System.Text.Encoding.Default.GetString(cardNumberAsBytes);
                    if(cardNumberAsString.Length >= 9)
                    {
                        cardNumberAsString = cardNumberAsString.Substring(0, 9);
                    }
                    else
                    {
                        throw new Exception("Card number is not of sufficient length");
                    }
                    if(!Int32.TryParse(cardNumberAsString, out int dummy))
                    {
                        throw new Exception("Card number is not a number");
                    }
                    RawData = new List<byte>();
                    SwipeResultHandler.Invoke(cardNumberAsString);
                }
            }
            catch
            {
                RawData = new List<byte>();
                SwipeResultHandler.Invoke("000000000");
            }
        }

        private byte[] DeCompressBuffer(byte[] byteArray)
        {
            try
            {
                List<byte> unCompressed = new List<byte>();

                unCompressed.Add(byteArray[0]);
                byte previous = byteArray[0];

                for (int i = 1; i < byteArray.Length; i++)
                {
                    var current = byteArray[i];
                    unCompressed.Add(current);
                    if (current == previous && i < byteArray.Length)
                    {
                        var next = byteArray[i + 1];

                        int nextCount = Convert.ToInt32(next);

                        for (int c = 2; c < nextCount; c++)
                        {
                            unCompressed.Add(current);
                        }
                        i += 2;
                        if (i <= byteArray.Length)
                        {
                            current = byteArray[i];
                            unCompressed.Add(current);
                            previous = current;
                        }
                    }
                    else
                    {
                        previous = current;
                    }
                }

                return unCompressed.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
