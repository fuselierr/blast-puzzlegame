using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class VerificationManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GridSpawner gridSpawner;
    [Tooltip("Load this custom level from persistent storage when this object starts.")]
    [SerializeField] private string customLevelIdentifier;
    [Tooltip("Delay before detonating all mines and letting projectiles resolve.")]
    [SerializeField] private float detonationDelay = 3f;

    private CustomLevelData customLevelData;
    private bool verificationStarted;

    public event Action<bool> OnVerificationComplete;

    private void Start()
    {
        if (gridSpawner == null)
        {
            gridSpawner = FindObjectOfType<GridSpawner>();
        }

        if (gridSpawner == null)
        {
            Debug.LogError("VerificationManager requires a GridSpawner reference.", this);
            return;
        }

        if (!TryLoadLevelData())
        {
            return;
        }

        StartVerification();
    }

    public void StartVerification()
    {
        if (verificationStarted)
        {
            return;
        }

        if (customLevelData == null)
        {
            return;
        }

        gridSpawner.LoadLevel(customLevelData);
        verificationStarted = true;
        StartCoroutine(VerificationRoutine());
    }

    public void StartVerification(CustomLevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("VerificationManager received null custom level data.", this);
            return;
        }

        customLevelData = levelData;
        StartVerification();
    }

    private bool TryLoadLevelData()
    {
        customLevelData = CustomLevelStorage.TakePendingEdit();
        if (customLevelData != null)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(customLevelIdentifier))
        {
            return false;
        }

        customLevelData = CustomLevelStorage.Load(customLevelIdentifier);
        if (customLevelData == null)
        {
            Debug.LogWarning($"VerificationManager could not load custom level '{customLevelIdentifier}'.", this);
            return false;
        }

        return true;
    }

    private IEnumerator VerificationRoutine()
    {
        if (detonationDelay > 0f)
        {
            yield return new WaitForSeconds(detonationDelay);
        }

        if (gridSpawner == null)
        {
            yield break;
        }

        gridSpawner.DetonateMinesAndResolve(HandleDetonationResolved);
    }

    private void HandleDetonationResolved(bool debrisCleared)
    {
        if (debrisCleared)
        {
            HandleVerificationSuccess();
        }
        else
        {
            HandleVerificationFailure();
        }

        OnVerificationComplete?.Invoke(debrisCleared);
    }

    private void HandleVerificationSuccess()
    {
        Debug.Log("Verification complete: all debris cleared.", this);
        if (customLevelData != null)
        {
            customLevelData.verified = true;
        }
    }

    private void HandleVerificationFailure()
    {
        Debug.Log("Verification complete: debris remains.", this);
        if (customLevelData != null)
        {
            customLevelData.verified = false;
        }
    }
}
