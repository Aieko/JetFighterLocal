using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Player
{

    public class JetFighter : NetworkBehaviour
    {

        private Weapon1 _minigun;
        private Weapon2 _rocketLauncher;
        private Weapon3 _bomb;

        private int _currentWeaponNum = 1;
        public Weapon CurrentWeapon { get; private set; }

        private float _switchCoolDown = 2f;
        private float _lastTimeSwitched;

        private ulong _localClientId;
        private PlayerController playerController;

        private void PreSwitchWeapon()
        {
            if (Time.time < _lastTimeSwitched + _switchCoolDown)
            {
                PlayerUI.Instance.ShowReloadText();
                //@TODO add sound for deny switching
                return;
            }
            
            if(IsServer)
            {
                SwitchWeapon();
            }
            else
            {
                SwitchWeaponServerRpc(_localClientId);
            }

            _lastTimeSwitched = Time.time;
        }
         
        private void SwitchWeapon(ulong clientId = 0)
        {
            switch (_currentWeaponNum)
            {
                case 1:
                    _currentWeaponNum = 2;
                    CurrentWeapon = _rocketLauncher;
                    break;
                case 2:
                    _currentWeaponNum = 3;
                    CurrentWeapon = _bomb;
                    break;
                case 3:
                    _currentWeaponNum = 1;
                    CurrentWeapon = _minigun;
                    break;
            }

            if (!IsServer) return;

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };


            ChangeWeaponTextClientRpc(CurrentWeapon.Name, clientRpcParams);
        }

        [ClientRpc]
        private void ChangeWeaponTextClientRpc(string weaponName, ClientRpcParams clientRpcParams = default)
        {
            PlayerUI.Instance.ChangeWeaponText(weaponName);
            //@TODO add sound for switching
        }

        [ServerRpc]
        private void SwitchWeaponServerRpc(ulong clientId)
        {
            var player = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId).gameObject;
            var jetFighter = player.GetComponent<JetFighter>();

            jetFighter.SwitchWeapon(clientId);
        }

        [ServerRpc]
        private void ShootServerRpc(ulong clientId)
        {
            var player = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId).gameObject;
            var jetFighter = player.GetComponent<JetFighter>();
           
            if(!jetFighter.CurrentWeapon.CanFire())
            {
                if (jetFighter.CurrentWeapon.OutOfAmmo)
                {
                    if (jetFighter.CurrentWeapon.Reload()) return;

                    ClientRpcParams clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { clientId }
                        }
                    };

                    ShowReloadTextClientRpc(clientRpcParams);
                }

                return;
            }

            var rotation = Quaternion.LookRotation(Vector3.forward, gameObject.transform.up);

            jetFighter.CurrentWeapon.Fire(player.transform, rotation, clientId);
        }

        [ClientRpc]
        private void ShowReloadTextClientRpc(ClientRpcParams clientRpcParams = default)
        {
            PlayerUI.Instance.ShowReloadText();
        }

        public void CheckSwitchWeapon()
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                PreSwitchWeapon();
            }
        }

        public void CheckFire()
        {
            if (Input.GetKey(KeyCode.Space))
            {
               if(IsServer)
                {
                    if (!CurrentWeapon.CanFire())
                    {
                        if (CurrentWeapon.OutOfAmmo)
                        {
                            if (CurrentWeapon.Reload()) return;

                            PlayerUI.Instance.ShowReloadText();
                        }
                        return;
                    }

                    var rotation = Quaternion.LookRotation(Vector3.forward, gameObject.transform.up);
                    CurrentWeapon.Fire(transform, rotation, _localClientId);
                }
                else ShootServerRpc(_localClientId);

            }
        }

        public void CheckTurn()
        {
            if(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
            {
                playerController.Turn(Input.GetAxis("Horizontal")); 
            }
        }


        private void Awake()
        {
            playerController = GetComponentInParent<PlayerController>();
            _localClientId = NetworkManager.Singleton.LocalClientId;
            _minigun = new Weapon1();
            _rocketLauncher = new Weapon2();
            _bomb = new Weapon3();

            CurrentWeapon = _minigun;
        }

    }

}
