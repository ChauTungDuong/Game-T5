using System.Linq;
using UnityEngine;

namespace CoreGuard
{
    [ExecuteAlways]
    public sealed class ArenaBackgroundFitter : MonoBehaviour
    {
        public const float TileSize = 1.28f;
        public const float DefaultWidth = 48f;
        public const float DefaultHeight = 28f;

        public Transform Floor;

        private void Awake() => Fit();
        private void Start() => Fit();
        private void OnEnable() => Fit();

        public void Fit()
        {
            var cam = Camera.main;
            var targetW = DefaultWidth;
            var targetH = DefaultHeight;
            if (cam && cam.orthographic)
            {
                var camH = cam.orthographicSize * 2f;
                var camW = camH * cam.aspect;
                targetW = Mathf.Max(targetW, camW + 8f);
                targetH = Mathf.Max(targetH, camH + 8f);
            }

            if (!Floor) Floor = transform.Find("Floor");
            if (Floor)
            {
                Floor.localPosition = Vector3.zero;
                Floor.localScale = new Vector3(targetW, targetH, 1f);
            }

            var cols = Mathf.CeilToInt(targetW / TileSize);
            if (cols % 2 == 0) cols++;
            var rows = Mathf.CeilToInt(targetH / TileSize);
            if (rows % 2 == 0) rows++;

            Sprite tileSprite = null;
            var firstTile = transform.Find("Ground tile 0-0");
            if (firstTile)
            {
                var sr = firstTile.GetComponent<SpriteRenderer>();
                if (sr) tileSprite = sr.sprite;
            }

            if (!tileSprite)
            {
                foreach (var s in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    if (s && s.name.Contains("tileSand1"))
                    {
                        tileSprite = s;
                        break;
                    }
                }
            }

            if (tileSprite)
            {
                var tileColor = new Color(.19f, .28f, .32f);
                for (var x = 0; x < cols; x++)
                {
                    for (var y = 0; y < rows; y++)
                    {
                        var name = $"Ground tile {x}-{y}";
                        var t = transform.Find(name);
                        var pos = new Vector3((x - (cols - 1) * .5f) * TileSize, (y - (rows - 1) * .5f) * TileSize, 0f);
                        if (!t)
                        {
                            var go = new GameObject(name, typeof(SpriteRenderer));
                            go.transform.SetParent(transform, false);
                            go.transform.localPosition = pos;
                            go.transform.localScale = new Vector3(2f, 2f, 1f);
                            var sr = go.GetComponent<SpriteRenderer>();
                            sr.sprite = tileSprite;
                            sr.color = tileColor;
                            sr.sortingOrder = -9;
                        }
                        else
                        {
                            t.localPosition = pos;
                        }
                    }
                }
            }
        }
    }
}
