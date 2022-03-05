using System;
using System.Collections;
using System.Linq;
using System.Text;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine;
using UnityEngine.UI;
using Ping = UnityEngine.Ping;


namespace Assets.Scripts.ConnectionApproval
{
    public class PasswordNetworkManager : MonoBehaviour
    {
        [SerializeField] private TMP_InputField passwordInputField;
        [SerializeField] private TMP_InputField ipAddressInputField;
        [SerializeField] private GameObject passwordEntryUI;
        [SerializeField] private GameObject leaveButton;
        [SerializeField] private Button clientButton;
        [SerializeField] private TMP_Text stateMassage;
        [SerializeField] private TMP_Text versionText;
        
        

        private bool clientPasswordVerification;

        private void Start()
        {
            versionText.text = Application.version;

            leaveButton.SetActive(false);

            NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        }

        private void OnDestroy()
        {

            if (NetworkManager.Singleton == null) return;

            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }

        //Host/Server methods
        private void HandleClientDisconnect(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId && NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.DisconnectClient(clientId);
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                passwordEntryUI.SetActive(false);
                leaveButton.SetActive(true);
            }
        }

        private void HandleServerStarted()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                HandleClientConnected(NetworkManager.Singleton.LocalClientId);
            }
        }

        //method binds to button
        public void Host()
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.Singleton.StartHost();
        }

        private void ApprovalCheck(byte[] connectionData, ulong clientID, NetworkManager.ConnectionApprovedDelegate callback)
        {
            var password = Encoding.ASCII.GetString(connectionData);

            var approval = password == passwordInputField.text;

            callback(true, null, approval, null, null);
        }

        private static bool ValidateIPv4(string ipString)
        {
            if (String.IsNullOrWhiteSpace(ipString))
            {
                return false;
            }

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            byte tempForParsing;

            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }

        //Client Methods
        private IEnumerator ConnectToHost(string address)
        {
            var time = 0;
            //Ping address
            var p = new Ping(address);
            while (!p.isDone)
            {
                if (time < 10)
                {
                    time++;
                    stateMassage.text = "Trying to ping : " + address + ":" + time;
                    yield return new WaitForSeconds(1f);
                }
                else
                {
                    stateMassage.text = "Can not connect to : " + address + "\nConnection timed out!";
                    clientButton.interactable = true;
                    yield break;
                }
            }

            stateMassage.text = "ping for address " + address + " is " + p.time;
            //Start Client
            StartClient(address);

            yield return new WaitForSeconds(1f);

            if (!NetworkManager.Singleton.IsClient)
            {
                stateMassage.text = "Wrong Password";
                clientButton.interactable = true;
                yield break;
            }

            time = 0;

            while (!NetworkManager.Singleton.IsConnectedClient)
            {
                if (time < 10)
                {
                    time++;
                    stateMassage.text = "Trying to connect : " + address + ":" + time;
                    yield return new WaitForSeconds(1f);
                }
                else
                {
                    stateMassage.text = "Can not connect to : " + address + "\nConnection timed out!";
                    NetworkManager.Singleton.Shutdown();
                    clientButton.interactable = true;
                    yield break;
                }
            }

            stateMassage.text = "Connection was successful!\n " + address;
            clientButton.interactable = true;
        }

        private void StartClient(string address)
        {
            //changing connect address
            ((UNetTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport).ConnectAddress = address;

            NetworkManager.Singleton.StartClient();
        }

        //method binds to button
        public void Client()
        {
            clientButton.interactable = false;

            //convert password into bytes for connection data
            NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(passwordInputField.text);

            var addressToConnect = ipAddressInputField.text;

            //checking that printed address has right format
            if (!ValidateIPv4(addressToConnect))
            {
                stateMassage.text = "Wrong ip address format, it should be like : 192.168.88.1 ";
                clientButton.interactable = true;
                return;
            }
            //Try to connect
            StartCoroutine(ConnectToHost(addressToConnect));
        }

        //method binds to button
        public void Leave()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
               
                //NetworkManager.Singleton.DisconnectClient(clientId);

            }

            NetworkManager.Singleton.Shutdown();
            passwordEntryUI.SetActive(true);
           
            leaveButton.SetActive(false);
        }
    }
}