using Blue.Input;
using UnityEngine;

namespace Blue.Game
{
    public class CharacterController : MonoBehaviour
    {
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Transform camTransform;
        [SerializeField] private InputMapType defaultInputMap;

        [Header("プレイヤーの操作に関する値")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 4f;
        [SerializeField] private float maxLookUpAngle = 80f;
        
        private PlayerInputHandler inputHandler;
        private float camVerticalRotation = 0f;
        private float mouseLookSensitivity = 10f;

        void Start()
        {
            Initialize();
        }

        void Update()
        {
            HandleMove();
            HandleViewRotation();

            // 仮の実装
            if (SceneLoader.CurrentSceneName == "Aquarium")
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Tab)) SceneLoader.LoadScene("Title");
            } 
        }

        private void Initialize()
        {
            inputHandler = new PlayerInputHandler();

            Cursor.lockState = CursorLockMode.Locked;
            inputHandler.SetInputMap(defaultInputMap);  
        }

        private void HandleMove()
        {
            Vector2 move_input = inputHandler.MoveInput;
            Vector3 forward = camTransform.forward;
            Vector3 right = camTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 move_direction = (forward * move_input.y + right * move_input.x).normalized;
            Vector3 current_velocity = rb.linearVelocity;
            Vector3 target_velocity = move_direction * moveSpeed;
            target_velocity.y = current_velocity.y;

            if (move_input.sqrMagnitude > 0.01f)
            {
                // 入力がある場合は加速
                Vector3 new_velocity = Vector3.MoveTowards(current_velocity, target_velocity, acceleration * Time.deltaTime);
                rb.linearVelocity = new_velocity;
            }
            else
            {
                // 入力がない場合は減速（慣性）
                Vector3 horizontal_velocity = new Vector3(current_velocity.x, 0f, current_velocity.z);
                horizontal_velocity = Vector3.MoveTowards(horizontal_velocity, Vector3.zero, deceleration * Time.deltaTime);
                rb.linearVelocity = new Vector3(horizontal_velocity.x, current_velocity.y, horizontal_velocity.z);
            }
        }

        private void HandleViewRotation()
        {
            Vector2 look_input = inputHandler.LookInput;
            Vector2 adjusted_look_input = look_input * mouseLookSensitivity * Time.deltaTime;

            camVerticalRotation -= adjusted_look_input.y;
            camVerticalRotation = Mathf.Clamp(camVerticalRotation, -maxLookUpAngle, maxLookUpAngle);
            camTransform.localRotation = Quaternion.Euler(camVerticalRotation, 0f, 0f);

            transform.Rotate(Vector3.up * adjusted_look_input.x);
        }
    }
}