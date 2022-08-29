using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaFandom.Code.Types
{
    /// <summary>
    /// This attribute indicates a class with delayed initialization.
    /// The attribute only informs and does not entail any action. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class LazyInstanceAttribute : Attribute
    {
        public LazyInstanceAttribute()
        {
            AllowLazyInstance = true;
        }

        public LazyInstanceAttribute(bool allowLazyInstance)
        {
            AllowLazyInstance = allowLazyInstance;
        }

        /// <summary>
        /// Whether delayed initialization is enabled.
        /// </summary>
        public bool AllowLazyInstance { get; private set; }
    }
}
