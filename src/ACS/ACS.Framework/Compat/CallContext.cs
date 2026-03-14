using System;
using System.Collections.Concurrent;
using System.Threading;

namespace System.Runtime.Remoting.Messaging
{
    /// <summary>
    /// .NET 8 compatibility shim for CallContext (removed in .NET Core).
    /// Uses AsyncLocal to provide equivalent thread-local storage.
    /// </summary>
    public static class CallContext
    {
        private static readonly ConcurrentDictionary<string, AsyncLocal<object>> _state 
            = new ConcurrentDictionary<string, AsyncLocal<object>>();

        public static void SetData(string name, object data)
        {
            _state.GetOrAdd(name, _ => new AsyncLocal<object>()).Value = data;
        }

        public static object GetData(string name)
        {
            return _state.TryGetValue(name, out var value) ? value.Value : null;
        }

        public static void FreeNamedDataSlot(string name)
        {
            if (_state.TryRemove(name, out var value))
            {
                value.Value = null;
            }
        }

        public static void LogicalSetData(string name, object data)
        {
            SetData(name, data);
        }

        public static object LogicalGetData(string name)
        {
            return GetData(name);
        }
    }
}

namespace System.Runtime.Remoting
{
    /// <summary>
    /// .NET 8 compatibility shim for RemotingServices
    /// </summary>
    public static class RemotingServices
    {
        public static bool IsTransparentProxy(object proxy)
        {
            return false;
        }

        public static bool IsObjectOutOfContext(object tp)
        {
            return false;
        }

        public static Proxies.RealProxy GetRealProxy(object proxy)
        {
            return null;
        }
    }

    public class ObjectHandle : MarshalByRefObject
    {
        private object _wrappedObject;
        
        public ObjectHandle(object o)
        {
            _wrappedObject = o;
        }
        
        public object Unwrap()
        {
            return _wrappedObject;
        }
    }

    public interface IRemotingTypeInfo
    {
        string TypeName { get; set; }
        bool CanCastTo(Type fromType, object o);
    }
}

namespace System.Runtime.Remoting.Proxies
{
    public abstract class RealProxy
    {
        private Type _proxiedType;

        protected RealProxy(Type classToProxy)
        {
            _proxiedType = classToProxy;
        }

        protected RealProxy()
        {
        }

        public Type GetProxiedType()
        {
            return _proxiedType;
        }

        public virtual object GetTransparentProxy()
        {
            return null;
        }

        public virtual Remoting.Messaging.IMessage Invoke(Remoting.Messaging.IMessage msg)
        {
            throw new NotSupportedException("Remoting proxies are not supported in .NET 8");
        }
    }
}

namespace System.Runtime.Remoting.Messaging
{
    public interface IMethodCallMessage : IMethodMessage
    {
        int InArgCount { get; }
        object GetInArg(int argNum);
        string GetInArgName(int index);
        object[] InArgs { get; }
    }

    public interface IMethodReturnMessage : IMethodMessage
    {
        Exception Exception { get; }
        int OutArgCount { get; }
        object GetOutArg(int argNum);
        string GetOutArgName(int index);
        object[] OutArgs { get; }
        object ReturnValue { get; }
    }

    public interface IMethodMessage : IMessage
    {
        string MethodName { get; }
        string TypeName { get; }
        object MethodSignature { get; }
        int ArgCount { get; }
        object GetArg(int argNum);
        string GetArgName(int index);
        object[] Args { get; }
        bool HasVarArgs { get; }
        System.Reflection.MethodBase MethodBase { get; }
        string Uri { get; }
        LogicalCallContext LogicalCallContext { get; }
    }

    public interface IMessage
    {
        System.Collections.IDictionary Properties { get; }
    }

    public class LogicalCallContext : ICloneable
    {
        public object Clone()
        {
            return new LogicalCallContext();
        }

        public void SetData(string name, object data)
        {
            CallContext.SetData(name, data);
        }

        public object GetData(string name)
        {
            return CallContext.GetData(name);
        }
    }
}
