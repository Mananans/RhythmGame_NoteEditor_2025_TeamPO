using UnityEngine;
using System.Collections.Generic;

public class VisibilityManager : MonoBehaviour
{
    public Camera mainCamera; // 감지할 카메라
    public List<GameObject> targetObjects = new List<GameObject>(); // 감지할 오브젝트 목록

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // 메인 카메라 자동 할당
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        foreach (GameObject obj in targetObjects)
        {
            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform == null) continue; // RectTransform이 없으면 건너뜀

            // 1️⃣ UI의 World Position을 Viewport 좌표(0~1)로 변환
            Vector3[] worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);

            bool isVisible = false;
            foreach (Vector3 corner in worldCorners)
            {
                Vector3 viewportPoint = mainCamera.WorldToViewportPoint(corner);

                // 2️⃣ Viewport 좌표가 (0,0)~(1,1) 범위 안에 있으면 카메라에 보임
                if (viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                    viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
                    viewportPoint.z > 0) // z가 0보다 커야 카메라 앞에 있음
                {
                    isVisible = true;
                    break;
                }
            }

            obj.SetActive(isVisible); // 화면에 보이면 활성화, 안 보이면 비활성화
        }
    }
}
