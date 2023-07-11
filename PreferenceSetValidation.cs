using Kitchen;
using KitchenMods;
using Unity.Collections;
using Unity.Entities;

namespace PreferenceSystem
{
    public class PreferenceSetValidation : GenericSystemBase, IModSystem
    {
        //public const float CHECK_INTERVAL = 20f;

        public struct SPreferenceSetCheck : IComponentData, IModComponent
        {
            public bool IsLoaded;
            public FixedString128 PreferenceSetName;
            public float DelayProgress;
            public bool IsDirty;
        }

        protected override void Initialise()
        {
            //base.Initialise();
        }

        protected override void OnUpdate()
        {
            //SPreferenceSetCheck checker = GetOrCreate<SPreferenceSetCheck>();

            //checker.DelayProgress -= Time.RealDeltaTime;
            //if (checker.DelayProgress < 0f)
            //{
            //    checker.IsDirty = PreferenceSystemRegistry.IsPreferencesTampered;
            //    checker.DelayProgress = CHECK_INTERVAL;
            //}
            //Set(checker);
        }
    }
}
