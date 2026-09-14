using UnityEngine;

namespace CoreGuard
{
    public sealed class CombatVfxPresenter : MonoBehaviour
    {
        public WeaponController Weapon;
        public DefenseController Defense;
        public Transform Muzzle;
        public Sprite BulletFlash;
        public Sprite RocketFlash;
        public Sprite MineFlash;
        public Sprite ShieldFlash;
        public Sprite EmpFlash;

        private WeaponController boundWeapon;
        private DefenseController boundDefense;

        private void OnEnable()
        {
            if (Weapon || Defense) Bind();
        }

        private void OnDisable() => Unbind();

        public void Bind()
        {
            Unbind();
            Weapon = Weapon ? Weapon : GetComponent<WeaponController>();
            Defense = Defense ? Defense : GetComponent<DefenseController>();
            Muzzle = Muzzle ? Muzzle : transform;
            boundWeapon = Weapon;
            boundDefense = Defense;
            if (boundWeapon) boundWeapon.AttackAccepted += HandleAttack;
            if (boundDefense)
            {
                boundDefense.ShieldActivated += HandleShield;
                boundDefense.EmpActivated += HandleEmp;
            }
        }

        private void HandleAttack(WeaponKind kind, Vector2 origin, Vector2 direction)
        {
            var sprite = kind == WeaponKind.Bullet ? BulletFlash : kind == WeaponKind.Rocket ? RocketFlash : MineFlash;
            var scale = kind == WeaponKind.Mine ? .045f : kind == WeaponKind.Rocket ? .055f : .032f;
            var shotDirection = direction.sqrMagnitude > .000001f ? direction.normalized : Vector2.right;
            var angle = Mathf.Atan2(shotDirection.y, shotDirection.x) * Mathf.Rad2Deg;
            // Kenney muzzle particles point upward by default; rotate them so the
            // flash follows the exact muzzle-to-aim direction.
            var rotation = Quaternion.Euler(0, 0, angle - 90f);
            Spawn(sprite, origin + shotDirection * .025f, rotation, scale,
                kind == WeaponKind.Rocket ? new Color(1f, .6f, .25f) : Color.white, .09f);
        }

        private void HandleShield() => Spawn(ShieldFlash, transform.position, Quaternion.identity, 1.5f, new Color(.25f, .9f, .85f, .8f), .28f);

        private void HandleEmp() => Spawn(EmpFlash, transform.position, Quaternion.identity, 2.2f, new Color(.75f, .35f, 1f, .85f), .32f);

        private void Spawn(Sprite sprite, Vector3 position, Quaternion rotation, float scale, Color color, float lifetime)
        {
            if (!sprite || !Application.isPlaying) return;
            var effect = new GameObject("Combat VFX");
            effect.transform.SetParent(transform.parent, true);
            effect.transform.position = position;
            effect.transform.rotation = rotation;
            effect.transform.localScale = Vector3.one * scale;
            var renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 8;
            Destroy(effect, lifetime);
        }

        private void Unbind()
        {
            if (boundWeapon) boundWeapon.AttackAccepted -= HandleAttack;
            if (boundDefense)
            {
                boundDefense.ShieldActivated -= HandleShield;
                boundDefense.EmpActivated -= HandleEmp;
            }
            boundWeapon = null;
            boundDefense = null;
        }
    }
}
