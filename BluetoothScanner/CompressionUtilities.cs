using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace BluetoothScanner
{
    public static class CompressionUtilities
    {
        public static byte[] DeCompressBuffer(byte[] byteArray)
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
                    if(current == previous && i < byteArray.Length)
                    {
                        var next = byteArray[i + 1];

                        int nextCount = Convert.ToInt32(next);

                        for (int c = 2; c < nextCount; c++)
                        {
                            unCompressed.Add(current);
                        }
                        i += 2;
                        if(i <= byteArray.Length)
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
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
