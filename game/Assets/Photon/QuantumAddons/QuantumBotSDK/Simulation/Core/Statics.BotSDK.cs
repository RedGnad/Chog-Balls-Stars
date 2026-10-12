namespace Quantum
{
#if QUANTUM_UNITY
    using PreserveAttribute = UnityEngine.Scripting.PreserveAttribute;
#endif
    
    partial class Statics
    {
        static partial void InitStaticDelegatesBotSDK()
        {
            StaticsBotSDK.InitStaticDelegates();
        }
        
        static partial void RegisterSimulationTypesBotSDK(TypeRegistry typeRegistry)
        {
            StaticsBotSDK.RegisterSimulationTypes(typeRegistry);
        }
        
        [Preserve]
        static void EnsureNotStrippedBotSDK()
        {
            StaticsBotSDK.EnsureNotStrippedGen();
        }
    }
}