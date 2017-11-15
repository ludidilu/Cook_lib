using System;
using System.IO;

namespace Cook_lib
{
    internal static class CookTest
    {
        public static CookMain client;

        public static CookMain server;

        public static void Check()
        {
            if (client == null || server == null)
            {
                return;
            }

            byte[] clientBytes;

            byte[] serverBytes;

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    client.ToBytes(bw);

                    clientBytes = ms.ToArray();
                }
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    server.ToBytes(bw);

                    serverBytes = ms.ToArray();
                }
            }

            if (clientBytes.Length != serverBytes.Length)
            {
                Print();

                throw new Exception("error!");
            }

            for (int i = 0; i < clientBytes.Length; i++)
            {
                if (clientBytes[i] != serverBytes[i])
                {
                    Print();

                    throw new Exception("error!");
                }
            }

            server = null;

            client = null;
        }

        private static void Print()
        {
            Log.Write("server:" + server.GetString());

            Log.Write("client:" + client.GetString());
        }
    }
}
