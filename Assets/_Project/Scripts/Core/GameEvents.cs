using System;
using UnityEngine;

namespace Tilcayo.Core
{
    /// <summary>
    /// Event bus estático desacoplado para Tilcayo: Pesadilla en Sistemas.
    /// Comunica subsistemas sin acoplamiento directo.
    /// </summary>
    public static class GameEvents
    {
        // Jugador & Salud
        public static event Action<int, int> OnPlayerDamaged; // currentHealth, maxHealth
        public static event Action OnPlayerDied;
        public static event Action<int> OnPlayerHealed;

        // Habilidad Slime TP
        public static event Action<Vector3, Vector3> OnTeleportUsed; // fromPos, toPos
        public static event Action<float> OnCooldownUpdate; // 0.0f a 1.0f (1.0 = listo)
        public static event Action OnSlimeSpawned;
        public static event Action OnSlimeExpired;

        // Checkpoints y Coleccionables
        public static event Action<Vector3> OnCheckpointReached; // checkpoint position
        public static event Action<int> OnDiskCollected; // current disk count

        // Niveles y Progresión
        public static event Action OnLevelCompleted;
        public static event Action<string> OnRequestLoadLevel;

        // Jefe Final
        public static event Action<int, int> OnBossDamaged; // boss current, max
        public static event Action OnBossDefeated;

        // Triggers seguros de eventos
        public static void TriggerPlayerDamaged(int current, int max) => OnPlayerDamaged?.Invoke(current, max);
        public static void TriggerPlayerDied() => OnPlayerDied?.Invoke();
        public static void TriggerPlayerHealed(int current) => OnPlayerHealed?.Invoke(current);

        public static void TriggerTeleportUsed(Vector3 fromPos, Vector3 toPos) => OnTeleportUsed?.Invoke(fromPos, toPos);
        public static void TriggerCooldownUpdate(float percent) => OnCooldownUpdate?.Invoke(percent);
        public static void TriggerSlimeSpawned() => OnSlimeSpawned?.Invoke();
        public static void TriggerSlimeExpired() => OnSlimeExpired?.Invoke();

        public static void TriggerCheckpointReached(Vector3 pos) => OnCheckpointReached?.Invoke(pos);
        public static void TriggerDiskCollected(int totalDisks) => OnDiskCollected?.Invoke(totalDisks);

        public static void TriggerLevelCompleted() => OnLevelCompleted?.Invoke();
        public static void TriggerRequestLoadLevel(string sceneName) => OnRequestLoadLevel?.Invoke(sceneName);

        public static void TriggerBossDamaged(int current, int max) => OnBossDamaged?.Invoke(current, max);
        public static void TriggerBossDefeated() => OnBossDefeated?.Invoke();
    }
}
