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
        private GameObject shieldIndicator;

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
                boundDefense.Changed += HandleDefenseChanged;
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

        private void HandleShield()
        {
            Spawn(ShieldFlash, transform.position, Quaternion.identity, 1.5f, new Color(.25f, .9f, .85f, .8f), .28f);
            if (!Application.isPlaying) return;
            if (shieldIndicator) Destroy(shieldIndicator);
            shieldIndicator = CreateRing("Shield Indicator", 1f, new Color(.15f, 1f, .9f, .9f), 0f, null);
        }

        private void HandleEmp()
        {
            Spawn(EmpFlash, transform.position, Quaternion.identity, 2.2f, new Color(.75f, .35f, 1f, .85f), .32f);
            if (!Application.isPlaying) return;
            CreateRing("EMP Indicator", boundDefense ? boundDefense.EmpRadius : 3f,
                new Color(.75f, .35f, 1f, .85f), .45f,
                $"EMP {(boundDefense ? boundDefense.LastEmpAffectedCount : 0)}");
        }

        private void HandleDefenseChanged()
        {
            if (boundDefense && boundDefense.ShieldActive) return;
            if (shieldIndicator) Destroy(shieldIndicator);
            shieldIndicator = null;
        }

        private GameObject CreateRing(string name, float radius, Color color, float lifetime, string label)
        {
            var previous = transform.Find(name);
            if (previous) Destroy(previous.gameObject);
            var effect = new GameObject(name);
            effect.transform.SetParent(transform, false);
            var line = effect.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 48;
            line.startWidth = line.endWidth = .06f;
            line.startColor = line.endColor = color;
            line.sortingOrder = 8;
            for (var i = 0; i < line.positionCount; i++)
            {
                var angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius));
            }
            if (!string.IsNullOrEmpty(label))
            {
                var textObject = new GameObject("Affected count");
                textObject.transform.SetParent(effect.transform, false);
                textObject.transform.localPosition = new Vector3(0f, radius + .25f, 0f);
                var text = textObject.AddComponent<TextMesh>();
                text.text = label;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.characterSize = .07f;
                text.fontSize = 32;
                text.color = color;
            }
            if (lifetime > 0f) Destroy(effect, lifetime);
            return effect;
        }

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
                boundDefense.Changed -= HandleDefenseChanged;
            }
            boundWeapon = null;
            boundDefense = null;
            if (shieldIndicator)
            {
                if (Application.isPlaying) Destroy(shieldIndicator);
                else DestroyImmediate(shieldIndicator);
            }
            shieldIndicator = null;
        }
    }
}
