using System;
using UnityEngine;

// Typed registry for global managers. Each manager calls RegisterSingleton(this) in Awake.
// Ins never searches the scene: a manager that is not registered yet is a setup error.
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T ins;

    public static T Ins
    {
        get
        {
            if (ins == null)
            {
                throw new InvalidOperationException(typeof(T).Name + " is not registered. Add it to the scene and call RegisterSingleton in Awake.");
            }

            return ins;
        }
    }

    protected void RegisterSingleton(T instance)
    {
        if (ins != null)
        {
            throw new InvalidOperationException("Duplicate singleton: " + typeof(T).Name);
        }

        ins = instance;
    }

    // Subclasses with their own cleanup must override this and call base.OnDestroy().
    protected virtual void OnDestroy()
    {
        if (ReferenceEquals(ins, this))
        {
            ins = null;
        }
    }
}
