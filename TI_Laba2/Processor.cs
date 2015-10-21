using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
//DES vs IDEA, все об алг IDEA, симметричные алг почему?

namespace TI_Laba2
{
    public enum Status
    {
        Coding,
        Decoding
    }


    public class Processor
    {
        public static CheckBox[] CbArr;
        public static UIElement[] UiEl;

        public static string EndingPath, Source;
        public static ushort[] Key = new ushort[54], InverseKey = new ushort[54];
        public static ushort[] File = new ushort[4];
        public const uint MultMod = 65537, SummMod = 65536;
        public static Status Status;

        private static void Deactivate()
        {
            foreach (var cb in UiEl)
                cb.IsEnabled = !cb.IsEnabled;
        }

        public static ushort[] PrepareKey(string keyPath)
        {
            var byteKey = new byte[16];
            var br = new BinaryReader(new FileStream(keyPath, FileMode.Open, FileAccess.Read));
            br.Read(byteKey, 0, 16);
            br.Close();
            BigInteger realKey = byteKey[0];
            for (var i = 1; i < 16; i++)
            {
                realKey <<= 8;
                realKey += byteKey[i];
            }

            var res = new ushort[56];
            var n = 0;
            while (n < 54)
            {
                var key = realKey;
                for (var i = 0; i < 8; i++)
                {
                    key = ROL(key, 128, 16);
                    var l = key & 65535;
                    res[n] = (ushort) l;
                    n++;
                }
                realKey = ROL(realKey, 128, 25);
            }
            Array.Resize(ref res, 52);
            return res;
        }

        private static BigInteger ROL(BigInteger source, int size, int count)
        {
            var o = 1;
            for (var i = 0; i < size - 1; i++)
            {
                o <<= 1;
                o++;
            }
            return (source << count) | (source >> (size - count) & o);
        }

        public static void Work()
        {
            Deactivate();
            InverseKey = Inverse(Key);

            var fs = new FileStream(Source, FileMode.Open, FileAccess.Read);
            fs.Close();
            {
                var bw = new BackgroundWorker();
                bw.DoWork += DoWork;
                bw.RunWorkerCompleted += WorkerCompleted;
                bw.RunWorkerAsync();
            }
        }

        private static void Gcdex(ushort a, uint b, out short x, out short y)
        {
            if (a == 0)
            {
                x = 0;
                y = 1;
                return;
            }
            short x1, y1;
            Gcdex((ushort) (b%a), a, out x1, out y1);
            x = (short) (y1 - (b/a)*x1);
            y = x1;
        }

        private static ushort MultiInverse(ushort a)
        {
            short x, y;
            Gcdex(a, MultMod, out x, out y);
            return (ushort) ((x%MultMod + MultMod)%MultMod);
        }

        private static void WorkerCompleted(object sender, RunWorkerCompletedEventArgs runWorkerCompletedEventArgs)
        {
            Deactivate();
            MessageBox.Show(Status == Status.Coding ? "Шифрование завершено!" : "Дешифрирование завершено!", "Внимание!",
                MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK,
                MessageBoxOptions.ServiceNotification);
        }

        private static void DoWork(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            var fs = new FileStream(Source, FileMode.Open, FileAccess.Read);
            var br = new BinaryReader(fs);
            var stream = new BinaryWriter(new FileStream(EndingPath, FileMode.Create, FileAccess.Write));
            
            var binaryFile = new byte[8];
            for (var k = 0; k < fs.Length/8; k++)
            {
                for (var i = 0; i < 8; i++)
                {
                    binaryFile[i] = br.ReadByte();
                    if (((i & 1) == 1) && i != 0)
                        File[(i - 1)/2] = (ushort) (binaryFile[i - 1]*256 + binaryFile[i]);
                }

                for (var j = 0; j < 9; j++)
                {
                    var keys = new uint[6];
                    Array.Copy(Status == Status.Coding ? Key : InverseKey, j*6, keys, 0, j != 8 ? 6 : 4);
                    if (j != 8)
                    {
                        Step(ref File, keys, j);
                    }
                    else
                    {
                        File[0] = (ushort)(Mult(File[0], keys[0]));
                        File[1] = (ushort)(Summ(File[1], keys[1]));
                        File[2] = (ushort)(Summ(File[2], keys[2]));
                        File[3] = (ushort)(Mult(File[3], keys[3]));
                    }
                }
                var byteResult = new byte[8];
                for (var i = 0; i < 4; i++)
                {
                    var z = (byte) (File[i] & 255);
                    File[i] >>= 8;
                    byteResult[2*i] = (byte) File[i];
                    byteResult[2*i + 1] = z;
                }
                var o = 8;
                if (Status == Status.Decoding&&k==fs.Length/8-1)
                    while (byteResult[o-1] == 0) { o--; }

                for (var i = 0; i < o; i++)
                    stream.Write(byteResult[i]);
            }

            if (br.PeekChar() >= 0)
            {
                for (var i = 0; i < 8; i++)
                {
                    binaryFile[i] = br.PeekChar() >= 0 ? br.ReadByte() : (byte) 0;
                    if (((i & 1) == 1) && i != 0)
                        File[(i - 1) / 2] = (ushort)(binaryFile[i - 1] * 256 + binaryFile[i]);
                }

                for (var j = 0; j < 9; j++)
                {
                    var keys = new uint[6];
                    Array.Copy(Status == Status.Coding ? Key : InverseKey, j * 6, keys, 0, j != 8 ? 6 : 4);
                    if (j != 8)
                    {
                        Step(ref File, keys, j);
                    }
                    else
                    {
                        File[0] = (ushort)(Mult(File[0],keys[0]));
                        File[1] = (ushort)(Summ(File[1], keys[1]));
                        File[2] = (ushort)(Summ(File[2], keys[2]));
                        File[3] = (ushort)(Mult(File[3], keys[3]));
                    }
                }
                var byteResult = new byte[8];
                for (var i = 0; i < 4; i++)
                {
                    var z = (byte)(File[i] & 255);
                    File[i] >>= 8;
                    byteResult[2 * i] = (byte)File[i];
                    byteResult[2 * i + 1] = z;
                }
                foreach (var t in byteResult)
                    stream.Write(t);
            }
            stream.Close();
            br.Close();
            fs.Close();
        }

        private static ushort[] Inverse(IReadOnlyList<ushort> key)
        {
            var newKey = new ushort[52];
            for (var i = 0; i < 9; i++)
            {
                newKey[i*6 + 0] = MultiInverse(key[(8 - i)*6 + 0]);
                newKey[i*6 + 1] = (ushort) (65536 - key[(8 - i)*6 + (i == 0 || i == 8 ? 1 : 2)]);
                newKey[i*6 + 2] = (ushort) (65536 - key[(8 - i)*6 + (i == 0 || i == 8 ? 2 : 1)]);
                newKey[i*6 + 3] = MultiInverse(key[(8 - i)*6 + 3]);
                if (i == 8) continue;
                newKey[i*6 + 4] = key[(7 - i)*6 + 4];
                newKey[i*6 + 5] = key[(7 - i)*6 + 5];
            }
            return newKey;
        }

        private static void Step(ref ushort[] d, uint[] keys, int stepNumber)
        {
            d[0] = (ushort)(Mult(d[0],keys[0]));
            d[1] = (ushort)(Summ(d[1],keys[1]));
            d[2] = (ushort)(Summ(d[2],keys[2]));
            d[3] = (ushort)(Mult(d[3],keys[3]));

            var t3 = (uint)(d[0] ^ d[2]);
            var t1 = (uint)(Mult(t3, keys[4]));
            var t4 = (uint)(d[1] ^ d[3]);
            var t5 = (uint)(Summ(t4, t1));
            var t2 = (uint)(Mult(t5, keys[5]));
                t1 = (uint)(Summ(t1,t2));

            d[0] = (ushort) (d[0] ^ t2);
            d[1] = (ushort) (d[1] ^ t1);
            d[2] = (ushort) (d[2] ^ t2);
            d[3] = (ushort) (d[3] ^ t1);
            if (stepNumber == 7) return;
            var t7 = d[1];
            d[1] = d[2];
            d[2] = t7;
        }

        private static ulong Mult(ulong a, ulong b)
        {
            return ((a == 0 ? 65536 : a)*(b == 0 ? 65536 : b))%MultMod;
        }

        private static ulong Summ(ulong a, ulong b)
        {
            return (a + b)%SummMod;
        }
    }
}