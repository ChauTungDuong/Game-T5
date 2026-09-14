using UnityEngine;

namespace CoreGuard
{
    public sealed class AimCursorPresenter : MonoBehaviour
    {
        public GameSession Session;
        public InputReader Input;
        public SpriteRenderer Renderer;

        private void Awake()
        {
            if (!Renderer) Renderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (!Renderer || !Session || !Input || Session.State != MatchState.Playing)
            {
                if (Renderer) Renderer.enabled = false;
                return;
            }

            transform.position = Input.AimWorld;
            Renderer.enabled = true;
        }
    }
}
