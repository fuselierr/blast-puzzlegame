using UnityEngine;

public interface IGameMode
{
    void Initialize(GameManager gameManager);
    void StartGame();
    void StopGame();
    void ResetGame();
    bool TryPlaceMine();
    void ReturnMine();
    void OnDetonate();
}
