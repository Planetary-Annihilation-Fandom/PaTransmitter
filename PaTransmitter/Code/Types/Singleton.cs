using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaFandom.Code.Types
{
    /// <summary>
    /// A base class for all single objects. Exists outside of Unity.
    /// </summary>
    public abstract class Singleton<TSingleton> where TSingleton : Singleton<TSingleton>, new()
    {
        private static TSingleton _instance;

        public static Promise<TSingleton> InstancePromise = new Promise<TSingleton>();

        /// <summary>
        /// Does a copy exist.
        /// </summary>
        public static bool Exist => _instance != null;

        protected virtual bool CanBeDestroyedOutside => true;

        public static TSingleton Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (!AllowLazyInstance)
                        throw new System.Exception("Lazy instance: "+typeof(TSingleton).ToString());

                    CreateInstance();
                }

                return _instance ?? throw new NullReferenceException();
            }
            protected set => _instance = value;
        }

        /// <summary>
        /// Whether initialization is allowed when an instance is accessed.
        /// </summary>
        protected static bool AllowLazyInstance
        {
            get
            {
                var lazyAttribute =
                    (LazyInstanceAttribute)Attribute.GetCustomAttribute(typeof(TSingleton),
                        typeof(LazyInstanceAttribute));

                if (lazyAttribute == null)
                    return true;

                return lazyAttribute.AllowLazyInstance;
            }
        }


        public static bool DestroyIfExist()
        {
            if (_instance != null)
            {
                if (!_instance.CanBeDestroyedOutside)
                    return false;

                _instance.Dispose();
                _instance = null;

                return true;
            }

            return true;
        }

        public static TSingleton CreateInstance()
        {
            if (!DestroyIfExist())
                return _instance;

            CreateInstanceInternal();

            return _instance;
        }

        protected static void CreateInstanceInternal()
        {
            _instance = new TSingleton();
            _instance.OnInstanceCreatedInternal();
        }

        protected virtual void OnInstanceCreatedInternal()
        {
            InstancePromise.Value = this as TSingleton;
        }

        protected virtual void Dispose()
        {
        }
    }
}
