
// =================================	
// Namespaces.
// =================================

using UnityEngine;

// =================================	
// Define namespace.
// =================================

namespace MirzaBeig
{

    namespace ParticleSystems
    {

        // =================================	
        // Classes.
        // =================================

        //[ExecuteInEditMode]
        [System.Serializable]

        public class DestroyAfterTime : MonoBehaviour
        {
            // =================================	
            // Nested classes and structures.
            // =================================

            // ...

            // =================================	
            // Variables.
            // =================================

            // ...

            public float time = 3.0f;

            // =================================	
            // Functions.
            // =================================
            
            // ...

            void Start()
            {
                if (gameObject.activeSelf)
                Destroy(gameObject, time);
            }
            
            // =================================	
            // End functions.
            // =================================

        }

        // =================================	
        // End namespace.
        // =================================

    }

}

// =================================	
// --END-- //
// =================================
