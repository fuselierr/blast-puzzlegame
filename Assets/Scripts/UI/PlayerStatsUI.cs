using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsCardListUI : MonoBehaviour
{
    [Header("Cards")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private PlayerStatCardUI statCardPrefab;

    [Header("Optional Text")]
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text statusText;

    private readonly List<PlayerStatCardUI> spawnedCards =
        new List<PlayerStatCardUI>();
    private int loadRequestVersion;

    private void Awake()
    {
        cardContainer = ResolveCardContainer();
    }

    private void OnEnable()
    {
        LoadStats();
    }

    private async void LoadStats()
    {
        int requestVersion = ++loadRequestVersion;
        ClearCards();
        SetStatus("Loading stats...");

        try
        {
            if (SupabaseManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "SupabaseManager is not available.");
            }

            PlayerStats stats =
                await SupabaseManager.Instance.GetPlayerStats();

            if (!CanApplyRequest(requestVersion))
            {
                return;
            }

            SetStatus(string.Empty);

            if (stats == null)
            {
                SetStatus("No stats found.");
                return;
            }

            if (usernameText != null)
            {
                usernameText.text = stats.userName;
            }

            AddCard("Mines Placed", stats.minesPlaced);
            AddCard("Debris Broken", stats.debrisBroken);
            AddCard("Freeplay Wins", stats.freeplayWins);
            AddCard("Classic Wins", stats.classicWins);
            AddCard("Custom Level Wins", stats.customLevelWins);
            AddCard(
                "Daily Challenge High Score",
                stats.dailyChallengeHighScore);
            AddCard("Freeplay High Score", stats.freeplayHighScore);
            AddCard("Classic High Score", stats.classicHighScore);
            AddCard("Custom Level Likes", stats.customLevelLikes);
            AddCard("Custom Level Plays", stats.customLevelPlays);

            Transform runtimeContainer = ResolveCardContainer();
            if (runtimeContainer is RectTransform containerRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
            }
        }
        catch (Exception exception)
        {
            if (!CanApplyRequest(requestVersion))
            {
                return;
            }

            ClearCards();
            SetStatus("Could not load player stats.");
            Debug.LogError(
                "Could not display player stats: " + exception,
                this);
        }
    }

    public void Refresh()
    {
        if (isActiveAndEnabled)
        {
            LoadStats();
        }
    }

    private void AddCard(string statName, int statValue)
    {
        Transform runtimeContainer = ResolveCardContainer();

        if (statCardPrefab == null || runtimeContainer == null)
        {
            Debug.LogError(
                    "PlayerStatsCardListUI is missing its card prefab or container.",
                this);
            return;
        }

        PlayerStatCardUI card = Instantiate(
            statCardPrefab,
            runtimeContainer);
        card.SetStat(statName, statValue);
        card.gameObject.SetActive(true);
        spawnedCards.Add(card);
    }

    private Transform ResolveCardContainer()
    {
        if (IsSceneObject(cardContainer))
        {
            return cardContainer;
        }

        if (cardContainer != null)
        {
            Debug.LogWarning(
                "Card Container references a prefab asset. Assign the " +
                "VerticalLayoutGroup object from the scene Hierarchy. " +
                "Falling back to this object's transform.",
                this);
        }

        return IsSceneObject(transform) ? transform : null;
    }

    private static bool IsSceneObject(Transform target)
    {
        return target != null && target.gameObject.scene.IsValid();
    }

    private void ClearCards()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null)
            {
                Destroy(spawnedCards[i].gameObject);
            }
        }

        spawnedCards.Clear();
    }

    private void SetStatus(string message)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message;
        statusText.gameObject.SetActive(
            !string.IsNullOrWhiteSpace(message));
    }

    private bool CanApplyRequest(int requestVersion)
    {
        return this != null &&
               isActiveAndEnabled &&
               requestVersion == loadRequestVersion;
    }
}