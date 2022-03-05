using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Player;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Instance;
    public float Ping { get; private set; }

    private JetFighter _player;

    [SerializeField] private TMP_Text _weaponText;
    [SerializeField] private TMP_Text _reloadingText;
    [SerializeField] private TMP_Text _rttText;
    [SerializeField] private bool _showLatency = true;

    public bool ReloadingCoroutineIsRunning { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        _reloadingText.canvasRenderer.SetAlpha(0);

        StartCoroutine("ShowRTT");
    }

    private void Awake()
    {
        MakeInstance();
    }

    private void MakeInstance()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != null)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator ShowRTT()
    {
        start:
       while(_showLatency)
        {
            Ping = (int)((NetworkManager.Singleton.LocalTime.TimeAsFloat - NetworkManager.Singleton.ServerTime.TimeAsFloat) * 1000f);
            _rttText.text = Ping.ToString();
            
            yield return new WaitForSeconds(2f);
        }
            
        yield return new WaitUntil(() => _showLatency);

        goto start;
    }


    public void ChangeWeaponText(string weaponName)
    {
        _weaponText.text = weaponName;
    }


    public void ShowReloadText()
    {
        if (ReloadingCoroutineIsRunning) return;
       
        StartCoroutine(ShowReloadTextEnumerator());
        ReloadingCoroutineIsRunning = true;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }


    private IEnumerator ShowReloadTextEnumerator()
    {
        var time = 0;

        while (time < 3)
        {
            if (_reloadingText.canvasRenderer.GetAlpha() >= 254)
            {
                _reloadingText.canvasRenderer.SetAlpha(0);
            }
            else
            {
                _reloadingText.canvasRenderer.SetAlpha(255f);
            }

            yield return new WaitForSeconds(0.5f);

            time++;
        }

        _reloadingText.canvasRenderer.SetAlpha(0);
        ReloadingCoroutineIsRunning = false;
    }

    public void SetPlayer()
    {
        _player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<JetFighter>();
        ChangeWeaponText(_player.CurrentWeapon.Name);
    }

}
