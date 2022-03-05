using Unity.Netcode;
using NetworkTransform = Unity.Netcode.Components.NetworkTransform;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Assets.Scripts.Player
{
    public class PlayerController : NetworkBehaviour
    {

        private ulong _localClientId;

        private JetFighter jetFighter;

        private Rigidbody2D _rigidbody;

        private NetworkVariable<Vector2> _lastPosition = new NetworkVariable<Vector2>();

        private NetworkVariable<Quaternion> _lastRotation = new NetworkVariable<Quaternion>();

        [SerializeField] private float _speed = 400f;

        [SerializeField] private float _rotSpeed = 120f;

        private NetworkClient nClient;
        
        
        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            _localClientId = NetworkManager.Singleton.LocalClientId;
            jetFighter = GetComponent<JetFighter>();
            PlayerUI.Instance.SetPlayer();
            SpawnAtRandomPosition();
        }

        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
          
        }

        // Update is called once per frame
        private void Update()
        {
            //Block of code for nonLocalPlayer clients
            if (!IsLocalPlayer)
            {  
                //checking if should interpolate or teleport
                if (Vector2.Distance(transform.position, _lastPosition.Value) > 50)
                {
                    transform.position = _lastPosition.Value;
                    return;
                }

                //Interpolation
                LerpPosition();
                return;
            }

            //Block of code for Clients
            jetFighter.CheckSwitchWeapon();
            jetFighter.CheckFire();
            jetFighter.CheckTurn();
            
            //Block of code for Server
            if (!IsServer) return;
            //TODO Fix ConstrainToMap to work from the server
            ConstrainToMap();

        }

        //MonoBehaviour.FixedUpdate has the frequency of the physics system; it is called every fixed frame-rate frame. 
        //Alter it by setting it to your preferred value within a script, or, navigate to Edit > Settings > Time > Fixed Timestep and set it there.
        private void FixedUpdate()
        {
            if (!IsLocalPlayer) return;

            //Block of code for Clients
            Move();

            if (IsClient)
            {
                //Transmitting position to server
                ProvidePositionServerRpc(transform.position);
                //Transmitting rotation to server
                ProvideRotationServerRpc(transform.rotation);
            }
        }

        private void Move()
        {
            transform.Translate(transform.up * _speed * Time.deltaTime, Space.World);
        }

        public void Turn(float turn)
        {
            float rotSpeed = turn > 0 ? -_rotSpeed : _rotSpeed;

            transform.RotateAround(transform.position, Vector3.forward, rotSpeed * Time.deltaTime);
        }

        private void LerpPosition()
        {
            if (Time.deltaTime < 0.001f) return;
            //Smooth out positions between clients
            transform.position = Vector3.Lerp(transform.position, _lastPosition.Value, Time.deltaTime * 15);
            transform.rotation = Quaternion.Lerp(transform.rotation, _lastRotation.Value, Time.deltaTime * 15);
        }

        private void SpawnAtRandomPosition()
        {
            var position = GetRandomPosition();
            transform.position = position;
        }

        private static Vector2 GetRandomPosition()
        {
            return new Vector2(Random.Range(-GameManager.Instance.CanvasWidth, GameManager.Instance.CanvasWidth), Random.Range(-GameManager.Instance.CanvasHeight, GameManager.Instance.CanvasHeight));
        }

        private void ConstrainToMap()
        {
            if (!GameManager.Instance.ConstrainToMap(transform.position, out var resultPosition)) return;
            
            transform.position = resultPosition;    
        }

        [ServerRpc]
        void ProvidePositionServerRpc(Vector3 pos)
        {
            _lastPosition.Value = pos;
        }

        [ServerRpc]
        void ProvideRotationServerRpc(Quaternion rot)
        {
            _lastRotation.Value = rot;
        }

       
        /*
    [ServerRpc]
    private void SubmitRandomPositionRequestServerRpc(ulong clientId)
    {
        var position = GetRandomPosition();
        var player = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
        //player.GetComponent<NetworkTransform>().Teleport(position, player.transform.rotation, player.transform.localScale);
        player.gameObject.transform.position = position;
    }

    [ServerRpc]
    private void SubmitPositionRequestServerRpc(ulong clientId, Vector2 position)
    {
        ChangePlayerPositionClientRPC(clientId, position);
    }

    [ClientRpc]
    private void ChangePlayerPositionClientRPC(ulong clientId, Vector2 position)
    {
        if (!IsLocalPlayer) return;
        var player = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
        player.GetComponent<NetworkTransform>().Interpolate = false;
        player.transform.position = position;
        player.GetComponent<NetworkTransform>().Interpolate = true;
    }

    [ServerRpc]
    private void SubmitTurnRequestServerRpc(ulong clientId, float turn)
    {
        var playerTransfrom = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId).transform;

        Turn(playerTransfrom, turn);
    }
    */
    }

}
