using System;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 編集モードの入力。差し替えるときはこのクラスだけを直す
    /// </summary>
    // 暫定実装。Aquarium の InputActionMap には Move / Look しか無く、設置・撤去・回転に
    // あたるアクションが無い。増やすには .inputactions と生成コードの両方を更新する必要が
    // あるため、まずは旧 Input で通す。Project Settings は Both なので併用できる
    [Serializable]
    public class AquariumEditInput
    {
        [SerializeField] private KeyCode rotateKey = KeyCode.R;
        [SerializeField] private KeyCode removeModifierKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode nextPieceKey = KeyCode.E;
        [SerializeField] private KeyCode previousPieceKey = KeyCode.Q;
        [SerializeField] private KeyCode toggleModeKey = KeyCode.Tab;

        [Header("カメラ")]
        [SerializeField] private KeyCode panForwardKey = KeyCode.W;
        [SerializeField] private KeyCode panBackKey = KeyCode.S;
        [SerializeField] private KeyCode panLeftKey = KeyCode.A;
        [SerializeField] private KeyCode panRightKey = KeyCode.D;

        public Vector2 PointerPosition => UnityEngine.Input.mousePosition;
        public bool Commit => UnityEngine.Input.GetMouseButtonDown(0);
        public bool Cancel => UnityEngine.Input.GetMouseButtonDown(1);
        public bool Rotate => UnityEngine.Input.GetKeyDown(rotateKey);
        public bool RemoveHeld => UnityEngine.Input.GetKey(removeModifierKey);
        public bool NextPiece => UnityEngine.Input.GetKeyDown(nextPieceKey);
        public bool PreviousPiece => UnityEngine.Input.GetKeyDown(previousPieceKey);
        public bool ToggleMode => UnityEngine.Input.GetKeyDown(toggleModeKey);
        public float Zoom => UnityEngine.Input.mouseScrollDelta.y;

        /// <summary>
        /// カメラを動かす向き。x が左右、y が奥行き
        /// </summary>
        public Vector2 Pan
        {
            get
            {
                float horizontal = 0f;
                float vertical = 0f;

                if (UnityEngine.Input.GetKey(panLeftKey)) horizontal -= 1f;
                if (UnityEngine.Input.GetKey(panRightKey)) horizontal += 1f;
                if (UnityEngine.Input.GetKey(panBackKey)) vertical -= 1f;
                if (UnityEngine.Input.GetKey(panForwardKey)) vertical += 1f;

                return new Vector2(horizontal, vertical);
            }
        }
    }
}
