using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Player
{
    public abstract class Weapon
    {
        protected float ReloadTime;
        protected float RateOfFire;
        protected float LastTimeFired;
        
        protected GameObject Bullet;
        protected AudioClip BulletAudioClip;
        
        protected int CurrentAmmoAmount;
        protected int MaxAmmoAmount;

        public abstract void Fire(Transform sceneObject, Quaternion rotation, ulong clientId);
        public bool OutOfAmmo { get; protected set; }
        public string Name { get; protected set; }

        public bool Reload()
        {
            if (Time.time >= LastTimeFired + ReloadTime)
            {
                CurrentAmmoAmount = MaxAmmoAmount;
                OutOfAmmo = false;
                return true;
            }
            else
                return false;
            
        }

        public virtual bool CanFire()
        {
            return !OutOfAmmo && !(Time.time <= LastTimeFired + RateOfFire);
        }
    }
    public class Weapon1 : Weapon
    {
        public Weapon1()
        {
            Name = "Minigun";
            Bullet = (GameObject)Resources.Load("Projectiles/Bullet");
            BulletAudioClip = (AudioClip)Resources.Load("Sounds/Minigun");
            ReloadTime = 2f;
            RateOfFire = 0.1f;
            MaxAmmoAmount = 10;
            CurrentAmmoAmount = MaxAmmoAmount;
        }

        public override void Fire(Transform sceneObject, Quaternion rotation, ulong clientId)
        { 
            //GameManager.Instance.PlayFireSound(BulletAudioClip);
            var bullet = Object.Instantiate(Bullet, sceneObject.position, rotation);
            bullet.GetComponent<NetworkObject>().Spawn();
            bullet.GetComponent<Projectile>().SetOwnerId(clientId);
            LastTimeFired = Time.time;
            CurrentAmmoAmount--;

            if (CurrentAmmoAmount <= 0)
            {
                OutOfAmmo = true;
            }
        }

    }

    public class Weapon2 : Weapon
    {
        public Weapon2()
        {
            Name = "Missiles";
            Bullet = (GameObject)Resources.Load("Projectiles/Rocket");
            BulletAudioClip = (AudioClip)Resources.Load("Sounds/Rocket");
            ReloadTime = 1.5f;
            MaxAmmoAmount = 2;
            CurrentAmmoAmount = MaxAmmoAmount;

        }

        public override void Fire(Transform sceneObject, Quaternion rotation, ulong clientId)
        {
            var bullet = Object.Instantiate(Bullet, sceneObject.Find("LeftGun").position, rotation);
            bullet.GetComponent<NetworkObject>().Spawn();
            bullet.GetComponent<Projectile>().SetOwnerId(clientId);
            var bullet2 = Object.Instantiate(Bullet, sceneObject.Find("RightGun").position, rotation);
            bullet2.GetComponent<NetworkObject>().Spawn();
            bullet2.GetComponent<Projectile>().SetOwnerId(clientId);
            CurrentAmmoAmount -= 2;
            LastTimeFired = Time.time;
            //GameManager.Instance.PlayFireSound(BulletAudioClip);
            if (CurrentAmmoAmount <= 0)
            {
                OutOfAmmo = true;
            }

        }

    }

    public class Weapon3 : Weapon
    {
        public Weapon3()
        {
            Name = "Bomb";
            Bullet = (GameObject)Resources.Load("Projectiles/Bomb");
            BulletAudioClip = (AudioClip)Resources.Load("Sounds/Bomb");
            ReloadTime = 2f;
            MaxAmmoAmount = 1;
            CurrentAmmoAmount = MaxAmmoAmount;
        }

        public override void Fire(Transform sceneObject, Quaternion rotation, ulong clientId)
        {
            var bullet = Object.Instantiate(Bullet, sceneObject.position, rotation);
            bullet.GetComponent<NetworkObject>().Spawn();
            bullet.GetComponent<Projectile>().SetOwnerId(clientId);
            LastTimeFired = Time.time;
            CurrentAmmoAmount--;
           //GameManager.Instance.PlayFireSound(BulletAudioClip);
            if (CurrentAmmoAmount <= 0)
            {
                OutOfAmmo = true;
            }
        }

    }
}
