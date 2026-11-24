using ChronowalkGame.Game.PlayerEntities;
using ChronowalkGame.Utils;
using UnityEngine;

namespace ChronowalkGame.UI
{
    public class PlayerCamera : MonoSingleton<PlayerCamera>
    {
        [SerializeField] private Vector3 offset;
        [SerializeField] private Vector3 eulerAngles;
        [SerializeField] private float distance;
        [SerializeField] private Camera pixelationCamera;
        [SerializeField, Min(1f)] private float pixelsPerUnit = 16f;

        private bool missingPixelationCameraWarningLogged;
        private bool missingPixelationTextureWarningLogged;

        private void Start()
        {
            transform.position = CalculatePosition();
            UpdatePixelationResolution();
        }

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, CalculatePosition(), Time.deltaTime * 5f);
        }

        private void LateUpdate()
        {
            UpdatePixelationResolution();
        }

        private Vector3 CalculatePosition()
        {
            Vector3 position = Player.Instance.transform.localPosition;
            Vector3 sphericalOffset = Quaternion.Euler(eulerAngles) * Vector3.forward * distance;
            position += offset + sphericalOffset;
            return position;
        }

        private void UpdatePixelationResolution()
        {
            if (pixelationCamera == null)
            {
                if (!missingPixelationCameraWarningLogged)
                {
                    Debug.LogWarning($"{nameof(PlayerCamera)} on {name} has no pixelation camera assigned.");
                    missingPixelationCameraWarningLogged = true;
                }

                return;
            }

            RenderTexture targetTexture = pixelationCamera.targetTexture;
            if (targetTexture == null)
            {
                if (!missingPixelationTextureWarningLogged)
                {
                    Debug.LogWarning($"{nameof(PlayerCamera)}'s pixelation camera has no target texture assigned.");
                    missingPixelationTextureWarningLogged = true;
                }

                return;
            }

            missingPixelationCameraWarningLogged = false;
            missingPixelationTextureWarningLogged = false;

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return;
            }

            float aspect = screenWidth / (float)screenHeight;
            float viewHeight;
            if (pixelationCamera.orthographic)
            {
                viewHeight = pixelationCamera.orthographicSize * 2f;
            }
            else
            {
                float depth = Vector3.Dot(
                    Player.Instance.transform.position - pixelationCamera.transform.position,
                    pixelationCamera.transform.forward);
                depth = Mathf.Max(depth, pixelationCamera.nearClipPlane);
                viewHeight = 2f * depth * Mathf.Tan(pixelationCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            }

            float requestedHeight = viewHeight * Mathf.Max(1f, pixelsPerUnit);
            float requestedWidth = requestedHeight * aspect;
            float resolutionScale = Mathf.Min(
                1f,
                screenHeight / requestedHeight,
                screenWidth / requestedWidth);

            int height = Mathf.Max(1, Mathf.RoundToInt(requestedHeight * resolutionScale));
            int width = Mathf.Max(1, Mathf.RoundToInt(requestedWidth * resolutionScale));

            if (targetTexture.width != width || targetTexture.height != height)
            {
                targetTexture.Release();
                targetTexture.width = width;
                targetTexture.height = height;
                if (!targetTexture.Create())
                {
                    Debug.LogError($"Could not create pixelation render texture at {width}x{height}.");
                }
            }

            if (!Mathf.Approximately(pixelationCamera.aspect, aspect))
            {
                pixelationCamera.aspect = aspect;
            }
        }

        public void Zoom(float sign)
        {
            float d = distance - sign * Time.deltaTime * 20f;
            distance = Mathf.Clamp(d, -20, -3);
        }
    }
}