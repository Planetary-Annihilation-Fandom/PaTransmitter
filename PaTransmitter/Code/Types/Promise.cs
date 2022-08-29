using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaFandom.Code.Types
{
    /// <summary>
    /// <para>This "promise" object allows you to get data that is only in the future.</para>
    /// <para>If an error occurs, the "promise" will return the object of the error.</para>
    /// <para>After calling one of the events, all subscribers will be unsubscribed.</para>
    /// <para>New subscribers will get a result even if the event was triggered earlier.</para>
    /// </summary>
    public class Promise<TValue, TFail>
    {
        public enum PromiseState
        {
            Waiting,
            Value,
            Fail
        }

        // One of the fields will be empty. Option is used to denote emptiness, in particular structures.
        private Option<TValue> _value = Option<TValue>.None;
        private Option<TFail> _fail = Option<TFail>.None;

        public PromiseState State { get; protected set; } = PromiseState.Waiting;

        protected event Action<TValue> OnValue = delegate (TValue obj) { };
        protected event Action<TFail> OnFail = delegate (TFail obj) { };

        public TValue Value
        {
            set
            {
                if (State == PromiseState.Fail)
                    throw new InvalidOperationException();

                _value = value;
                State = PromiseState.Value;

                OnValue(_value.option);

                ClearAllSubscribers();
            }
        }

        public TFail Fail
        {
            set
            {
                if (State == PromiseState.Value)
                    throw new InvalidOperationException();

                _fail = value;
                State = PromiseState.Fail;

                OnFail(_fail.option);

                ClearAllSubscribers();
            }
        }

        public void GetValue(Action<TValue> resultAction)
        {
            OnValue += resultAction;

            if (_value)
                InvokeValue(_value.option);
        }

        public void GetFail(Action<TFail> failAction)
        {
            OnFail += failAction;

            if (_fail)
                InvokeFail(_fail.option);
        }

        protected void InvokeValue(in TValue result)
        {
            OnValue(result);
            ClearAllSubscribers();
        }

        protected void InvokeFail(in TFail fail)
        {
            OnFail(fail);
            ClearAllSubscribers();
        }

        protected void ClearAllSubscribers()
        {
            OnValue = delegate (TValue r) { };
            OnFail = delegate (TFail f) { };
        }
    }

    public class Promise<TValue> : Promise<TValue, int>
    {

    }
}
