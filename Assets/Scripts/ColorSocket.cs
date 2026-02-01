using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Threading;

public class ColorSocketReceiver : MonoBehaviour
{
    public NinjaController player;
    UdpClient udp;
    Thread thread;
    int port = 8008;

    volatile int colorId = 0;

    void Start()
    {
        udp = new UdpClient(port);
        thread = new Thread(ReceiveLoop);
        thread.IsBackground = true;
        thread.Start();
    }

    void ReceiveLoop()
    {
        IPEndPoint remote = new IPEndPoint(IPAddress.Any, port);

        while (true)
        {
            try
            {
                byte[] data = udp.Receive(ref remote);
                string msg = Encoding.ASCII.GetString(data);
                colorId = int.Parse(msg);
            }
            catch { }
        }
    }

    void Update()
    {
        // Consume colorId on main thread
        switch (colorId)
        {
            case 1:
                Debug.Log("RED");
                player.setProfile(0);
                break;
            case 2:
                Debug.Log("BLUE");
                player.setProfile(1);
                break;
            case 3:
                Debug.Log("PURPLE");
                player.setProfile(2);
                break;
            case 4:
                Debug.Log("YELLOW");
                player.setProfile(3);
                break;
        }
    }

    void OnApplicationQuit()
    {
        thread?.Abort();
        udp?.Close();
    }

    public void Shutdown()
    {
        thread?.Abort();
        udp?.Close();
        udp = null;
    }
}
