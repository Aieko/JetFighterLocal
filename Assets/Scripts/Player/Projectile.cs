using System.Collections;
using Unity.Netcode;
using UnityEngine;
using NetworkTransform = Unity.Netcode.Components.NetworkTransform;

namespace  Assets.Scripts.Player
{
    public class Projectile : NetworkBehaviour
    {

        #region  components

        private Animator _animator;
        private SpriteRenderer _spriteRenderer;

        #endregion


        #region private variables

        private float _spawnTime;
        private float _currentSpeed;
        private NetworkVariable<Vector2> _lastPosition = new NetworkVariable<Vector2>();
        private int _ownerId = -1;

        private int _clusters;

        private bool _isWhite;
        private bool _isAnimationFinished;
        private bool _hit;


        private Transform _target;

        #endregion

        [SerializeField]
        private float _acceleration = 2f;
        [SerializeField]
        private float _accelerationSpeed = 2f;
        [SerializeField]
        private float _startSpeed = 20f;
        [SerializeField]
        private float _maxSpeed = 150f;
        [SerializeField]
        private float _lifeTime = 5f;
        [SerializeField]
        private bool _shouldAccelerate;
        [SerializeField]
        private bool _shouldExplode;
        [SerializeField]
        private bool _homingMissile;
        [SerializeField]
        private float _rotationSpeed = 150f;
        [SerializeField]
        private float _missileActivationDelay = 0.5f;
        [SerializeField]
        private LayerMask _playerLayerMask;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            
        }

        // Start is called before the first frame update
        private void Start()
        {

            _isAnimationFinished = false;
            _spawnTime = Time.time;
            _currentSpeed = _startSpeed;

            if (_shouldExplode)
            {
                transform.localScale *= 2f;
            }

            if (!IsClient) return;

            var ping = PlayerUI.Instance.Ping;

            StartCoroutine("HighPingHide", ping);

        }

        public void SetOwnerId(ulong id)
        {
            _ownerId = (int)id;
        }

        // Update is called once per frame
        private void Update()
        {
            if (!IsOwner && !_hit)
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

            if (!IsServer) return;

            ConstrainToMap();

            CheckExplosion();
        }

        private void FixedUpdate()
        {
            if (_hit) return;

            Move();

            if (!IsServer) return;

            _lastPosition.Value = transform.position;

        }

        private IEnumerator HighPingHide(float ping)
        {
            GetComponent<Renderer>().enabled = false;

            yield return new WaitForSeconds(ping / 1000f);

            GetComponent<Renderer>().enabled = true;

        }

        private void ConstrainToMap()
        {
            if (!GameManager.Instance.ConstrainToMap(transform.position, out var resultPosition)) return;

            transform.position = resultPosition;
        }

        private void LerpPosition()
        {
            //Smooth out position variables between clients
            transform.position = Vector3.Lerp(transform.position, _lastPosition.Value, Time.deltaTime * 15);
        }

        private void SelfHoming()
        {
            //for self-recognition
            if (_ownerId == -1) return;

            if (_target == null)
            {
                //try to find the target
                Navigate();
                return;
            }
            
            var direction = (Vector2)(_target.position - transform.position);

            //if missile distance to the target will be less than 100, then stop following the target
            if (direction.magnitude <= 100)
            {
                _homingMissile = false;
                return;
            }

            direction.Normalize();
            float dotProduct = Vector2.Dot(direction, transform.right);
            
            //missile wheel control
            if (dotProduct == 0)
            {
                return;
            }
            if (dotProduct > 0)
            {
                transform.RotateAround(transform.position, Vector3.back, _rotationSpeed * Time.deltaTime);
            }
            else if (dotProduct < 0)
            {
                transform.RotateAround(transform.position, Vector3.forward, _rotationSpeed * Time.deltaTime);
            }
        }

        private IEnumerator Accelerate()
        {
            _currentSpeed += _acceleration;

            yield return new WaitForSeconds(_accelerationSpeed);
        }

        private void Explode()
        {
            var bulletDirection = 0f;
            //GameManager.Instance.PlayFireSound((AudioClip) Resources.Load("Sounds/bombExplosion"));

            for (int i = 0; i < 7; i++)
            {
                var spread = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0f, 0f, bulletDirection));
                bulletDirection += 60f;
                _clusters++;
                var bullet = Instantiate((GameObject) Resources.Load("Projectiles/Bullet"), transform.position, spread);
                bullet.GetComponent<NetworkObject>().Spawn();
                bullet.GetComponent<Projectile>().SetOwnerId((ulong)_ownerId);
            }
        }

        private void CheckExplosion()
        {
            if (Time.time >= _spawnTime + _lifeTime || _hit)
            {
                
                if (_shouldExplode && _clusters < 6)
                {
                    _currentSpeed = 0f;
                    _maxSpeed = 0f;
                    transform.localScale /= 2;
                    Explode();
                }

               //TODO fix wierd animation glitch on Client side
                _animator.SetBool("Explosion", true);
                
                
                if (!_isAnimationFinished) return;

                Object.Destroy(gameObject);

            }
        }

        private void Navigate()
        {
            var collider = Physics2D.OverlapCircle(transform.position, 400f, _playerLayerMask);
           
            if (!collider) return;

            var targetClientId = collider.gameObject.GetComponent<JetFighter>().OwnerClientId;

            if ((ulong)_ownerId != targetClientId)
            {
                _target = collider.transform;
            }
        }

        private void Move()
        {
            if (_hit) return;

            if (_shouldAccelerate && _currentSpeed < _maxSpeed)
            {
                StartCoroutine(Accelerate());
            }

            if (_homingMissile && Time.time >= _spawnTime + _missileActivationDelay)
            {
                SelfHoming();
            }

            transform.Translate(transform.up * _currentSpeed * Time.deltaTime, Space.World);
        }

        public void AnimationFinishedTrigger()
        {
            _isAnimationFinished = true;
        }


        private void OnTriggerEnter2D(Collider2D collider)
        {
            //return if ownerId of this projectile don't set yet
            if (_ownerId == -1) return;

            if (!IsServer) return;

            var targetClientId = collider.gameObject.GetComponent<JetFighter>().OwnerClientId;

            if ((ulong)_ownerId != targetClientId)
            {
                _hit = true;

                //TODO some ingame mechanic, maybe decrease Jet's health or add to score  
            }
        }

    }

}
