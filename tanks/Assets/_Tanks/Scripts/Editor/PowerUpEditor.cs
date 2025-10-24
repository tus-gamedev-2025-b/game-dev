using UnityEditor;

namespace Tanks.Complete
{
    [CustomEditor(typeof(PowerUp))]
    public class PowerUpEditor : Editor
    {
        private SerializedProperty collectFX;
        private SerializedProperty cooldownReduction;
        private SerializedProperty damageMultiplier;
        private SerializedProperty damageReduce;
        private SerializedProperty durationTime;
        private SerializedProperty healingAmount;
        private SerializedProperty powerUpType;
        private SerializedProperty speedBonus, turnSpeedBonus;

        private void OnEnable()
        {
            // gets references of the fields
            powerUpType = serializedObject.FindProperty("m_PowerUpType");
            durationTime = serializedObject.FindProperty("m_DurationTime");
            collectFX = serializedObject.FindProperty("m_CollectFX");
            speedBonus = serializedObject.FindProperty("m_SpeedBonus");
            turnSpeedBonus = serializedObject.FindProperty("m_TurnSpeedBonus");
            damageReduce = serializedObject.FindProperty("m_DamageReduction");
            cooldownReduction = serializedObject.FindProperty("m_CooldownReduction");
            healingAmount = serializedObject.FindProperty("m_HealingAmount");
            damageMultiplier = serializedObject.FindProperty("m_DamageMultiplier");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Shows the power up type and duration time
            EditorGUILayout.PropertyField(powerUpType);
            EditorGUILayout.PropertyField(collectFX);

            // Shows  the fields of the selected type
            var selectedType = (PowerUp.PowerUpType)powerUpType.enumValueIndex;

            switch (selectedType)
            {
                case PowerUp.PowerUpType.Speed:
                    EditorGUILayout.PropertyField(durationTime);
                    EditorGUILayout.PropertyField(speedBonus);
                    EditorGUILayout.PropertyField(turnSpeedBonus);
                    break;

                case PowerUp.PowerUpType.DamageReduction:
                    EditorGUILayout.PropertyField(durationTime);
                    EditorGUILayout.PropertyField(damageReduce);
                    break;

                case PowerUp.PowerUpType.ShootingBonus:
                    EditorGUILayout.PropertyField(durationTime);
                    EditorGUILayout.PropertyField(cooldownReduction);
                    break;

                case PowerUp.PowerUpType.Healing:
                    EditorGUILayout.PropertyField(healingAmount);
                    break;

                case PowerUp.PowerUpType.DamageMultiplier:
                    EditorGUILayout.PropertyField(damageMultiplier);
                    break;
                case PowerUp.PowerUpType.Invincibility:
                    EditorGUILayout.PropertyField(durationTime);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
