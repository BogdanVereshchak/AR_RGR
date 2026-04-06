using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARGameManager : MonoBehaviour
{
    public enum GameState { Setup, Playing, GameOver }

    [Header("AR References")]
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private ARAnchorManager arAnchorManager; 
    [SerializeField] private Camera arCamera;
    
    [Header("Game References")]
    [SerializeField] private ARGameUI uiController;
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private GameObject ballPrefab;
    
    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 60f; 
    [SerializeField] private float shootForce = 15f; 

    private GameState currentState = GameState.Setup;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private List<GameObject> activeTargets = new List<GameObject>();
    
    private int score = 0;
    private float timeRemaining;
    private float timeElapsed = 0f; // Додано для статистики

    void Start()
    {
        if (arCamera == null) arCamera = Camera.main;
        StartSetup();
    }

    void Update()
    {
        if (currentState == GameState.Playing)
        {
            timeRemaining -= Time.deltaTime;
            timeElapsed += Time.deltaTime; // Рахуємо скільки пройшло часу
            
            uiController.UpdateTime(timeRemaining);

            if (timeRemaining <= 0)
            {
                EndGame(false); // Час вийшов (Програш/Кінець)
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (uiController.IsPointerOverUI(Input.mousePosition)) return;

            if (currentState == GameState.Setup) PlaceTarget(Input.mousePosition);
            else if (currentState == GameState.Playing) ShootBall();
        }
    }

    private void PlaceTarget(Vector2 screenPosition)
    {
        if (arRaycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            TrackableId planeId = hits[0].trackableId;

            ARAnchor anchor = arAnchorManager.AttachAnchor(arRaycastManager.GetComponent<ARPlaneManager>().GetPlane(planeId), hitPose);
            
            if (anchor != null)
            {
                GameObject newTarget = Instantiate(targetPrefab, anchor.transform);
                
                // НОВЕ: Робимо так, щоб мішень дивилася на камеру
                Vector3 lookDirection = arCamera.transform.position - newTarget.transform.position;
                lookDirection.y = 0; // Блокуємо нахил по осі Y, щоб мішень стояла рівно на підлозі, а не завалювалась назад
                newTarget.transform.rotation = Quaternion.LookRotation(lookDirection);

                activeTargets.Add(newTarget);
                uiController.UpdateTargetsCount(activeTargets.Count);
            }
        }
    }

    private void ShootBall()
    {
        GameObject ball = Instantiate(ballPrefab, arCamera.transform.position, arCamera.transform.rotation);
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null) rb.AddForce(arCamera.transform.forward * shootForce, ForceMode.Impulse);
    }

    public void StartGame()
    {
        if (activeTargets.Count == 0) return; 

        currentState = GameState.Playing;
        score = 0;
        timeRemaining = gameDuration;
        timeElapsed = 0f;
        
        uiController.UpdateScore(score);
        uiController.SetUIState(GameState.Playing);
    }

    // НОВИЙ МЕТОД: Викликається самою мішенню, коли її ХП падає до 0
    public void TargetDestroyed(GameObject target, int pointsGiven)
    {
        if (currentState == GameState.Playing)
        {
            score += pointsGiven;
            uiController.UpdateScore(score);
            
            activeTargets.Remove(target);
            
            // Якщо мішеней не залишилось - дострокова перемога!
            if (activeTargets.Count == 0)
            {
                EndGame(true);
            }
        }
    }

    private void EndGame(bool allCleared)
    {
        currentState = GameState.GameOver;
        uiController.SetUIState(GameState.GameOver);
        
        // Передаємо в UI статус: чи ми збили все, який рахунок і за який час
        uiController.ShowFinalResults(score, timeElapsed, allCleared);
    }

    public void RestartSetup()
    {
        foreach (var target in activeTargets)
        {
            if (target != null) Destroy(target);
        }
        activeTargets.Clear();
        StartSetup();
    }

    private void StartSetup()
    {
        currentState = GameState.Setup;
        uiController.SetUIState(GameState.Setup);
        uiController.UpdateTargetsCount(0);
    }
}