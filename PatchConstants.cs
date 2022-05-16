using System;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using FilesChecker;
using ISession = GInterface115;

namespace SIT.Tarkov.Coop.Core
{
    public static class PatchConstants
    {
        //public static BindingFlags PrivateFlags { get; private set; }
        //public static Type[] EftTypes { get; private set; }
        //public static Type[] FilesCheckerTypes { get; private set; }
        //public static Type LocalGameType { get; private set; }
        //public static Type ExfilPointManagerType { get; private set; }
        //public static Type BackendInterfaceType { get; private set; }
        //public static Type SessionInterfaceType { get; private set; }
        //public static Type GroupingType { get; }

        private static ISession _backEndSession;
        public static ISession BackEndSession
        {
            get
            {
                if (_backEndSession == null)
                {
                    _backEndSession = Singleton<ClientApplication>.Instance.GetClientBackEndSession();
                }

                return _backEndSession;
            }
        }

        //static PatchConstants()
        //{
        //    _ = nameof(ISession.GetPhpSessionId);

        //    PrivateFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        //    EftTypes = typeof(AbstractGame).Assembly.GetTypes();
        //    FilesCheckerTypes = typeof(ICheckResult).Assembly.GetTypes();
        //    LocalGameType = EftTypes.Single(x => x.Name == "LocalGame");
        //    ExfilPointManagerType = EftTypes.Single(x => x.GetMethod("InitAllExfiltrationPoints") != null);
        //    BackendInterfaceType = EftTypes.Single(x => x.GetMethods().Select(y => y.Name).Contains("CreateClientSession") && x.IsInterface);
        //    SessionInterfaceType = EftTypes.Single(x => x.GetMethods().Select(y => y.Name).Contains("GetPhpSessionId") && x.IsInterface);

        //}
    }

    public static class PrivateValueAccessor
    {
        public const BindingFlags Flags = BindingFlags.GetProperty
                                        | BindingFlags.SetProperty
                                        | BindingFlags.GetField
                                        | BindingFlags.SetField
                                        | BindingFlags.NonPublic
                                        | BindingFlags.Public
                                        | BindingFlags.FlattenHierarchy
                                        | BindingFlags.IgnoreCase;

        public static PropertyInfo GetPrivatePropertyInfo(Type type, string propertyName)
        {
            PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | Flags);
            return props.FirstOrDefault(propInfo => propInfo.Name == propertyName);
        }

        public static PropertyInfo GetStaticPropertyInfo(Type type, string propertyName)
        {
            PropertyInfo[] props = type.GetProperties(BindingFlags.Static | Flags);
            return props.FirstOrDefault(propInfo => propInfo.Name == propertyName);
        }

        public static object GetStaticPropertyValue(Type type, string propertyName)
        {
            return GetStaticPropertyInfo(type, propertyName).GetValue(null);
        }

        public static object GetPrivatePropertyValue(Type type, string propertyName, object o)
        {
            return GetPrivatePropertyInfo(type, propertyName).GetValue(o);
        }

        public static FieldInfo GetPrivateFieldInfo(Type type, string fieldName)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | Flags);
            return fields.FirstOrDefault(fieldInfo => fieldInfo.Name == fieldName);
        }

        public static object GetPrivateFieldValue(Type type, string fieldName, object o)
        {
            return GetPrivateFieldInfo(type, fieldName).GetValue(o);
        }

        public static void SetPrivateFieldValue(Type type, string fieldName, object o, object value)
        {
            GetPrivateFieldInfo(type, fieldName).SetValue(o, value);
        }
    }
}
