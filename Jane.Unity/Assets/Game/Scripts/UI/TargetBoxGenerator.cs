using Jane.Unity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TargetBoxGenerator : MonoBehaviour
{
    [SerializeField] private List<CheckPoint> checkPointReferences = new();
    public List<CheckPoint> CheckPoints => checkPointReferences;
    private LinkedList<CheckPoint> checkPoints;
    private CheckPoint currentTargetingCheckPoint;

    private Camera mainCam;
    private List<GameObject> checkPointTargetBoxes = new();
    public List<NetworkPlayer> enemyList = new();
    public List<GameObject> enemyTargetBoxes = new();

    public GameObject checkPointTargetBoxPrefab;
    public GameObject enemyTargetBoxPrefab;

    [SerializeField] private Vector2 minSize = new(100, 100);
    [SerializeField] private Vector2 sizeMargin = new(-50, -50);

    void Start()
    {
        mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        checkPoints = new LinkedList<CheckPoint>(checkPointReferences);

        foreach (var checkPoint in checkPoints)
        {
            GameObject targetBox = Instantiate(checkPointTargetBoxPrefab, transform);
            targetBox.SetActive(false);
            checkPoint.DistanceText = targetBox.GetComponent<DistanceText>();
            checkPointTargetBoxes.Add(targetBox);
        }

        checkPointTargetBoxes.First().SetActive(true);
    }

    void FixedUpdate()
    {
        if (checkPoints != null) 
        { 
            UpdateCheckpointBox();
        }
        if (enemyList != null)
        {
            UpdatePlayerBox();
        }
    }

    private void UpdateCheckpointBox()
    {
        bool isInView = IsInScreen(currentTargetingCheckPoint.transform.position);
        currentTargetingCheckPoint.GetComponent<CanvasGroup>().alpha = isInView ? 1 : 0;

        if (isInView is false) { return; }

        Rect targetRect = GetBoundsInScreenSpace(currentTargetingCheckPoint.gameObject, mainCam);
        RectTransform targetRectTransform = currentTargetingCheckPoint.gameObject.GetComponent<RectTransform>();

        targetRectTransform.position = new Vector2(targetRect.center.x, targetRect.center.y);
        targetRectTransform.sizeDelta = new Vector2(Mathf.Max(targetRect.width, minSize.x), Mathf.Max(targetRect.height, minSize.y)) + sizeMargin;

        currentTargetingCheckPoint.DistanceText.UpdateDistance(currentTargetingCheckPoint.transform.position);
    }

    private void UpdatePlayerBox()
    {
        for (int i = 0; i < enemyList.Count; i++)
        {
            bool isInView = IsInScreen(enemyList[i].transform.position);
            enemyTargetBoxes[i].GetComponent<CanvasGroup>().alpha = isInView ? 1 : 0;

            if (isInView)
            {
                Rect targetRect = GetBoundsInScreenSpace(enemyList[i].gameObject, mainCam);
                RectTransform targetRectTransform = enemyTargetBoxes[i].GetComponent<RectTransform>();

                targetRectTransform.position = new Vector2(targetRect.center.x, targetRect.center.y);
                targetRectTransform.sizeDelta = new Vector2(Mathf.Max(targetRect.width, minSize.x), Mathf.Max(targetRect.height, minSize.y)) + sizeMargin;
                enemyTargetBoxes[i].GetComponent<DistanceText>().UpdateDistance(enemyList[i].transform.position);
            }
        }
    }
    
    public static Rect GetBoundsInScreenSpace(GameObject targetObj, Camera camera)
    {
        Bounds targetBounds = targetObj.GetComponent<Renderer>().bounds;
        Vector3[] screenSpaceCorners = new Vector3[8];

        screenSpaceCorners[0] = camera.WorldToScreenPoint(new Vector3(targetBounds.center.x + targetBounds.extents.x, targetBounds.center.y + targetBounds.extents.y, targetBounds.center.z + targetBounds.extents.z));
        screenSpaceCorners[1] = camera.WorldToScreenPoint(new Vector3(targetBounds.center.x + targetBounds.extents.x, targetBounds.center.y + targetBounds.extents.y, targetBounds.center.z - targetBounds.extents.z));
        screenSpaceCorners[2] = camera.WorldToScreenPoint(new Vector3(targetBounds.center.x + targetBounds.extents.x, targetBounds.center.y - targetBounds.extents.y, targetBounds.center.z + targetBounds.extents.z));
        screenSpaceCorners[3] = camera.WorldToScreenPoint(new Vector3(targetBounds.center.x + targetBounds.extents.x, targetBounds.center.y - targetBounds.extents.y, targetBounds.center.z - targetBounds.extents.z));
        screenSpaceCorners[4] = camera.WorldToScreenPoint(new Vector3(targetBounds.center.x - targetBounds.extents.x, targetBounds.center.y + targetBounds.extents.y, targetBounds.center.z + targetBounds.extents.z));
        screenSpaceCorners[5] = camera.WorldToScreenPoint(new Vector3(targetBounds.center.x - targetBounds.extents.x, targetBounds.center.y + targetBounds.extents.y, targetBounds.center.z - targetBounds.extents.z));
        screenSpaceCorners[6] = camera.WorldToScreenPoint(new Vector3(targetBounds.center.x - targetBounds.extents.x, targetBounds.center.y - targetBounds.extents.y, targetBounds.center.z + targetBounds.extents.z));
        screenSpaceCorners[7] = camera.WorldToScreenPoint(new Vector3(targetBounds.center.x - targetBounds.extents.x, targetBounds.center.y - targetBounds.extents.y, targetBounds.center.z - targetBounds.extents.z));

        float min_x = screenSpaceCorners[0].x;
        float min_y = screenSpaceCorners[0].y;
        float max_x = screenSpaceCorners[0].x;
        float max_y = screenSpaceCorners[0].y;

        for (int i = 1; i < 8; i++)
        {
            if (screenSpaceCorners[i].x < min_x)
            {
                min_x = screenSpaceCorners[i].x;
            }
            if (screenSpaceCorners[i].y < min_y)
            {
                min_y = screenSpaceCorners[i].y;
            }
            if (screenSpaceCorners[i].x > max_x)
            {
                max_x = screenSpaceCorners[i].x;
            }
            if (screenSpaceCorners[i].y > max_y)
            {
                max_y = screenSpaceCorners[i].y;
            }
        }
        return Rect.MinMaxRect(min_x, min_y, max_x, max_y);
    }

    public bool IsInScreen(Vector3 targetPos)
    {
        Vector3 viewportPos = mainCam.WorldToViewportPoint(targetPos);
        if ((0 < viewportPos.x && viewportPos.x < 1) && (0 < viewportPos.y && viewportPos.y < 1) && 0 < viewportPos.z)
        {
            return true;
        }
        return false;
    }
    
    public void AddEnemyTargetBox(NetworkPlayer enemyPlayer)
    {
        enemyList.Add(enemyPlayer);
        GameObject targetBox = Instantiate(enemyTargetBoxPrefab, transform);

        enemyTargetBoxes.Add(targetBox);
        targetBox.GetComponent<DistanceText>().SetName(enemyPlayer.UserId);
    }
}
