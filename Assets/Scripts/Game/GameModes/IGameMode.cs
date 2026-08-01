using UnityEngine;

public interface IGameMode
{
    void Initialize(GameManager gameManager);
    void StartGame();
    void StopGame();
    void ResetGame();
    bool TryPlaceMine(MineType mineType = MineType.Standard);
    void ReturnMine(MineType mineType = MineType.Standard);
    void OnDetonate();
    int calculateScore();
}
