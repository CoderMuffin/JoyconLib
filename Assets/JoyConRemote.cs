using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class JoyConRemote: MonoBehaviour
{
    private List<Joycon> joycons;

    // Values made available via Unity
    public float[] stick;
    public UnityEngine.UI.Text debugTxt;
    public Vector3 gyro;
    public Vector3 accel;
    public int jc_ind = 0;
    public Quaternion orientation;
    public Quaternion adj = Quaternion.identity;
    public Quaternion lastOne = Quaternion.identity;
    NetworkStream stream;
    //SERVER
    #region private members 	
    /// <summary> 	
    /// TCPListener to listen for incomming TCP connection 	
    /// requests. 	
    /// </summary> 	
    private TcpListener tcpListener;
    /// <summary> 
    /// Background thread for TcpServer workload. 	
    /// </summary> 	
    private Thread tcpListenerThread;
    /// <summary> 	
    /// Create handle to connected tcp client. 	
    /// </summary> 	
    private TcpClient connectedTcpClient;
    bool canUpdate = false;
    #endregion

    public bool connected = false;
    public bool joyconConnected;

    // Use this for initialization
    void Start()
    {
        adj = Quaternion.identity;
        // Start TcpServer background thread 		
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequests));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
        //tcpListenerThread.Start();
        gyro = new Vector3(0, 0, 0);
        accel = new Vector3(0, 0, 0);
        // get the public Joycon array attached to the JoyconManager in scene
        joycons = JoyconManager.Instance.j;
        if (joycons.Count < jc_ind + 1)
        {
            //Destroy(gameObject);
        }
    }

    string DebugString()
    {
        return ""+BR(orientation.w) +","+ BR(orientation.x) +","+ BR(orientation.y) +","+ BR(orientation.z)+"\n"+(1/Time.fixedDeltaTime)+"\n"+joyconConnected.ToString()+"\n"+connected.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        if (joycons.Count > 0)
        {
            orientation = joycons[jc_ind].GetVector();
            joyconConnected = true;
        }
        else
        {
            orientation = transform.localRotation;
            joyconConnected = false;
        }
        if (joycons[jc_ind].GetButton(Joycon.Button.STICK))
        {
            adj = Quaternion.Inverse(orientation);
            lastOne = Quaternion.identity;
        }
        transform.localRotation = adj*orientation*lastOne*Quaternion.AngleAxis(-Time.fixedDeltaTime * 90 / 13, new Vector3(0, 1, 0));
        lastOne *= Quaternion.AngleAxis(-Time.fixedDeltaTime * 90 / 13,new Vector3(0, 1, 0));
        orientation = transform.localRotation;
        if (connectedTcpClient != null&&!canUpdate)
        {
            Debug.Log("<Socket> Connected!");
            connected = true;
            stream = connectedTcpClient.GetStream();
            canUpdate = true;
        }
        if (canUpdate)
        {
            SendMessage();
        }
        debugTxt.text = DebugString();
    }

    /// <summary> 	
    /// Runs in background TcpServerThread; Handles incomming TcpClient requests 	
    /// </summary> 	
    private void ListenForIncommingRequests()
    {
        try
        {

            Debug.Log("<Socket> Initializing...");
            // Create listener on localhost port 8052. 			
            //try { tcpListener.Stop(); } catch { return; }
            tcpListener = new TcpListener(IPAddress.Any, 4294);
            tcpListener.Start();
            
            Debug.Log("<Socket> Port listener active");
            Byte[] bytes = new Byte[1024];
            while (true)
            {
                using (connectedTcpClient = tcpListener.AcceptTcpClient())
                {
                    // Get a stream object for reading 					
                    using (NetworkStream lstream = connectedTcpClient.GetStream())
                    {
                        int length;
                        // Read incomming stream into byte arrary. 
                        while ((length = lstream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            var incommingData = new byte[length];
                            Array.Copy(bytes, 0, incommingData, 0, length);
                            // Convert byte array to string message.
                            Debug.Log("Message Recieved");
                            string clientMessage = Encoding.ASCII.GetString(incommingData);
                        }
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.LogError("<Socket> Exception: " + socketException.ToString());
            tcpListener.Stop();
        }
    }
    /// <summary> 	
    /// Send message to client using socket connection. 	
    /// </summary> 	
    private void SendMessage()
    {
        if (connectedTcpClient == null)
        {
            return;
        }

        try
        {
            // Get a stream object for writing. 			
            
            if (stream.CanWrite)
            {
                //Debug.Log("w" + orientation.w);
                //Debug.Log("x" + orientation.x);
                //Debug.Log("y" + orientation.y);
                //Debug.Log("z" + orientation.z);
                string serverMessage = orientation.w.ToString() + "," + orientation.x.ToString() + "," + orientation.y.ToString() + "," + orientation.z.ToString() + "," + (joycons[jc_ind].GetButtonDown(Joycon.Button.SHOULDER_2)?1:0) + (joycons[jc_ind].GetButtonDown(Joycon.Button.SHOULDER_1) ? 1 : 0) + (joycons[jc_ind].GetButtonDown(Joycon.Button.PLUS) ? 1 : 0) + (joycons[jc_ind].GetStick()[0]) + (joycons[jc_ind].GetStick()[1]);
                // Convert string message to byte array.
                Debug.Log(serverMessage);
                byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(serverMessage);
                // Write byte array to socketConnection stream.               
                stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
            tcpListener.Stop();
        }
    }
    public void OnApplicationQuit()
    {
        tcpListener.Stop();
    }
    public float BR(float i)
    {
        return Mathf.Round(i * 100) / 100;
    }
}