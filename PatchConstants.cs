using System;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using FilesChecker;
using ISession = GInterface115;

namespace SIT.Tarkov.Core
{
    public static class PatchConstants
    {
        public static BindingFlags PrivateFlags { get; private set; }
        public static Type[] EftTypes { get; private set; }
        public static Type[] FilesCheckerTypes { get; private set; }
        public static Type LocalGameType { get; private set; }
        public static Type ExfilPointManagerType { get; private set; }
        public static Type BackendInterfaceType { get; private set; }
        public static Type SessionInterfaceType { get; private set; }

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

        static PatchConstants()
        {
            _ = nameof(ISession.GetPhpSessionId);

            PrivateFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            EftTypes = typeof(AbstractGame).Assembly.GetTypes();
            FilesCheckerTypes = typeof(ICheckResult).Assembly.GetTypes();
            LocalGameType = EftTypes.Single(x => x.Name == "LocalGame");
            ExfilPointManagerType = EftTypes.Single(x => x.GetMethod("InitAllExfiltrationPoints") != null);
            BackendInterfaceType = EftTypes.Single(x => x.GetMethods().Select(y => y.Name).Contains("CreateClientSession") && x.IsInterface);
            SessionInterfaceType = EftTypes.Single(x => x.GetMethods().Select(y => y.Name).Contains("GetPhpSessionId") && x.IsInterface);
        }
    }
}
