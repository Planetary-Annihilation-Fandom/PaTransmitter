using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaFandom.Code.Types
{
    /// <summary>
    /// <para>This structure is an analog of option from the F# language.
    /// Allows you to return a result or nothing.</para>
    /// <para>This type signals that the result may return in two ways. 
    /// The options are Some and None.</para>
    /// <para>Converts to bool and generic type implicitly.</para>
    /// </summary>
    public readonly struct Option<TValue>
    {
        private static Option<TValue> _none = new Option<TValue>();
        public static ref readonly Option<TValue> None => ref _none;

        private readonly TValue _option;

        /// <summary>
        /// Does value exist.
        /// </summary>
        public readonly bool IsSome;

        /// <summary>
        /// Whether the value is missing.
        /// </summary>
        public bool IsNone => !IsSome;

        /// <summary>
        /// Nested value.
        /// </summary>
        public TValue option => _option;

        public Option(TValue option)
        {
            _option = option;
            IsSome = option != null;
        }

        public static implicit operator TValue(Option<TValue> option)
        {
            return option.option;
        }

        public static implicit operator Option<TValue>(TValue value)
        {
            return new Option<TValue>(value);
        }

        public static implicit operator bool(Option<TValue> option)
        {
            return option.IsSome;
        }

        public override bool Equals(object obj)
        {
            if (obj is Option<TValue>)
                return this.Equals((Option<TValue>)obj);
            else
                return false;
        }

        public bool Equals(Option<TValue> other)
        {
            if (this.IsSome && other.IsSome)
                return object.Equals(_option, other._option);
            else
                return IsNone == other.IsNone;
        }

        public override int GetHashCode()
        {
            return !this ? -1 : _option.GetHashCode();
        }

        public override string ToString()
        {
            return this ? $"Some: {option.ToString()}" : "None";
        }
    }
}
