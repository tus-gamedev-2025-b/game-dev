using UnityEngine;

namespace Tanks.Complete
{
    public static class TurretUtility
    {
        /// <summary>
        /// Recursively search for a turret-like Transform 
        /// (child name containing "turret" or "barrel")
        /// </summary>
        public static Transform FindTurretRecursive(Transform parent)
        {
            foreach (Transform child in parent)
            {
                string lowerName = child.name.ToLower();

                if (lowerName.Contains("turret") || lowerName.Contains("barrel"))
                    return child;

                Transform found = FindTurretRecursive(child);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}