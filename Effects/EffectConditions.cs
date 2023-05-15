using Kitchen;
using KitchenData;
using KitchenMods;
using Unity.Collections;
using Unity.Entities;

namespace PreferenceSystem.Effects
{

    public struct CEffectPreferenceCondition : IEffectCondition, IComponentData, IModComponent
    {
        public enum Condition
        {
            Never,
            Always,
            WhileBeingUsed,
            AtDay,
            AtNight
        }

        public FixedString128 ModGUID;
        public FixedString128 BoolPreferenceID;
        public Condition ConditionWhenEnabled = Condition.Always;
        public Condition ConditionWhenDisabled = Condition.Never;

        public CEffectPreferenceCondition() { }

        public CEffectPreferenceCondition(PreferenceSystemManager preferenceManager, string boolPreferenceID)
        {
            ModGUID = preferenceManager.MOD_GUID;
            BoolPreferenceID = boolPreferenceID;
        }
    }

    [UpdateInGroup(typeof(ActivateEffectsGroup))]
    public class ActivateEffectPreference : GameSystemBase, IModSystem
    {
        EntityQuery AppliesEffects;
        protected override void Initialise()
        {
            base.Initialise();
            AppliesEffects = GetEntityQuery(new QueryHelper()
                .All(typeof(CAppliesEffect), typeof(CEffectPreferenceCondition)));
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> entities = AppliesEffects.ToEntityArray(Allocator.Temp);
            using NativeArray<CEffectPreferenceCondition> preferences = AppliesEffects.ToComponentDataArray<CEffectPreferenceCondition>(Allocator.Temp);
            using NativeArray<CAppliesEffect> appliesEffects = AppliesEffects.ToComponentDataArray<CAppliesEffect>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                CEffectPreferenceCondition preference = preferences[i];
                CAppliesEffect effect = appliesEffects[i];

                bool preferenceEnabled = PreferenceSystemRegistry.GetPreferenceValue<bool>(preference.ModGUID.ConvertToString(), preference.BoolPreferenceID.ConvertToString());
                CEffectPreferenceCondition.Condition activeCondition = preferenceEnabled ? preference.ConditionWhenEnabled : preference.ConditionWhenDisabled;
                effect.IsActive = GetActiveState(activeCondition, in entity);
                Set(entity, effect);
            }
        }

        private bool GetActiveState(CEffectPreferenceCondition.Condition condition, in Entity entity)
        {
            float timeOfDay = GetOrDefault<STime>().TimeOfDay;
            bool is_night = timeOfDay > 0.66f;
            switch (condition)
            {
                case CEffectPreferenceCondition.Condition.Never:
                    return false;
                case CEffectPreferenceCondition.Condition.Always:
                    return true;
                case CEffectPreferenceCondition.Condition.WhileBeingUsed:
                    if (RequireBuffer(entity, out DynamicBuffer<CBeingActedOnBy> actors))
                    {
                        foreach (CBeingActedOnBy actor in actors)
                        {
                            if (!actor.IsTransferOnly)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                case CEffectPreferenceCondition.Condition.AtDay:
                    return !is_night;
                case CEffectPreferenceCondition.Condition.AtNight:
                    return is_night;
                default:
                    break;
            }
            return false;
        }
    }
}
